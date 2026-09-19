using System;
using System.IO;
using UnityEngine;

namespace ThreeMonthsOfSpring
{
    /// <summary>
    /// セーブデータのファイル入出力。
    ///
    /// 保存先は <c>Application.persistentDataPath</c>。Windows なら
    /// %USERPROFILE%\AppData\LocalLow\&lt;会社名&gt;\&lt;製品名&gt;\ になる。
    /// プロジェクト内には置かない（リポジトリにセーブデータが混ざるため）。
    /// </summary>
    public static class SaveSystem
    {
        public const int SlotCount = 3;

        private static string PathFor(int slot)
        {
            return Path.Combine(Application.persistentDataPath, $"save_{slot}.json");
        }

        public static bool Exists(int slot)
        {
            return File.Exists(PathFor(slot));
        }

        /// <summary>読み込む。存在しない・壊れている・バージョン不一致なら null。</summary>
        public static SaveData Load(int slot)
        {
            string path = PathFor(slot);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                string json = File.ReadAllText(path);
                var data = JsonUtility.FromJson<SaveData>(json);

                if (data == null)
                {
                    Debug.LogWarning($"[Save] スロット {slot} を解釈できませんでした。");
                    return null;
                }

                if (data.version != SaveData.CurrentVersion)
                {
                    Debug.LogWarning(
                        $"[Save] スロット {slot} はバージョン {data.version} のデータです" +
                        $"（現在は {SaveData.CurrentVersion}）。読み込めません。");
                    return null;
                }

                if (string.IsNullOrEmpty(data.inkState))
                {
                    Debug.LogWarning($"[Save] スロット {slot} に進行状態が入っていません。");
                    return null;
                }

                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] スロット {slot} の読み込みに失敗しました: {e.Message}");
                return null;
            }
        }

        /// <summary>書き込む。成功したら true。</summary>
        public static bool Save(int slot, SaveData data)
        {
            data.version = SaveData.CurrentVersion;
            data.savedAtIso = DateTime.Now.ToString("o");

            string path = PathFor(slot);

            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);

                // 書き込み中に落ちても既存データを壊さないよう、一時ファイル経由で置き換える。
                string temporary = path + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(data, true));

                if (File.Exists(path))
                {
                    File.Delete(path);
                }

                File.Move(temporary, path);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] スロット {slot} の保存に失敗しました: {e.Message}");
                return false;
            }
        }

        public static void Delete(int slot)
        {
            string path = PathFor(slot);

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[Save] スロット {slot} の削除に失敗しました: {e.Message}");
            }
        }

        /// <summary>スロット一覧のボタンに出す文字列。</summary>
        public static string Describe(int slot)
        {
            SaveData data = Load(slot);
            if (data == null)
            {
                return Exists(slot)
                    ? $"スロット {slot + 1}　-　読み込めないデータ"
                    : $"スロット {slot + 1}　-　空き";
            }

            string when = DateTime.TryParse(data.savedAtIso, out DateTime parsed)
                ? parsed.ToString("yyyy/MM/dd HH:mm")
                : "日時不明";

            string chapter = string.IsNullOrEmpty(data.chapter) ? "（章なし）" : data.chapter;

            return $"スロット {slot + 1}　{chapter}\n<size=70%>{when}</size>";
        }
    }
}
