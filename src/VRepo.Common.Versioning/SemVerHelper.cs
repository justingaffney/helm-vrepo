using Semver;

namespace VRepo.Common.Versioning;

public static class SemVerHelper
{
    private static readonly SemVersion _sortingFallbackChartVersion = new(0, 0, 0);

    /// <summary>
    /// Normalize version to valid SemVer version 2
    /// </summary>
    /// <remarks>
    /// Some versions are not valid SemVer, even though they should be.
    /// 
    /// Handles a few simple cases of invalid formatting that have been encountered out in the wild.
    /// </remarks>
    /// <param name="version">The version, which should be (mostly) SemVer v2 format.</param>
    /// <returns>The normalized version, which should be parsable as valid SemVer v2.</returns>
    public static string Normalize(string version)
    {
        // Values starting with "v" (e.g. "v1.2.3")
        if (version.StartsWith('v'))
        {
            version = version[1..];
        }

        // Remove leading zeroes in MAJOR.MINOR.PATCH (e.g. "11.02.33")
        var versionComponents = version.Split('-');
        if (versionComponents.Length > 0)
        {
            var majorMinorPatchComponents = versionComponents[0].Split('.');
            var major = majorMinorPatchComponents.Length >= 1
                ? majorMinorPatchComponents[0]
                : string.Empty;
            var minor = majorMinorPatchComponents.Length >= 2
                ? majorMinorPatchComponents[1]
                : string.Empty;
            var patch = majorMinorPatchComponents.Length >= 3
                ? majorMinorPatchComponents[2]
                : string.Empty;

            // If there are more components (e.g. 1.2.3.4) discard them.
            // TODO Or maybe add it as the build component (e.g. +build) if it is not present?

            var majorMinorPatch = string.Join('.', new[] { major, minor, patch }.Select(component =>
            {
                var trimmed = component.Trim().TrimStart('0');

                return !string.IsNullOrEmpty(trimmed)
                    ? trimmed
                    : "0"; // Version component must have been 0
            }));

            versionComponents[0] = majorMinorPatch;

            version = string.Join('-', versionComponents);
        }

        return version;
    }

    public static IEnumerable<T> SortVersionedElements<T>(
        IEnumerable<T> sequence,
        Func<T, string> getVersion,
        bool ascending
    )
    {
        var projectedSequence = sequence
            .Select(element =>
            {
                var versionStr = Normalize(getVersion(element));
                if (!SemVersion.TryParse(versionStr, out var version))
                {
                    // Version is not valid SemVer version 2,
                    // Use fallback SemVer for sorting
                    version = _sortingFallbackChartVersion;
                }

                return new
                {
                    Version = version,
                    Element = element
                };
            });

        var orderedSequence = ascending
            ? projectedSequence.OrderBy(value => value.Version, SemVersion.SortOrderComparer)
            : projectedSequence.OrderByDescending(value => value.Version, SemVersion.SortOrderComparer);

        return orderedSequence.Select(value => value.Element);
    }
}
