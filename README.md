# 三か月の春 (Three Months of Spring)

短編小説のプロットをもとにした、Unity 製のノベルゲームです。

三か月限定の現場に送られた、心の折れたエンジニアの話。原案には無かった **選択肢** と
**バッドエンド** を加え、「誰の評価のために働くのか」を選び直せる構造にしています。

> 登場する人物・企業・団体はすべて架空です。

---

## 遊び方

| 操作 | 動作 |
|---|---|
| 画面クリック / `Space` / `Enter` | 次へ進む（文字送り中なら全文表示） |
| 選択肢をクリック | 分岐を選ぶ |
| 画面右下の操作バー | ログ / セーブ / ロード / BGM 切替 / タイトルへ |

エンディングは 5 種類。到達したものはタイトル画面に記録されます。

### 機能

- **セーブ / ロード** — 3 スロット。上書き時は確認が入ります
- **バックログ** — 本文・章の区切り・選んだ選択肢を記録。**セーブデータに同梱されるので、ロードすればログごと復元されます**
- **BGM** — 場面に応じて自動で切り替わり（クロスフェード）。ON / OFF は記憶されます

セーブデータの保存先は `Application.persistentDataPath`（Windows なら
`%USERPROFILE%\AppData\LocalLow\ryo-sato0619\three-months-of-spring\save_0.json` など）です。
プロジェクト内には作られないので、リポジトリに混ざることはありません。

---

## 必要環境

- **Unity 6000.6.2f1**（別バージョンでも動くはずですが、検証はこのバージョンのみ）
- 日本語フォントがインストールされた OS
  - Windows: 游ゴシック / メイリオ / MS ゴシック
  - macOS: ヒラギノ角ゴ
  - Linux: Noto Sans CJK JP など

`git` が PATH に必要です。ink のパッケージを Git URL 経由で取得するため。

---

## セットアップ

```bash
git clone <このリポジトリの URL>
```

あとは Unity Hub から `three-months-of-spring` フォルダを開き、
`Assets/Scenes/Main.unity` を開いて Play を押すだけです。

初回起動時にパッケージ（ink）の取得が走るため、少し時間がかかります。

### シーンを作り直したい場合

UI はすべて `Assets/Scripts/Editor/SceneBuilder.cs` がコードから組み立てています。
Inspector で手作業の配線をする必要はありません。UI を変えたいときはこのスクリプトを
編集して、メニューから再生成してください。

- `Tools > 三か月の春 > 1. TMP 必須リソースを取り込む`
- `Tools > 三か月の春 > 2. シーンを生成する`

---

## プロジェクト構成

```
Assets/
  Ink/
    ThreeMonthsOfSpring.ink     シナリオ本体。分岐とエンディングもここ
  Scenes/
    Main.unity                  SceneBuilder が生成したシーン
  Scripts/
    NovelGameController.cs      ink を読み進める本体
    JapaneseFontProvider.cs     OS のフォントから TMP フォントを実行時生成
    BackgroundPalette.cs        #bg タグ → 背景のグラデーション
    Editor/
      ProjectSetup.cs           TMP 必須リソースの取り込み
      SceneBuilder.cs           シーンをコードから生成
  TextMesh Pro/                 TMP の必須リソース（取り込み済み）
docs/
  branching.md                  分岐とエンディングの全体図
```

---

## シナリオの書き方

シナリオは [ink](https://www.inklestudios.com/ink/)（inkle 製のオープンソースな
分岐物語スクリプト言語）で書かれています。`Assets/Ink/ThreeMonthsOfSpring.ink` を
テキストエディタで編集して保存すれば、Unity 側が自動で再コンパイルします。

### 使っているタグ

行末の `#` 以降がタグです。ゲーム側が解釈するのは次の 4 つ。

| タグ | 役割 |
|---|---|
| `# name: 羽田 春香` | 話者名。付けない行は地の文として扱われる |
| `# chapter: 第二章　日常と、気持ちの変化` | 画面左上の章タイトル |
| `# bg: office_evening` | 背景。画像かグラデーションのキー |
| `# bgm: warm` | BGM。`Assets/Resources/Bgm/` のファイル名 |
| `# sprite: haruka_smile` | 立ち絵。`none` で非表示 |
| `# ending: true_spring` | エンディング ID。到達記録に使う |

### 背景と BGM の差し替え

| 種類 | 置き場所 | 無い場合 |
|---|---|---|
| 背景 | `Assets/Resources/Backgrounds/<キー>.jpg` | `BackgroundPalette.cs` の色グラデーションで代用 |
| BGM | `Assets/Resources/Bgm/<キー>.ogg` | 無音（ゲームは動く） |
| 立ち絵 | `Assets/Resources/Sprites/<キー>.png` | 立ち絵なし（ゲームは動く） |

**立ち絵はまだ未配置です。** 表示する仕組みだけ実装してあるので、
6 枚の PNG を置けば有効になります。用意の仕方と仕様は
[docs/character-sprites.md](docs/character-sprites.md) を参照してください。

素材の出典とライセンスは [CREDITS.md](CREDITS.md) を参照。
再配布が明示的に許諾されているもの（Unsplash License / CC BY 4.0）だけを選んでいます。

ファイル名を `#bg` / `#bgm` タグの値に合わせて置くだけで自動的に使われます。
背景写真は縦横比を保ったまま画面を覆うように配置されます（`NovelGameController.FitBackground`）。

---

## 分岐とエンディング

詳細は [docs/branching.md](docs/branching.md) を参照。

内部的には 2 つの変数だけで制御しています。

| 変数 | 意味 | 範囲 |
|---|---|---|
| `kizuna` | ヒロインとの距離。前向きな選択で増える | 0〜5 |
| `risk` | 契約外業務への無報告な深入り度 | 0〜2 |
| `tsutaeta` | 最終日に感謝を言葉にしたか | true / false |

| エンディング | 到達条件 |
|---|---|
| TRUE END ／ 三か月の春 | 延長を受ける ＋ `kizuna >= 4` ＋ `tsutaeta` |
| NORMAL END ／ それぞれの春 | 延長を受ける ＋ 上記を満たさない |
| NORMAL END ／ 春は過ぎて | 延長を断る ＋ `kizuna >= 3` |
| BAD END ／ 評価されないまま | 延長を断る ＋ `kizuna <= 2` |
| BAD END ／ 越えた一線 | `risk` が 2 に達する（第三章で打ち切り） |

---

## 現状の制限

以下は未実装です。

- **立ち絵の素材**（表示する仕組みは実装済み。[docs/character-sprites.md](docs/character-sprites.md)）
- 効果音
- 既読スキップ、オート再生
- 画面ごとのトランジション（背景は即時切り替え）
- 日本語フォントは OS のものを実行時に読み込むため、**配布ビルドでは
  ライセンスの明確なフォント（Noto Sans JP など）を同梱してください**。
  `JapaneseFontProvider.Override` に差し替えれば切り替わります。

---

## サードパーティ

素材とソフトウェアの出典・ライセンスは [CREDITS.md](CREDITS.md) にまとめています。

- 音楽: Kevin MacLeod (incompetech.com) — CC BY 4.0
- 背景写真: Unsplash — Unsplash License
- [ink / ink-unity-integration](https://github.com/inkle/ink-unity-integration) — MIT License, inkle Ltd.
- TextMeshPro — Unity Companion License（Unity 同梱）
