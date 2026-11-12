namespace RaspiStorage.Cli.Shell.Commands;

public class ExitCommand : ICommand
{
    public string Usage => "exit";
    public string Description => "Exit the shell";

    public Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct)
    {
        ctx.Exit = true;
        return Task.CompletedTask;
    }
}