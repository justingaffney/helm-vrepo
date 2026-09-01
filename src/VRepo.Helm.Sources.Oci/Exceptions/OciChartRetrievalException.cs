namespace VRepo.Helm.Sources.Oci.Exceptions;

public sealed class OciChartRetrievalException(string message, Exception? innerException = null) : Exception(message, innerException);
