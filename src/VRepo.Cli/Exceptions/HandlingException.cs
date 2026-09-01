namespace VRepo.Cli.Exceptions;

public class HandlingException(string message, Exception? innerException = null) : Exception(message, innerException);
