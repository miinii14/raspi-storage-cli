namespace RaspiStorage.Cli.DTOs;

public record FolderDto(Guid Id, Guid OwnerId, Guid? ParentId, string Name,  DateTime CreatedAt);