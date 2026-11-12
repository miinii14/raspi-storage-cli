using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.Config;
using RaspiStorage.Cli.Exceptions;
using RaspiStorage.Cli.Shell;

HttpClient http;

try
{
    http = CliConfig.CreateHttpClient();
} catch (ConfigException ex)
{
    Output.Error("Config error", ex.Message);
    return 1;
}

FilesApi filesApi = new(http);
FoldersApi foldersApi = new(http);
Context context = new();
CommandShell shell = new(http, context, filesApi, foldersApi);

await shell.RunAsync();

return 0;