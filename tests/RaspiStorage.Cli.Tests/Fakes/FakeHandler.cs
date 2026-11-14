namespace RaspiStorage.Cli.Tests.Fakes;

public class FakeHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> respond) : HttpMessageHandler
{
    public List<string> Requests { get; } = [];

    public FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> respond)
        : this(request => Task.FromResult(respond(request)))
    {
    }

    public HttpClient CreateClient() => new(this) { BaseAddress = new Uri("https://fake/") };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        Requests.Add($"{request.Method} {request.RequestUri!.PathAndQuery}");
        return await respond(request);
    }
}
