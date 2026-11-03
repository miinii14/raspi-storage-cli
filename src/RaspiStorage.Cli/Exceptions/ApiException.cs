namespace RaspiStorage.Cli.Exceptions;

public record ApiProblem(
    string? Title,
    int? Status,
    string? Detail,
    string? Code,
    string? TraceId,
    Dictionary<string, string[]>? Errors);

public class ApiException(int status, ApiProblem? problem)
    : Exception(problem?.Detail ?? problem?.Title ?? $"Server returned HTTP {status}")
{
    public int Status { get; } = status;
    public ApiProblem? Problem { get; } = problem;
    public string? Code => Problem?.Code;
}