# クレジット

このゲームで使用している素材の出典とライセンスです。

いずれも **再配布が明示的に許諾されているもの**だけを選んでいます。
再配布禁止の規約を持つ素材（日本の多くのフリー素材サイトなど）は、
public リポジトリに同梱できないため使用していません。

---

## 音楽

すべて **Kevin MacLeod** (incompetech.com) の楽曲で、
**Creative Commons: By Attribution 4.0 License** の下で利用しています。
<https://creativecommons.org/licenses/by/4.0/>

| 場面 | ファイル | 曲名 |
|---|---|---|
| タイトル | `title.ogg` | *Bittersweet* |
| 沈んだ場面・バッドエンド | `quiet.ogg` | *Disquiet* |
| 春香との日常 | `warm.ogg` | *Morning* |
| 緊張・監査 | `tension.ogg` | *Stay the Course* |
| 迷い・決断 | `thinking.ogg` | *Immersed* |
| エンディング | `ending.ogg` | *Inspired* |

```
"Bittersweet", "Disquiet", "Morning", "Stay the Course", "Immersed", "Inspired"
Kevin MacLeod (incompetech.com)
Licensed under Creative Commons: By Attribution 4.0 License
http://creativecommons.org/licenses/by/4.0/
```

> リポジトリに含めるサイズを抑えるため、配布元の 320kbps MP3 を
> OGG Vorbis (q3) に変換しています。それ以外の改変はしていません。

---

## 背景画像

すべて [Unsplash](https://unsplash.com) の写真で、**Unsplash License** の下で利用しています。
同ライセンスは複製・改変・**再配布**・商用利用を、許諾や帰属表示なしに認めています。
以下の表記は義務ではなく、撮影者への敬意によるものです。

| ファイル | 場面 | 撮影者 | 写真 ID |
|---|---|---|---|
| `street_morning.jpg` | 朝の通勤路 | Mylène Larnaud | `TeNP4a_hJzQ` |
| `office_day.jpg` | オフィス（昼） | Petr | `Ugnm0F4e00U` |
| `office_evening.jpg` | オフィス（夕） | kate.sade | `2zZp12ChxhU` |
| `office_night.jpg` | オフィス（夜） | JC Gellidon | `EH9f0TI5wco` |
| `restaurant_noon.jpg` | 定食屋 | Ryunosuke Kikuno | `5jAfMVcE0Ag` |
| `izakaya_night.jpg` | 居酒屋の路地 | Pema G. Lama | `6cfK0SEtpbY` |
| `meeting_room.jpg` | 会議室 | Benjamin Child | `GWe0dlVD9e0` |
| `park_noon.jpg` | 公園 | Jelena Kostic | `ZUPzx-3-Hd0` |
| `street_evening.jpg` | 夕暮れの帰り道 | Weichao Deng | `2fapz9fG8NI` |
| `street_night.jpg` | 夜道 | Lutz Stallknecht | `-VNwMCUu5WI` |

写真ページは `https://unsplash.com/photos/<写真 ID>` で参照できます。

> リポジトリのサイズを抑えるため、幅 1920px・JPEG 品質 80 で取得しています。

---

## 立ち絵

羽田春香の立ち絵 6 点は、**画像生成 AI によって生成**したものです。

| ファイル | 表情 |
|---|---|
| `haruka_smile.png` | 柔らかい笑顔 |
| `haruka_normal.png` | 真顔 |
| `haruka_laugh.png` | 笑い |
| `haruka_troubled.png` | 困り顔 |
| `haruka_sad.png` | 寂しげ |
| `haruka_surprise.png` | 驚き |

生成後、`tools/Remove-CheckerBackground.ps1` で背景を除去し、
透過 PNG に変換しています。それ以外の改変はしていません。

> **補足**: 日本の著作権法では、人間の創作的寄与が乏しい AI 生成物には
> 著作権が発生しないと解されています。利用条件は生成に使用した
> サービスの利用規約に従います。

---

## ソフトウェア

| | ライセンス |
|---|---|
| [ink / ink-unity-integration](https://github.com/inkle/ink-unity-integration) — inkle Ltd. | MIT License |
| TextMeshPro — Unity Technologies | Unity Companion License |

---

## フォント

**Noto Sans JP** (`NotoSansJP-Regular.otf`) を同梱しています。

- 提供: Google / [Noto CJK](https://github.com/notofonts/noto-cjk)
- ライセンス: **SIL Open Font License 1.1**
- ライセンス全文: [`Assets/Fonts/NotoSansJP-LICENSE.txt`](Assets/Fonts/NotoSansJP-LICENSE.txt)

同梱しているのは、実行時に OS のフォントを読む方式では
Android や WebGL で日本語が表示できないためです。
フォント自体は改変しておらず、TextMeshPro のフォントアセット
（`Assets/Resources/Fonts/NotoSansJP SDF.asset`）を生成して参照しています。

解決の優先順位は `JapaneseFontProvider.cs` にあり、同梱フォント →
OS のフォント、の順で探します。
