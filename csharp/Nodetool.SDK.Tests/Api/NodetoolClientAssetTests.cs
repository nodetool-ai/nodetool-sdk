using System.Net;
using System.Text;
using Nodetool.SDK.Api;

namespace Nodetool.SDK.Tests.Api;

public class NodetoolClientAssetTests
{
    [Fact]
    public async Task UploadAsset_PreservesMediaContentTypeAndCurrentGetUrl()
    {
        var handler = new InspectingHandler(request =>
        {
            var multipart = Assert.IsType<MultipartFormDataContent>(request.Content);
            var file = Assert.Single(multipart);
            Assert.Equal("image/png", file.Headers.ContentType?.MediaType);
            Assert.Equal("sample.png", file.Headers.ContentDisposition?.FileName?.Trim('"'));

            const string json = """
                {
                  "id": "asset-1",
                  "name": "sample.png",
                  "content_type": "image/png",
                  "size": 3,
                  "get_url": "http://localhost/assets/asset-1.png",
                  "created_at": "2026-07-23T00:00:00Z"
                }
                """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        using var httpClient = new HttpClient(handler);
        var client = new NodetoolClient(httpClient);
        client.Configure("http://localhost:7777");
        using var content = new MemoryStream(new byte[] { 1, 2, 3 });

        var asset = await client.UploadAssetAsync(
            "sample.png",
            content,
            "image/png");

        Assert.Equal("asset-1", asset.Id);
        Assert.Equal("http://localhost/assets/asset-1.png", asset.GetUrl);
    }

    private sealed class InspectingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> inspect) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(inspect(request));
    }
}
