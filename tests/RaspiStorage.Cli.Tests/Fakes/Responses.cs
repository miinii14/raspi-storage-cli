using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;

namespace RaspiStorage.Cli.Tests.Fakes;

public static class Responses
{
    public static HttpResponseMessage Json<T>(T body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = JsonContent.Create(body) };

    public static HttpResponseMessage Problem(HttpStatusCode status, string detail, string code) =>
        new(status)
        {
            Content = JsonContent.Create(
                new { title = status.ToString(), status = (int)status, detail, code },
                mediaType: new MediaTypeHeaderValue("application/problem+json"))
        };

    public static HttpResponseMessage Raw(HttpStatusCode status, string body, string mediaType) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, mediaType) };

    public static HttpResponseMessage NoContent() => new(HttpStatusCode.NoContent);
}
