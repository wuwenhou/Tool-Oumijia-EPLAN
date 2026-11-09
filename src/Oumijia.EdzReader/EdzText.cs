using System;

namespace Oumijia.EdzReader
{
    /// <summary>
    /// Helpers for EPLAN multilingual strings.
    /// EPLAN stores them as "en_US@Text;de_DE@Text;..." inside one XML attribute.
    /// </summary>
    public static class EdzText
    {
        /// <summary>
        /// Returns the best single-language text from an EPLAN multilingual value:
        /// en_US → de_DE → first plausible segment. Junk language tags (e.g. "??_??") are skipped.
        /// Plain text without a language tag is returned unchanged.
        /// </summary>
        public static string CleanMultiLanguage(string value)
        {
            if (String.IsNullOrWhiteSpace(value)) return null;

            string english = null;
            string german = null;
            string first = null;

            foreach (string rawSegment in value.Split(';'))
            {
                string segment = rawSegment.Trim();
                if (segment.Length == 0) continue;

                string language = null;
                string text = segment;
                int at = segment.IndexOf('@');
                if (at > 0)
                {
                    string prefix = segment.Substring(0, at).Trim();
                    // EPLAN uses "xx_XX@text". Anything with '?' is corrupt metadata, not a language.
                    if (prefix.IndexOf('?') >= 0) continue;
                    if (IsLanguageTag(prefix))
                    {
                        language = prefix;
                        text = segment.Substring(at + 1).Trim();
                    }
                }

                if (text.Length == 0) continue;
                if (first == null) first = text;

                if (IsEnglish(language))
                {
                    english = text;
                    break;
                }
                if (german == null && IsGerman(language)) german = text;
            }

            string result = english ?? german ?? first;
            if (String.IsNullOrWhiteSpace(result)) return null;
            return result.Trim();
        }

        private static bool IsEnglish(string language)
        {
            return String.Equals(language, "en_US", StringComparison.OrdinalIgnoreCase)
                || String.Equals(language, "en_EN", StringComparison.OrdinalIgnoreCase)
                || String.Equals(language, "en_GB", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsGerman(string language)
        {
            return String.Equals(language, "de_DE", StringComparison.OrdinalIgnoreCase)
                || String.Equals(language, "de_AT", StringComparison.OrdinalIgnoreCase)
                || String.Equals(language, "de_CH", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLanguageTag(string value)
        {
            if (value == null || value.Length != 5) return false;
            if (!Char.IsLetter(value[0]) || !Char.IsLetter(value[1])) return false;
            if (value[2] != '_') return false;
            if (!Char.IsLetter(value[3]) || !Char.IsLetter(value[4])) return false;
            return true;
        }
    }
}
