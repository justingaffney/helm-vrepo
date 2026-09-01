namespace VRepo.Helm.Sources.Http.Exceptions;

public sealed class RepositoryIndexRetrievalException(string message, Exception? innerException = null) : Exception(message, innerException);
