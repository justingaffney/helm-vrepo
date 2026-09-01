using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Semver;

namespace VRepo.Common.Versioning;

public sealed class VersionPattern
{
    private readonly string rawPattern;
    private readonly SemVersionRange? semverRange;
    private readonly Regex? regex;

    private VersionPattern(string rawPattern, SemVersionRange semverRange)
    {
        this.rawPattern = rawPattern;
        this.semverRange = semverRange;
    }

    private VersionPattern(string rawPattern, Regex regex)
    {
        this.rawPattern = rawPattern;
        this.regex = regex;
    }

    public override string ToString()
    {
        return rawPattern;
    }

    public bool IsMatch(string version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(version);

        if (semverRange is not null)
        {
            if (!SemVersion.TryParse(SemVerHelper.Normalize(version), out var semver))
            {
                // Version is not valid SemVer version 2
                return false;
            }

            return semverRange.Contains(semver);
        }
        else
        {
            return regex!.IsMatch(version);
        }
    }

    public static VersionPattern Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException("Value is null or blank.");
        }

        // 1. SemVer range
        if (SemVersionRange.TryParseNpm(value, out var semverRange) || SemVersionRange.TryParse(value, out semverRange))
        {
            return new VersionPattern(value, semverRange);
        }

        // 2. Regex
        if (RegexHelper.TryParse(value, out var regex) || RegexHelper.TryParse(RegexHelper.WildcardToRegex(value), out regex))
        {
            return new VersionPattern(value, regex);
        }

        throw new FormatException($"Value \"{value}\" is not a valid version pattern.");
    }

    public static bool TryParse(string value, [NotNullWhen(true)] out VersionPattern? pattern)
    {
        try
        {
            pattern = Parse(value);
            return true;
        }
        catch (FormatException)
        {
            pattern = null;
            return false;
        }
    }
}
