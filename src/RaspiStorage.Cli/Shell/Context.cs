using RaspiStorage.Cli.DTOs;
using RaspiStorage.Cli.Shell.Commands;

namespace RaspiStorage.Cli.Shell;

public class Context
{
    public bool Exit { get; set; } = false;
    public List<FolderDto> Folders { get; set; } = [];
    public FolderDto? CurrentFolder => Folders.LastOrDefault();
    public string Path => PathResolver.ToPath(Folders);
}
