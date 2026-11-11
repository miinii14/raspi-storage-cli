using RaspiStorage.Cli.Exceptions;
using Spectre.Console;

namespace RaspiStorage.Cli.Shell.Commands;

public class ClearCommand : ICommand
{
    public string Description => "Clear console output";
    public Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct)
    {
        if (args.Length > 0)
            throw new CliException("clear", "Usage: clear");
        
        AnsiConsole.Clear();
        return Task.CompletedTask;
    }
}