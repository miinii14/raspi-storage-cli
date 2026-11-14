using System.Net;
using RaspiStorage.Cli.Api;
using RaspiStorage.Cli.DTOs;
using RaspiStorage.Cli.Exceptions;
using RaspiStorage.Cli.Tests.Fakes;

namespace RaspiStorage.Cli.Tests;

public class ApiBaseTests
{
    private static FoldersApi ApiReturning(HttpResponseMessage response) =>
        new(new FakeHandler(_ => response).CreateClient());

    [Fact]
    public async Task Success_ReturnsDeserializedDto()
    {
        var folder = new FolderDto(Guid.NewGuid(), Guid.NewGuid(), null, "Photos", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var api = ApiReturning(Responses.Json(folder));

        var result = await api.GetByIdAsync(folder.Id);

        Assert.Equal(folder, result);
    }

    [Fact]
    public async Task ProblemJson_ThrowsApiExceptionWithProblemFields()
    {
        var api = ApiReturning(Responses.Problem(HttpStatusCode.NotFound, "Folder not found", "not_found"));

        var ex = await Assert.ThrowsAsync<ApiException>(() => api.GetByIdAsync(Guid.NewGuid()));

        Assert.Equal(404, ex.Status);
        Assert.Equal("not_found", ex.Code);
        Assert.Equal("Folder not found", ex.Message);
    }

    [Fact]
    public async Task PlainTextError_ThrowsApiExceptionWithoutProblem()
    {
        var api = ApiReturning(Responses.Raw(HttpStatusCode.InternalServerError, "boom", "text/plain"));

        var ex = await Assert.ThrowsAsync<ApiException>(() => api.GetByIdAsync(Guid.NewGuid()));

        Assert.Equal(500, ex.Status);
        Assert.Null(ex.Problem);
        Assert.Equal("Server returned HTTP 500", ex.Message);
    }

    [Fact]
    public async Task BrokenJsonError_ThrowsApiExceptionNotJsonException()
    {
        var api = ApiReturning(Responses.Raw(HttpStatusCode.BadRequest, "{not json", "application/json"));

        var ex = await Assert.ThrowsAsync<ApiException>(() => api.GetByIdAsync(Guid.NewGuid()));

        Assert.Equal(400, ex.Status);
        Assert.Null(ex.Problem);
    }

    [Fact]
    public async Task SuccessWithNullBody_ThrowsInvalidOperation()
    {
        var api = ApiReturning(Responses.Raw(HttpStatusCode.OK, "null", "application/json"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => api.GetByIdAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetByParentId_Root_SendsEmptyParentId()
    {
        var handler = new FakeHandler(_ => Responses.Json(Array.Empty<FolderDto>()));
        var api = new FoldersApi(handler.CreateClient());

        await api.GetByParentIdAsync(null);

        Assert.Equal(["GET /api/folders?parentId="], handler.Requests);
    }
}
