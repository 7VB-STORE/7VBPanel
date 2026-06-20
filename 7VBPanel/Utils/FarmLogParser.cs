using System.Text.RegularExpressions;

namespace _7VBPanel.Utils
{
    /// <summary>Парсинг con_log CS2 для фарма.</summary>
    public static class FarmLogParser
    {
        private static readonly Regex CtScored = new Regex(@"Team\s+""CT""\s+scored", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex TScored = new Regex(@"Team\s+""(?:TERRORIST|T)""\s+scored", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex RoundStart = new Regex(
            @"(?:World triggered )?Round_Start|Round_Official_Start|beginning of round|round_start",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex[] TeamPatterns =
        {
            new Regex(@"(?:assigned to|joined)\s+team[:\s""]+(CT|TERRORIST|T|Counter-Terrorist)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"You are on the\s+(CT|TERRORIST|Terrorist|Counter-Terrorist)\s+team", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"join_team\s+(ct|t|terrorist)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"""team""\s+""?(CT|TERRORIST|T)""?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            new Regex(@"SwitchTeam.*?team\s*=\s*(CT|TERRORIST|T)", RegexOptions.IgnoreCase | RegexOptions.Compiled),
        };

        public static void Count(string logText, out int ctScored, out int tScored, out long roundStarts)
        {
            ctScored = 0;
            tScored = 0;
            roundStarts = 0;
            if (string.IsNullOrEmpty(logText))
                return;

            ctScored = CtScored.Matches(logText).Count;
            tScored = TScored.Matches(logText).Count;
            roundStarts = RoundStart.Matches(logText).Count;
        }

        /// <summary>Последнее упоминание команды в con_log этого клиента.</summary>
        public static string TryParsePlayerTeam(string logText)
        {
            if (string.IsNullOrEmpty(logText))
                return null;

            string last = null;
            foreach (var rx in TeamPatterns)
            {
                foreach (Match m in rx.Matches(logText))
                {
                    if (m.Groups.Count > 1)
                        last = TeamHelper.Normalize(m.Groups[1].Value);
                }
            }
            return last;
        }
    }
}
