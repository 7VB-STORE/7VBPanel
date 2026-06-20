using System;

namespace _7VBPanel.Utils
{
    public static class TeamHelper
    {
        public static string Normalize(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            string t = raw.Trim().ToUpperInvariant();
            if (t == "CT" || t == "COUNTER-TERRORIST" || t == "COUNTERTERRORIST" || t == "3")
                return "CT";
            if (t == "T" || t == "TERRORIST" || t == "TERRORISTS" || t == "2")
                return "T";
            return null;
        }
    }
}
