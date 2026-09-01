using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace VRepo.Common.Versioning;

public static class RegexHelper
{
    public static bool TryParse(string pattern, [NotNullWhen(true)] out Regex? regex)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            regex = null;
            return false;
        }

        try
        {
            Regex.Match(string.Empty, pattern);

            // Regex is valid
            regex = new Regex(pattern);
            return true;
        }
        catch (ArgumentException)
        {
            regex = null;
            return false;
        }
    }

    public static string WildcardToRegex(string value)
    {
        return $"^{Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*")}$";
    }
}
