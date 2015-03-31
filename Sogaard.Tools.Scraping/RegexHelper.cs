namespace Sogaard.Tools.Scraping
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Regex helper class
    /// </summary>
    public static class RegexHelper
    {
        public static bool Validate(string pattern, string source)
        {
            Regex re = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Singleline);
            Match m = re.Match(source);
            return m.Success;
        }

        /// <summary>
        /// Return the first result of the regex
        /// Will return null if no match.
        /// </summary>
        public static string Match(string pattern, string source)
        {
            if (string.IsNullOrWhiteSpace(pattern) || 
                string.IsNullOrWhiteSpace(source))
                return null;

            Regex re = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Singleline);
            Match m = re.Match(source);
            if (m.Groups.Count == 0)
                return null;

            return m.Groups[1].Value;
        }

        /// <summary>
        /// Return the fist match of very good.
        /// Good to fine one thing multible times
        /// </summary>
        public static List<string> MatchAll(string pattern, string source)
        {
            if (string.IsNullOrWhiteSpace(pattern) ||
                string.IsNullOrWhiteSpace(source))
                return new List<string>();

            Regex re = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Singleline);
            MatchCollection mc = re.Matches(source);

            List<string> results = new List<string>();
            foreach (Match m in mc)
            {
                if (m.Groups.Count > 1)
                    foreach (var c in m.Groups[1].Captures)
                    {
                        results.Add(c.ToString());
                    }
            }
            return results;
        }

        /// <summary>
        /// Find all groups
        /// </summary>
        public static List<List<string>> MatchAllGroups(string pattern, string source)
        {
            if (string.IsNullOrWhiteSpace(pattern) ||
                string.IsNullOrWhiteSpace(source))
                return new List<List<string>>();

            Regex re = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline | RegexOptions.Singleline);
            MatchCollection mc = re.Matches(source);

            List<List<string>> results = new List<List<string>>();
            foreach (Match m in mc)
            {
                var innerList = new List<string>();
                for (int g = 1; g < m.Groups.Count; g++)
                {
                    innerList.Add(m.Groups[g].Value);
                }
                results.Add(innerList);
            }
            return results;
        }

        /// <summary>
        /// Remove all HTML tags from a string
        /// </summary>
        public static string RemoveHtmlTags(string soruce)
        {
            return Regex.Replace(soruce, "<.*?>", string.Empty);
        }

        /// <summary>
        /// Regex replace
        /// </summary>
        public static string Replace(string pattern, string replacePattern, string str)
        {
            return Regex.Replace(str, pattern, replacePattern,
                RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline |
                RegexOptions.Singleline);
        }
    }
}
