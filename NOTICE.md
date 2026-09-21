# ライセンスの適用範囲 / Notice

[LICENSE](LICENSE) の MIT ライセンスは **このリポジトリで新規に作成した部分**に適用されます。

The MIT license in [LICENSE](LICENSE) applies to the parts originally created
for this repository.

| 対象 | 内容 |
|---|---|
| `Assets/Scripts/` | ゲーム本体・エディタ拡張のスクリプト |
| `Assets/Ink/ThreeMonthsOfSpring.ink` | シナリオ本体 |
| `Assets/WebGLTemplates/` | WebGL 用テンプレート |
| `Assets/Scenes/` | コードから生成したシーン |
| `tools/` `docs/` | ツールとドキュメント |

Copyright (c) 2026 砂糖

---

## 同梱している第三者の素材

**これらには MIT ライセンスは及びません。それぞれ元のライセンスのままです。**

Bundled third-party assets remain under their own licenses. The MIT license
does **not** extend to them.

| 素材 | 場所 | ライセンス |
|---|---|---|
| 背景写真 10 点 | `Assets/Resources/Backgrounds/` | Unsplash License |
| BGM 6 曲 — Kevin MacLeod | `Assets/Resources/Bgm/` | CC BY 4.0 |
| Noto Sans JP | `Assets/Fonts/` | SIL Open Font License 1.1 |
| ink / ink-unity-integration — inkle Ltd. | `Packages/manifest.json` で参照 | MIT License |
| TextMesh Pro — Unity Technologies | `Assets/TextMesh Pro/` | Unity Companion License |

出典・撮影者・曲名などの詳細は [CREDITS.md](CREDITS.md) を参照してください。
See [CREDITS.md](CREDITS.md) for full attribution.

---

## 立ち絵について

羽田春香の立ち絵 6 点は画像生成 AI で作成したものです。
日本の著作権法では、人間の創作的寄与が乏しい AI 生成物には著作権が
発生しないと解されています。利用条件は生成に使用したサービスの
利用規約に従います。

---

## ink について

ink はパッケージとして参照しているだけで、**コードは一切改変していません。**
`Packages/manifest.json` でコミット `73cc147` に固定しています。
実体は `Library/PackageCache/` に展開され、このリポジトリには含まれません。
