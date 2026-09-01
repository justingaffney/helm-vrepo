using VRepo.Helm.Specification.Models.Repositories.References;

namespace VRepo.Helm.Specification.Models.Charts.Oci;

public sealed record OciChartReference(OciUrlRepository Repository, string Name);
