namespace RaspiStorage.Cli.Exceptions;

public sealed record ApiProblem(
    string? Title,
    int? Status,
    string? Detail,
    string? Code,
    string? TraceId,
    Dictionary<string, string[]>? Errors);

public sealed class ApiException(int status, ApiProblem? problem)
    : Exception(problem?.Detail ?? problem?.Title ?? $"Server returned HTTP {status}")
{
    public int Status { get; } = status;
    public ApiProblem? Problem { get; } = problem;
    public string? Code => Problem?.Code;
}