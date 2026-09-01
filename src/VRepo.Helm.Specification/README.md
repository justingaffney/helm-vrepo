# Helm Schema
Implementations of standard Helm repository index and chart YAML and OCI schemas.

| Type | Documentation | Implementation |
| --- | --- | --- |
| Chart | [Helm Documentation](https://helm.sh/docs/v3/topics/charts/#the-chartyaml-file), [JSON Schema](https://www.schemastore.org/chart.json), [OCI Chart Manifest Description](https://helm.sh/docs/topics/registries/#helm-chart-manifest) | [`Models/Charts/ChartMetadata.cs`](Models/Charts/ChartMetadata.cs), [`Models/Charts/Oci/OciChartManifest.cs`](Models/Charts/Oci/OciChartManifest.cs) |
| Repository Index | [Helm Documentation](https://helm.sh/docs/topics/chart_repository/#the-index-file) | [`Models/Repositories/RepositoryIndex.cs`](Models/Repositories/RepositoryIndex.cs) |
