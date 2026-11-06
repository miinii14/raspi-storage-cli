using RaspiStorage.Cli.DTOs;

namespace RaspiStorage.Cli.Shell;

public class Context
{
   public bool Exit { get; set; } = false;
   public FolderDto? CurrentFolder { get; set; } = null;
   public List<FolderDto> Folders = [];
   public string Path => "/" + string.Join("/", Folders.Select(f => f.Name));
}