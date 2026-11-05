namespace RaspiStorage.Cli.Shell;

public class ExitCommand : ICommand
{
    public string Description => "Exit the shell";

    public Task ExecuteAsync(string[] args, Context ctx, CancellationToken ct)
    {
        ctx.Exit = true;
        return Task.CompletedTask;
    }
}