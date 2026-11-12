namespace RaspiStorage.Cli.Shell.Commands;

public interface ICommand
{
    string Usage { get; }
    string Description { get; }
    Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct);
}