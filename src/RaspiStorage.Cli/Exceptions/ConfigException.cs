namespace RaspiStorage.Cli.Exceptions;

public class ConfigException(string message, Exception? innerException = null) : Exception(message, innerException);