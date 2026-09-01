namespace VRepo.Common.Serialization.Exceptions;

public sealed class SerializationException(string message, Exception innerException) : Exception(message, innerException);
