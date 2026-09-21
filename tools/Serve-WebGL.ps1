<#
.SYNOPSIS
    WebGL ビルドをローカルの HTTP サーバで配信する。

.DESCRIPTION
    Unity の WebGL ビルドは file:// では動かない。ブラウザが JavaScript の
    モジュール読み込みや fetch をローカルファイルに対して拒否するため、
    HTTP 経由で配信する必要がある。

    このスクリプトは .NET の HttpListener を使うので、Node や Python を
    入れなくても動く。同じ LAN の別 PC やスマートフォンからも開けるよう、
    既定ですべてのインターフェースで待ち受ける。

.PARAMETER Path
    配信するフォルダ（index.html がある場所）。

.PARAMETER Port
    待ち受けポート。既定 8080。

.EXAMPLE
    .\Serve-WebGL.ps1 -Path "$env:USERPROFILE\Desktop\三か月の春_WebGL"

.NOTES
    すべてのインターフェースで待ち受けるには管理者権限が必要なことがある。
    権限が無い場合は自動で localhost のみにフォールバックする。
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $Path,
    [int] $Port = 8080
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path (Join-Path $Path 'index.html'))) {
    throw "index.html が見つかりません: $Path"
}

$root = (Resolve-Path $Path).Path

# Unity の WebGL ビルドが返す必要のある種類。
# .wasm を application/wasm で返さないとブラウザがストリーミング実行を拒否する。
$types = @{
    '.html' = 'text/html; charset=utf-8'
    '.js'   = 'application/javascript'
    '.wasm' = 'application/wasm'
    '.data' = 'application/octet-stream'
    '.json' = 'application/json'
    '.css'  = 'text/css'
    '.png'  = 'image/png'
    '.jpg'  = 'image/jpeg'
    '.ico'  = 'image/x-icon'
    '.svg'  = 'image/svg+xml'
}

$listener = New-Object System.Net.HttpListener
$prefix = "http://+:$Port/"
try {
    $listener.Prefixes.Add($prefix)
    $listener.Start()
} catch {
    # 権限が足りない場合は localhost だけで開く。
    Write-Warning "すべてのインターフェースで待ち受けられませんでした。localhost のみで起動します。"
    Write-Warning "他の端末から開くには、PowerShell を管理者として実行してください。"
    $listener = New-Object System.Net.HttpListener
    $prefix = "http://localhost:$Port/"
    $listener.Prefixes.Add($prefix)
    $listener.Start()
}

$addresses = Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue |
    Where-Object { $_.IPAddress -notmatch '^(127\.|169\.254\.)' } |
    Select-Object -ExpandProperty IPAddress

Write-Host ""
Write-Host "配信中: $root" -ForegroundColor Cyan
Write-Host ""
Write-Host "  この PC から     : http://localhost:$Port/" -ForegroundColor Green
foreach ($ip in $addresses) {
    Write-Host "  同じ LAN の端末から: http://${ip}:$Port/" -ForegroundColor Green
}
Write-Host ""
Write-Host "  停止するには Ctrl+C" -ForegroundColor DarkGray
Write-Host ""

try {
    while ($listener.IsListening) {
        # ブラウザが転送の途中で接続を切ると例外が飛ぶ。
        # 1 件の失敗でサーバ全体が止まらないよう、要求ごとに閉じ込める。
        try {
            $context = $listener.GetContext()
        } catch {
            if (-not $listener.IsListening) { break }
            Write-Host ("接続の受付に失敗: " + $_.Exception.Message) -ForegroundColor DarkYellow
            continue
        }

        $request = $context.Request
        $response = $context.Response

        try {
            $relative = [System.Uri]::UnescapeDataString($request.Url.AbsolutePath).TrimStart('/')
            if ([string]::IsNullOrEmpty($relative)) { $relative = 'index.html' }

            $file = Join-Path $root ($relative -replace '/', '\')

            # 配信フォルダの外に出る要求は拒否する。
            $full = [System.IO.Path]::GetFullPath($file)
            if (-not $full.StartsWith($root, [System.StringComparison]::OrdinalIgnoreCase)) {
                $response.StatusCode = 403
                continue
            }

            if (-not (Test-Path $full -PathType Leaf)) {
                $response.StatusCode = 404
                Write-Host ("404  " + $relative) -ForegroundColor DarkYellow
                continue
            }

            $extension = [System.IO.Path]::GetExtension($full).ToLowerInvariant()
            $response.ContentType = if ($types.ContainsKey($extension)) { $types[$extension] } else { 'application/octet-stream' }

            $length = (Get-Item $full).Length
            $response.ContentLength64 = $length

            # HEAD はヘッダだけを返す。本体を書くとエラーになる。
            if ($request.HttpMethod -ne 'HEAD') {
                # .data や .wasm は数十 MB になるため、全体をメモリに載せずに流す。
                $stream = [System.IO.File]::OpenRead($full)
                try {
                    $stream.CopyTo($response.OutputStream, 81920)
                } finally {
                    $stream.Dispose()
                }
            }

            Write-Host ("{0}  {1}  ({2:N1} MB)" -f $request.HttpMethod, $relative, ($length / 1MB)) -ForegroundColor DarkGray
        } catch {
            # 転送中の切断はブラウザ側の都合でよく起きる。記録だけして続行する。
            Write-Host ("中断  " + $_.Exception.Message) -ForegroundColor DarkYellow
        } finally {
            try { $response.Close() } catch { }
        }
    }
} finally {
    $listener.Stop()
    $listener.Close()
}
