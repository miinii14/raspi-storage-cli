namespace RaspiStorage.Cli.Shell.Commands;

public interface ICommand
{
    string Description { get; }
    Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct);
}