using System.Text;

namespace TiltRush
{
    /// <summary>
    /// Checks player names (leaderboards, online). Allowed: 3-12 letters, digits, space, _ - .
    /// Blocks insults, slurs and extremist words in English and German, also when written with tricks like
    /// "F_u_c_k" or "5h1t" (digits are turned into letters, separators removed before checking).
    /// Names of OTHER players that do not pass are shown as "Player" + a number.
    /// Extend the Blocked list if you find more - lower case, letters only.
    /// </summary>
    public static class NameFilter
    {
        public const int MinLength = 3, MaxLength = 12;

        static readonly string[] Blocked =
        {
            // English
            "fuck", "fuk", "shit", "bitch", "cunt", "dick", "pussy", "whore", "slut", "bastard", "asshole",
            "nigg", "nigr", "fag", "retard", "porn", "penis", "vagina", "dildo", "jizz", "wank", "twat",
            "nazi", "hitler", "kkk", "isis", "jihad", "terror",
            // German
            "arsch", "fotze", "hure", "nutte", "wichs", "ficken", "fick", "schlampe", "missgeburt", "spast", "spasti",
            "schwuchtel", "neger", "kanake", "hurensohn", "huso", "bastard", "pimmel", "muschi", "titten", "heilhitler",
            "siegheil", "judensau",
        };

        /// <summary>Result: null = ok, otherwise the reason (shown to the player).</summary>
        public static string Check(string name)
        {
            if (string.IsNullOrEmpty(name)) return "Please enter a name.";
            name = name.Trim();
            if (name.Length < MinLength) return "At least " + MinLength + " characters.";
            if (name.Length > MaxLength) return "At most " + MaxLength + " characters.";
            foreach (char c in name)
                if (!(char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-' || c == '.')) return "Only letters, numbers, space, _ - .";
            if (!IsClean(name)) return "Please choose a friendlier name.";
            return null;
        }

        /// <summary>true when the name contains none of the blocked words.</summary>
        public static bool IsClean(string name)
        {
            string n = Normalize(name);
            foreach (var w in Blocked) if (n.Contains(w)) return false;
            return true;
        }

        /// <summary>Name to show for another player: cleaned up or replaced.</summary>
        public static string Display(string name, int seed)
        {
            if (!string.IsNullOrEmpty(name) && IsClean(name)) return name;
            return "Player" + (1000 + (seed & 0x7fffffff) % 9000);
        }

        /// <summary>lower case, leet-speak to letters, everything that is not a letter removed.</summary>
        static string Normalize(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char raw in s.ToLowerInvariant())
            {
                char c = raw;
                switch (c)
                {
                    case '0': c = 'o'; break;
                    case '1': case '!': case '|': c = 'i'; break;
                    case '3': c = 'e'; break;
                    case '4': case '@': c = 'a'; break;
                    case '5': case '$': c = 's'; break;
                    case '7': c = 't'; break;
                    case '8': c = 'b'; break;
                    case 'ä': sb.Append("ae"); continue;
                    case 'ö': sb.Append("oe"); continue;
                    case 'ü': sb.Append("ue"); continue;
                    case 'ß': sb.Append("ss"); continue;
                }
                if (c >= 'a' && c <= 'z') sb.Append(c);
            }
            return sb.ToString();
        }
    }
}
