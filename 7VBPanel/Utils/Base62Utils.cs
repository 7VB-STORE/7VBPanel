using System;
using System.Collections.Generic;

namespace _7VBPanel.Utils
{
    /// <summary>Как FSN_Panel: match_id (decimal) → компактная строка для сравнения в AutoAccept.</summary>
    public static class Base62Utils
    {
        private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private static readonly int Base = Alphabet.Length;

        public static string ToBase62(long num)
        {
            if (num == 0)
                return "0";
            if (num < 0)
                num = -num;
            var result = new List<char>();
            long n = num;
            while (n > 0)
            {
                n = Math.DivRem(n, Base, out long rem);
                result.Add(Alphabet[(int)rem]);
            }
            result.Reverse();
            return new string(result.ToArray());
        }
    }
}
