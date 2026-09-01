namespace VRepo.Helm.Specification.Models.Charts.Oci;

public sealed record OciChartVersionReference(OciChartReference Chart, string Version);
