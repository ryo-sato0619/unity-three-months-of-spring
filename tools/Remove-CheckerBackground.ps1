<#
.SYNOPSIS
    立ち絵画像から「描き込まれた市松模様の背景」を取り除き、本物の透過 PNG にする。

.DESCRIPTION
    画像生成 AI に「透過背景で」と指示すると、透過のつもりで
    market模様（チェッカーボード）を絵として描いてしまうことがよくある。
    アルファチャンネルは無いので、そのまま使うと背景に灰色の格子が出る。

    このスクリプトは画像の四辺から塗りつぶし（フラッドフィル）を行い、
    「彩度が低く、明るい」領域を背景とみなしてアルファを 0 にする。
    キャラクターに囲まれた白いシャツや靴は四辺と繋がっていないため、
    塗りつぶしが到達せず残る。

.PARAMETER InputPath
    入力画像。PNG / WebP / JPEG など。WebP の場合は ffmpeg が必要。

.PARAMETER OutputPath
    出力する PNG のパス。

.PARAMETER SaturationTolerance
    背景とみなす彩度の上限 (RGB の最大値 - 最小値)。既定 22。
    髪や肌が消える場合は下げる。格子が残る場合は上げる。

.PARAMETER MinLightness
    背景とみなす明るさの下限。既定 165。

.PARAMETER Erode
    輪郭に残る半透明の縁を削る回数。既定 2。
    キャラの輪郭が痩せる場合は 0〜1 に下げる。

.EXAMPLE
    .\Remove-CheckerBackground.ps1 -InputPath haruka.webp `
        -OutputPath ..\Assets\Resources\Sprites\haruka_smile.png
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $InputPath,
    [Parameter(Mandatory = $true)][string] $OutputPath,
    [int] $SaturationTolerance = 22,
    [int] $MinLightness = 165,
    [int] $MaxLightness = 252,
    [int] $Erode = 2
)

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# WebP など System.Drawing が読めない形式は、先に ffmpeg で PNG にする。
$work = $InputPath
if ([System.IO.Path]::GetExtension($InputPath) -notin '.png', '.bmp', '.jpg', '.jpeg') {
    $ffmpeg = (Get-Command ffmpeg -ErrorAction SilentlyContinue)
    if (-not $ffmpeg) {
        throw "$([System.IO.Path]::GetExtension($InputPath)) を読むには ffmpeg が必要です。"
    }
    $work = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.Guid]::NewGuid().ToString() + '.png')
    & ffmpeg -hide_banner -loglevel error -y -i $InputPath $work
}

$source = @'
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class CheckerBackgroundRemover
{
    public static string Run(string inPath, string outPath, int satTol, int minL, int maxL, int erode)
    {
        using (var src = new Bitmap(inPath))
        {
            int w = src.Width, h = src.Height;
            using (var bmp = new Bitmap(w, h, PixelFormat.Format32bppArgb))
            {
                using (var g = Graphics.FromImage(bmp)) g.DrawImage(src, 0, 0, w, h);

                var rect = new Rectangle(0, 0, w, h);
                var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                int stride = data.Stride;
                var buf = new byte[stride * h];
                Marshal.Copy(data.Scan0, buf, 0, buf.Length);

                var isBg = new bool[w * h];
                var queue = new Queue<int>();

                // 四辺を起点にする。
                for (int x = 0; x < w; x++) { Seed(buf, stride, w, isBg, queue, x, 0, satTol, minL, maxL);
                                              Seed(buf, stride, w, isBg, queue, x, h - 1, satTol, minL, maxL); }
                for (int y = 0; y < h; y++) { Seed(buf, stride, w, isBg, queue, 0, y, satTol, minL, maxL);
                                              Seed(buf, stride, w, isBg, queue, w - 1, y, satTol, minL, maxL); }

                int[] dx = { 1, -1, 0, 0 };
                int[] dy = { 0, 0, 1, -1 };

                while (queue.Count > 0)
                {
                    int cur = queue.Dequeue();
                    int cx = cur % w, cy = cur / w;
                    for (int d = 0; d < 4; d++)
                    {
                        int nx = cx + dx[d], ny = cy + dy[d];
                        if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                        Seed(buf, stride, w, isBg, queue, nx, ny, satTol, minL, maxL);
                    }
                }

                // 輪郭に残る格子の縁を削る。背景に接していて、かつ背景色に近い画素を落とす。
                for (int pass = 0; pass < erode; pass++)
                {
                    var extra = new List<int>();
                    for (int y = 0; y < h; y++)
                    {
                        for (int x = 0; x < w; x++)
                        {
                            int idx = y * w + x;
                            if (isBg[idx]) continue;
                            bool touches = false;
                            for (int d = 0; d < 4 && !touches; d++)
                            {
                                int nx = x + dx[d], ny = y + dy[d];
                                if (nx < 0 || ny < 0 || nx >= w || ny >= h) continue;
                                if (isBg[ny * w + nx]) touches = true;
                            }
                            if (!touches) continue;

                            int o = y * stride + x * 4;
                            int b = buf[o], gg = buf[o + 1], r = buf[o + 2];
                            int mx = Math.Max(r, Math.Max(gg, b)), mn = Math.Min(r, Math.Min(gg, b));
                            // 縁の判定は本体より緩め（アンチエイリアスで少し暗くなっているため）。
                            if ((mx - mn) <= satTol + 10 && mx >= minL - 25) extra.Add(idx);
                        }
                    }
                    foreach (int i in extra) isBg[i] = true;
                }

                int cleared = 0;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < w; x++)
                    {
                        if (!isBg[y * w + x]) continue;
                        int o = y * stride + x * 4;
                        buf[o] = 0; buf[o + 1] = 0; buf[o + 2] = 0; buf[o + 3] = 0;
                        cleared++;
                    }
                }

                Marshal.Copy(buf, 0, data.Scan0, buf.Length);
                bmp.UnlockBits(data);

                // 余白を切り詰める。立ち絵は下端が接地点になるよう、下は詰めきる。
                int minX = w, minY = h, maxX = -1, maxY = -1;
                for (int y = 0; y < h; y++)
                    for (int x = 0; x < w; x++)
                        if (!isBg[y * w + x])
                        {
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }

                if (maxX < 0) throw new Exception("全画素が背景と判定されました。しきい値を見直してください。");

                var crop = new Rectangle(minX, minY, maxX - minX + 1, maxY - minY + 1);
                using (var trimmed = bmp.Clone(crop, PixelFormat.Format32bppArgb))
                {
                    trimmed.Save(outPath, ImageFormat.Png);
                    return string.Format(
                        "{0}x{1} -> {2}x{3} / 背景として除去: {4:0.0}%",
                        w, h, trimmed.Width, trimmed.Height, cleared * 100.0 / (w * h));
                }
            }
        }
    }

    static void Seed(byte[] buf, int stride, int w, bool[] isBg, Queue<int> q,
                     int x, int y, int satTol, int minL, int maxL)
    {
        int idx = y * w + x;
        if (isBg[idx]) return;
        int o = y * stride + x * 4;
        int b = buf[o], g = buf[o + 1], r = buf[o + 2];
        int mx = Math.Max(r, Math.Max(g, b)), mn = Math.Min(r, Math.Min(g, b));
        if ((mx - mn) > satTol || mx < minL || mx > maxL) return;
        isBg[idx] = true;
        q.Enqueue(idx);
    }
}
'@

Add-Type -TypeDefinition $source -ReferencedAssemblies System.Drawing, System.Collections -Language CSharp

$outDir = Split-Path -Parent $OutputPath
if ($outDir -and -not (Test-Path $outDir)) { New-Item -ItemType Directory -Force $outDir | Out-Null }

$result = [CheckerBackgroundRemover]::Run(
    (Resolve-Path $work).Path, $OutputPath,
    $SaturationTolerance, $MinLightness, $MaxLightness, $Erode)

Write-Host $result
Write-Host "出力: $OutputPath"
