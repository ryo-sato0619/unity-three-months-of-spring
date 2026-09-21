using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace ThreeMonthsOfSpring
{
    /// <summary>
    /// 一度でも表示した本文を覚えておく。既読スキップの判定に使う。
    ///
    /// セーブデータとは別管理にしている。既読はプレイの周回をまたいで
    /// 蓄積されるもので、特定のセーブスロットに属さないため。
    ///
    /// 本文そのものではなくハッシュを保存する。ファイルが小さくなるのと、
    /// シナリオを書き換えた行は自然に「未読」へ戻るため。
    /// </summary>
    public static class ReadHistory
    {
        private const string FileName = "read.json";

        /// <summary>この件数だけ新規に既読が増えたら書き出す。書き込み回数を抑えるため。</summary>
        private const int FlushThreshold = 40;

        [Serializable]
        private sealed class Payload
        {
            public List<string> hashes = new List<string>();
        }

        private static HashSet<string> hashes;
        private static int pendingCount;

        private static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

        private static HashSet<string> Hashes
        {
            get
            {
                if (hashes == null)
                {
                    Load();
                }

                return hashes;
            }
        }

        /// <summary>その本文を読んだことがあるか。</summary>
        public static bool IsRead(string line)
        {
            return !string.IsNullOrEmpty(line) && Hashes.Contains(Hash(line));
        }

        /// <summary>既読として記録する。実際のファイル書き込みはまとめて行う。</summary>
        public static void Mark(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return;
            }

            if (!Hashes.Add(Hash(line)))
            {
                return;
            }

            pendingCount++;
            if (pendingCount >= FlushThreshold)
            {
                Flush();
            }
        }

        /// <summary>未書き出しの分をファイルに反映する。</summary>
        public static void Flush()
        {
            if (hashes == null || pendingCount == 0)
            {
                return;
            }

            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);

                var payload = new Payload { hashes = new List<string>(hashes) };
                string temporary = FilePath + ".tmp";
                File.WriteAllText(temporary, JsonUtility.ToJson(payload));

                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }

                File.Move(temporary, FilePath);
                pendingCount = 0;
            }
            catch (Exception e)
            {
                Debug.LogError($"[既読] 保存に失敗しました: {e.Message}");
            }
        }

        /// <summary>既読記録をすべて消す。</summary>
        public static void Clear()
        {
            hashes = new HashSet<string>();
            pendingCount = 0;

            try
            {
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[既読] 削除に失敗しました: {e.Message}");
            }
        }

        /// <summary>記録されている既読行数。</summary>
        public static int Count => Hashes.Count;

        private static void Load()
        {
            hashes = new HashSet<string>();
            pendingCount = 0;

            try
            {
                if (!File.Exists(FilePath))
                {
                    return;
                }

                var payload = JsonUtility.FromJson<Payload>(File.ReadAllText(FilePath));
                if (payload?.hashes == null)
                {
                    return;
                }

                foreach (string h in payload.hashes)
                {
                    hashes.Add(h);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[既読] 読み込みに失敗したため、既読なしとして続行します: {e.Message}");
                hashes = new HashSet<string>();
            }
        }

        /// <summary>
        /// FNV-1a (64bit)。string.GetHashCode は実行ごとに値が変わりうるため使わない。
        /// </summary>
        private static string Hash(string value)
        {
            const ulong offset = 14695981039346656037;
            const ulong prime = 1099511628211;

            ulong hash = offset;
            foreach (byte b in Encoding.UTF8.GetBytes(value))
            {
                hash ^= b;
                hash *= prime;
            }

            return hash.ToString("x16");
        }
    }
}
