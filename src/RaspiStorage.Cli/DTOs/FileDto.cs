namespace RaspiStorage.Cli.DTOs;

public record FileDto(Guid Id, Guid OwnerId, Guid? FolderId, string Name, long SizeBytes, string? MimeType, DateTime CreatedAt, DateTime UpdatedAt);