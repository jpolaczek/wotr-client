using System;
using System.Linq;
using UnityEngine;

namespace Wotr.Client
{
    // UI-572 (owner, 2026-11-22: the question whether to enter the Mordor track comes "only once"): the games in which this
    // installation has asked it, kept across restarts. Only the latest games are kept; an older one forgotten would ask
    // again at most once.
    public static class MordorQuestionMemory
    {
        public const string Key = "wotr.mordor-question.v1";
        private const int Kept = 40;

        public static bool Asked(string matchId) => matchId.Length > 0 && Matches().Contains(matchId);

        public static void Remember(string matchId)
        {
            if (matchId.Length == 0 || Asked(matchId))
                return;
            var kept = Matches().Append(matchId).Reverse().Take(Kept).Reverse();
            PlayerPrefs.SetString(Key, string.Join(",", kept));
            PlayerPrefs.Save();
        }

        private static string[] Matches() =>
            PlayerPrefs.GetString(Key, "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    }
}
