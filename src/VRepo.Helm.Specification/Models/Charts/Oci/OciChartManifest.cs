namespace VRepo.Helm.Specification.Models.Charts.Oci;

public static class OciChartManifest
{
    public static class MediaTypes
    {
        public const string HelmConfigV1Json = "application/vnd.cncf.helm.config.v1+json";
    }

    public static class Annotations
    {
        public const string Name = "org.opencontainers.image.title";
        public const string Version = "org.opencontainers.image.version";
        public const string Description = "org.opencontainers.image.description";
        public const string CreationTime = "org.opencontainers.image.created";
        public const string HomeUrl = "org.opencontainers.image.url";
        public const string SourceUrl = "org.opencontainers.image.source";
        public const string Maintainers = "org.opencontainers.image.authors";
    }
}
