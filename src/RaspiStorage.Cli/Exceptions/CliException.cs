namespace RaspiStorage.Cli.Exceptions;

public class CliException(string title, string message, Exception? innerException = null) : Exception(message, innerException)
{
    public string Title { get; } = title;
}