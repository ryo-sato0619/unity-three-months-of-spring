using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ink.Runtime;
using Ink.UnityIntegration;
using UnityEditor;
using UnityEngine;

namespace ThreeMonthsOfSpring.EditorTools
{
    /// <summary>
    /// シナリオの全分岐を総当たりで走査し、分岐構造が壊れていないか検証する。
    ///
    /// 確認すること:
    ///   1. すべての終端がエンディングタグを持っているか（タグ漏れの検出）
    ///   2. 宣言したエンディングすべてに到達できるか（到達不能ルートの検出）
    ///   3. 逆に、宣言していないエンディング ID が出てこないか（タイプミスの検出）
    /// </summary>
    public static class StoryVerifier
    {
        private const string InkPath = "Assets/Ink/ThreeMonthsOfSpring.ink";
        private const string EndingTagPrefix = "ending:";

        private sealed class Report
        {
            public readonly Dictionary<string, int> EndingCounts = new Dictionary<string, int>();
            public readonly List<string> PathsWithoutEnding = new List<string>();
            public readonly HashSet<string> BackgroundKeys = new HashSet<string>();
            public readonly HashSet<string> BgmKeys = new HashSet<string>();
            public readonly HashSet<string> SpriteKeys = new HashSet<string>();
            public int TotalPaths;
            public int MaxDepth;
        }

        [MenuItem("Tools/三か月の春/3. 全分岐を検証する")]
        public static void Verify()
        {
            InkFile inkFile = InkAssetUtility.LoadCompiled(InkPath);
            if (inkFile == null)
            {
                Debug.LogError($"[Verify] ink アセットが見つかりません: {InkPath}");
                return;
            }

            // コンパイルエラーは isCompiled より先に報告する。原因が分からないと直せないため。
            if (inkFile.hasErrors || inkFile.hasUnhandledCompileErrors)
            {
                foreach (InkCompilerLog error in inkFile.errors)
                {
                    Debug.LogError($"[Verify] ink エラー ({error.relativeFilePath}:{error.lineNumber}) {error.content}");
                }

                foreach (string error in inkFile.unhandledCompileErrors)
                {
                    Debug.LogError($"[Verify] ink エラー: {error}");
                }

                return;
            }

            if (!inkFile.isCompiled)
            {
                Debug.LogError($"[Verify] ink が未コンパイルです（エラー報告なし）: {InkPath}");
                return;
            }

            var report = new Report();
            Explore(new Story(inkFile.storyJson), new List<int>(), report);

            var sb = new StringBuilder();
            sb.AppendLine("=== 分岐検証 ===");
            sb.AppendLine($"到達可能なルート総数: {report.TotalPaths}");
            sb.AppendLine($"最大選択回数: {report.MaxDepth}");
            sb.AppendLine();

            bool ok = true;

            sb.AppendLine("--- エンディング到達数 ---");
            foreach ((string id, string label) in NovelGameController.AllEndings)
            {
                report.EndingCounts.TryGetValue(id, out int count);
                sb.AppendLine($"  {(count > 0 ? "OK  " : "到達不能")}  {count,4} ルート  {id}  ({label})");
                if (count == 0)
                {
                    ok = false;
                }
            }

            string[] declared = NovelGameController.AllEndings.Select(e => e.Id).ToArray();
            string[] unexpected = report.EndingCounts.Keys.Where(k => !declared.Contains(k)).ToArray();
            if (unexpected.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine("--- 未宣言のエンディング ID（タイプミスの疑い） ---");
                foreach (string id in unexpected)
                {
                    sb.AppendLine($"  {id}");
                }

                ok = false;
            }

            if (report.PathsWithoutEnding.Count > 0)
            {
                sb.AppendLine();
                sb.AppendLine("--- エンディングタグの無い終端 ---");
                foreach (string path in report.PathsWithoutEnding.Take(10))
                {
                    sb.AppendLine($"  選択列: [{path}]");
                }

                ok = false;
            }

            // --- 素材の対応づけ ---
            // タイトル画面でしか鳴らない "title" は ink のタグに出てこないので明示的に足す。
            report.BgmKeys.Add("title");

            sb.AppendLine();
            sb.AppendLine("--- 背景 (Resources/Backgrounds) ---");
            foreach (string key in new SortedSet<string>(report.BackgroundKeys))
            {
                bool hasImage = Resources.Load<Texture2D>("Backgrounds/" + key) != null;
                bool hasGradient = BackgroundPalette.IsKnown(key);
                string state = hasImage ? "画像" : hasGradient ? "グラデーション代用" : "定義なし";
                sb.AppendLine($"  {(hasImage || hasGradient ? "OK  " : "欠落")}  {key,-18} {state}");
                if (!hasImage && !hasGradient)
                {
                    ok = false;
                }
            }

            sb.AppendLine();
            sb.AppendLine("--- BGM (Resources/Bgm) ---");
            foreach (string key in new SortedSet<string>(report.BgmKeys))
            {
                bool hasClip = Resources.Load<AudioClip>("Bgm/" + key) != null;
                sb.AppendLine($"  {(hasClip ? "OK  " : "未配置")}  {key,-18} {(hasClip ? "読み込み可" : "無音になります")}");
            }

            sb.AppendLine();
            sb.AppendLine("--- 立ち絵 (Resources/Sprites) ---");
            foreach (string key in new SortedSet<string>(report.SpriteKeys))
            {
                if (CharacterSpriteProvider.ExistsExactly(key))
                {
                    sb.AppendLine($"  OK    {key,-20} 読み込み可");
                }
                else if (CharacterSpriteProvider.Exists(key))
                {
                    sb.AppendLine($"  代用  {key,-20} 同じキャラの別表情で代用されます");
                }
                else
                {
                    sb.AppendLine($"  未配置  {key,-20} 非表示になります");
                }
            }

            if (ok)
            {
                Debug.Log(sb.ToString() + "\n検証に成功しました。");
            }
            else
            {
                Debug.LogError(sb.ToString() + "\n検証に失敗しました。");
            }
        }

        /// <summary>
        /// 選択肢のたびに状態を保存して全ての枝に潜る、深さ優先の総当たり。
        /// </summary>
        private static void Explore(Story story, List<int> path, Report report)
        {
            string ending = null;

            while (story.canContinue)
            {
                story.Continue();
                foreach (string tag in story.currentTags)
                {
                    if (tag.StartsWith(EndingTagPrefix))
                    {
                        ending = tag.Substring(EndingTagPrefix.Length).Trim();
                    }
                    else if (tag.StartsWith("bg:"))
                    {
                        report.BackgroundKeys.Add(tag.Substring(3).Trim());
                    }
                    else if (tag.StartsWith("bgm:"))
                    {
                        report.BgmKeys.Add(tag.Substring(4).Trim());
                    }
                    else if (tag.StartsWith("sprite:"))
                    {
                        string key = tag.Substring(7).Trim();
                        if (!CharacterSpriteProvider.IsHideKey(key))
                        {
                            report.SpriteKeys.Add(key);
                        }
                    }
                }
            }

            if (story.currentChoices.Count > 0)
            {
                string snapshot = story.state.ToJson();
                int choiceCount = story.currentChoices.Count;

                for (int i = 0; i < choiceCount; i++)
                {
                    story.state.LoadJson(snapshot);
                    story.ChooseChoiceIndex(i);

                    path.Add(i);
                    Explore(story, path, report);
                    path.RemoveAt(path.Count - 1);
                }

                return;
            }

            // 終端に到達した。
            report.TotalPaths++;
            report.MaxDepth = Mathf.Max(report.MaxDepth, path.Count);

            if (string.IsNullOrEmpty(ending))
            {
                report.PathsWithoutEnding.Add(string.Join(", ", path));
                return;
            }

            report.EndingCounts.TryGetValue(ending, out int current);
            report.EndingCounts[ending] = current + 1;
        }
    }
}
