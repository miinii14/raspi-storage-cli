namespace RaspiStorage.Cli.DTOs;

public record CreateFolderRequest(string Name, Guid? ParentId);