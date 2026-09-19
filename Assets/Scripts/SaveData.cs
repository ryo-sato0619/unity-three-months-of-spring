using System;
using System.Collections.Generic;

namespace ThreeMonthsOfSpring
{
    /// <summary>バックログ1行分。</summary>
    [Serializable]
    public sealed class LogEntry
    {
        /// <summary>0 = 通常の行 / 1 = 章の区切り / 2 = プレイヤーが選んだ選択肢。</summary>
        public int kind;

        /// <summary>話者名。地の文では空。</summary>
        public string speaker;

        public string text;

        public const int KindLine = 0;
        public const int KindChapter = 1;
        public const int KindChoice = 2;
    }

    /// <summary>
    /// セーブデータ1件分。
    ///
    /// ink の物語進行は <c>inkState</c>（Story.state.ToJson の出力）が全て持っている。
    /// それとは別に「いま画面に出ている行」を保存しているのは、ink の state が
    /// 「次に読む位置」しか覚えておらず、表示済みの行は復元できないため。
    /// ログもここに同梱するので、ロードすればバックログごと引き継がれる。
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        /// <summary>データ構造を変えたら上げる。互換性の無い古いデータを弾くため。</summary>
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        /// <summary>保存日時（ISO 8601、ローカル時刻）。スロット一覧の表示に使う。</summary>
        public string savedAtIso;

        // --- 画面の復元に必要な情報 ---
        public string chapter;
        public string speaker;
        public string text;
        public string background;
        public string bgm;

        /// <summary>表示中の立ち絵のキー。空なら立ち絵なし。</summary>
        public string sprite;

        // --- 物語の進行状態 ---
        public string inkState;

        // --- バックログ ---
        public List<LogEntry> log = new List<LogEntry>();
    }
}
