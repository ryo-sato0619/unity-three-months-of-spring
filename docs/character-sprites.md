# 立ち絵の用意

立ち絵を表示する仕組みは実装済みです。**下記の名前で PNG を置くだけ**で有効になり、
置かなければ立ち絵なしで通常どおり動きます。

置き場所: `Assets/Resources/Sprites/`

## 必要なファイル

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
| 形式 | PNG（**背景は透過**） |
| 推奨サイズ | 縦 1400〜2000px 程度。横は成り行き |
| 構図 | 膝上〜腰上のバストアップ。**足元が画像の下端に来るように** |
| 表情差分 | **同じポーズ・同じ位置・同じサイズで、顔だけ差し替える** |

最後の項目が重要です。ポーズや拡大率が表情ごとに違うと、表情が変わるたびに
立ち絵が跳ねて見えます。同一の元画像から顔だけ描き換えるのが確実です。

インポート設定（Sprite 化、ピボットを下端中央に設定）は
`Assets/Scripts/Editor/TextureImportRules.cs` が自動で適用します。手作業は不要です。

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
anime style visual novel character sprite, full body from the knees up,
Japanese woman in her late twenties, short black hair, boyish and tidy,
tall for a woman, narrow eyes with a soft friendly expression,
office casual clothing (blouse and cardigan), autumn,
standing straight facing slightly to the left,
transparent background, clean line art, soft lighting, single character only
```

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
