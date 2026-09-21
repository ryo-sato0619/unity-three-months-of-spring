# 立ち絵

**6 表情とも配置済みです。** この文書は、差し替えたり表情を追加したりするときの仕様です。

置き場所: `Assets/Resources/Sprites/`
ファイル名がシナリオの `#sprite:` タグの値と一致すれば、それだけで使われます。

現在の立ち絵は画像生成 AI で作ったものです（[CREDITS.md](../CREDITS.md) に明記）。

## ファイルと表情

登場するのは **羽田 春香** だけです。主人公の鈴木清吾は一人称視点なので立ち絵は不要です。

| ファイル名 | 表情 | 使われる場面 |
|---|---|---|
| `haruka_smile.png` | 基本。柔らかい笑顔 | 初対面、昼食の誘い、歓迎会、最終日 |
| `haruka_normal.png` | 真顔。落ち着いた表情 | 業務の質問、仕事の話をしているとき |
| `haruka_laugh.png` | 声を立てて笑う、屈託ない | 軽口が返ってきたとき、別れ際 |
| `haruka_troubled.png` | 困り顔、眉が下がる | 相談を持ちかけるとき、断られたとき |
| `haruka_sad.png` | 寂しげ、伏し目 | 距離を置かれたとき、頭を下げるとき |
| `haruka_surprise.png` | きょとん、目を見開く | 予想外の言葉をかけられたとき |

必要な表情は
`Tools > 三か月の春 > 3. 全分岐を検証する`
で確認できます。シナリオ中の `#sprite:` タグを走査して、未配置のものを一覧表示します。

## 技術仕様

| 項目 | 指定 |
|---|---|
| 形式 | **PNG**（WebP は Unity が読めません） |
| 背景 | **本物の透過**（アルファチャンネル） |
| 推奨サイズ | 縦 1500〜2000px 程度。横は成り行き |
| 構図 | **全身**（頭から足先まで）。直立、正面かやや斜め |
| 表情差分 | **同じポーズ・同じ位置・同じサイズで、顔だけ差し替える** |

最後の項目が重要です。ポーズや拡大率が表情ごとに違うと、表情が変わるたびに
立ち絵が跳ねて見えます。同一の元画像から顔だけ描き換えるのが確実です。

全身で用意するのは、ゲーム側が「足元を画面の下に逃がして腰から上を見せる」
配置にしているためです。足先まで入っていれば、あとは表示側で調整できます。

インポート設定（Sprite 化、ピボットを下端中央に設定）は
`Assets/Scripts/Editor/TextureImportRules.cs` が自動で適用します。手作業は不要です。

### 表情が足りないうちは代用されます

`haruka_smile.png` だけを置いた状態でも、他の表情は同じキャラクターの
手持ちの絵で自動的に代用されます（`CharacterSpriteProvider.FindSubstitute`）。
表情を 1 枚ずつ足していく途中でも、立ち絵が消えたり現れたりしません。

検証ツールでは、実ファイルがあるものは `OK`、代用されているものは `代用` と表示されます。

---

## 透過背景の落とし穴

画像生成 AI に「透過背景で」と指示すると、**透過を表す市松模様（チェッカーボード）を
絵として描いてしまう**ことがよくあります。見た目は透過そのものですが、
アルファチャンネルは無く、灰色と白の格子が普通のピクセルとして入っています。
そのまま使うとゲーム画面に格子が出ます。

確認方法は、画像編集ソフトで開いて背景が「透明」と表示されるか見るのが確実です。
ファイル形式が JPEG や、アルファ無しの PNG / WebP なら、その時点で透過ではありません。

### 対処

リポジトリに除去ツールを入れてあります。

```powershell
cd tools
.\Remove-CheckerBackground.ps1 `
    -InputPath ..\raw\haruka_laugh.webp `
    -OutputPath ..\Assets\Resources\Sprites\haruka_laugh.png `
    -MaxLightness 255
```

画像の四辺から塗りつぶしを行い、「彩度が低く明るい」領域を背景として除去します。
キャラクターの白いシャツや靴は線画の輪郭に囲まれていて四辺と繋がらないため、
塗りつぶしが到達せずに残ります。あわせて余白の切り詰めと、輪郭に残る縁の除去も行います。
WebP の入力は ffmpeg があれば自動で PNG に変換されます。

うまくいかない場合のつまみ:

| パラメータ | 既定 | 調整 |
|---|---|---|
| `-MaxLightness` | 252 | 白い背景が残るなら `255` |
| `-SaturationTolerance` | 22 | 格子が残るなら上げる。髪や肌が欠けるなら下げる |
| `-Erode` | 2 | 輪郭が痩せるなら `0` か `1` |

**そもそも回避するなら、透過ではなく「単色べた塗りの背景」で生成させるのが確実です。**
`solid chroma green background` のように指定すれば、除去がずっと簡単で正確になります。

## キャラクター設定（原案より）

> 羽田 春香（はねだ はるか）。20代後半。
> ショートカットの黒髪。ボーイッシュな雰囲気。
> 身長は160センチを超え、女性としては高いほう。
> 細目だが、笑顔は柔らかく親しみやすい。
> 営業部署から総務系の部署に異動してきたばかりで、IT の知識はほとんどない。
> 明るく人懐っこく、相手との距離の取り方が近い。
> 一方でリアリストな面もあり、軽口の中に芯がある。

服装は日本のオフィスカジュアル（ブラウスにカーディガン、あるいはジャケット）が
作品の雰囲気に合います。舞台は秋です。

## 生成 AI を使う場合

### 押さえるべきこと

**「AI 生成だから自由に使える」わけではありません。** 実務上効いてくるのは著作権より
**生成サービスの利用規約**です。このリポジトリは public なので、
**出力物の再配布が明示的に許諾されているサービス**を選んでください。

確認すべき点:

1. 出力物の**商用利用**が許諾されているか
2. 出力物の**再配布**（リポジトリへの同梱）が許諾されているか
3. 無料プランに上記の制限が付いていないか（有料プランのみ許諾、というサービスがあります）

日本の著作権法では、人間の創作的寄与が乏しい AI 生成物には著作権が発生しないと
解されています。つまり誰の許諾も要らない代わりに、**あなたも独占的な権利を主張できません**。
個人制作のゲームであれば実害はありませんが、把握はしておいてください。

### クレジットへの記載

生成 AI の利用を明記するのは適切な判断です。義務ではありませんが、
Steam をはじめ AI 利用の開示を求める配布プラットフォームが増えています。

素材を追加したら、[CREDITS.md](../CREDITS.md) とゲーム内クレジット
（`NovelGameController.CreditsText`）の両方に追記してください。書式の例:

```
立ち絵

羽田春香の立ち絵は画像生成AI（<サービス名>）で生成したものです。
生成物の利用条件は <サービス名> の利用規約に従います。
```

### プロンプトの例

そのままでは使えないはずなので、出力を見ながら調整してください。
表情差分は、まず基本の1枚を確定させてから
「同じキャラクター・同じポーズで表情だけ変更」という指示で派生させます。

```
anime style visual novel character sprite, full body from head to feet,
Japanese woman in her late twenties, short black hair, boyish and tidy,
tall for a woman, narrow eyes with a soft friendly expression,
office casual clothing, autumn,
standing straight facing the viewer, arms relaxed at her sides,
solid flat chroma green background, clean line art, soft lighting,
single character only, no text, no watermark
```

背景を `transparent` ではなく `solid flat chroma green` にしているのは、
上記の「市松模様が描き込まれる」問題を避けるためです。
生成後に `Remove-CheckerBackground.ps1` で緑を除去します
（`-SaturationTolerance` を上げ、`-MinLightness` を下げる必要があります）。

表情の指定を差し替えて使います。

| ファイル | 追加する指定 |
|---|---|
| `haruka_smile` | `gentle warm smile, relaxed` |
| `haruka_normal` | `calm neutral expression, attentive` |
| `haruka_laugh` | `laughing cheerfully, eyes closed, head tilted back slightly` |
| `haruka_troubled` | `troubled expression, eyebrows lowered, apologetic smile` |
| `haruka_sad` | `downcast eyes, sad and quiet expression` |
| `haruka_surprise` | `surprised, eyes wide open, mouth slightly open` |

## 生成 AI を使わない場合

- **VRoid Studio**（無料）で 3D キャラクターを作り、正面から書き出す方法があります。
  自分で作ったモデルなので権利関係が明確で、表情差分も同じポーズのまま量産できます。
  この作品のように表情差分が必要なケースとは相性が良いです。
- 立ち絵素材の配布サイトを使う場合は、**再配布の可否**を必ず確認してください。
  日本の多くのフリー素材サイトは素材そのものの再配布を禁じており、
  public リポジトリへの同梱ができません。その場合は
  `Assets/Resources/Sprites/` を `.gitignore` に加え、
  各自がダウンロードして配置する形にしてください。

## 表示位置の調整

立ち絵の位置・大きさは `Assets/Scripts/Editor/SceneBuilder.cs` の
`CreateCharacterSprite()` で決めています。既定では画面のやや右寄り、下端基準です。
変えたい場合はこの関数を編集して、
`Tools > 三か月の春 > 2. シーンを生成する` で作り直してください。
