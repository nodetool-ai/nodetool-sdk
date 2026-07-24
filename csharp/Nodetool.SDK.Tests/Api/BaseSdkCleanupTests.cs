using System.Net;
using System.Text;
using Nodetool.SDK.Api;
using Nodetool.SDK.Assets;
using Nodetool.SDK.Types;
using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.Tests.Api;

public class BaseSdkCleanupTests
{
    [Fact]
    public void TypeRegistry_DiscoversGeneratedTypesFromTheBaseSdk()
    {
        var registry = new NodeToolTypeRegistry();

        registry.RegisterAllTypes();

        Assert.Equal("Nodetool.Types.Core.ImageRef", registry.GetType("image")?.FullName);
    }

    [Fact]
    public void DisposingHttpApiClient_DoesNotDisposeInjectedHttpClient()
    {
        var handler = new TrackingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var httpClient = new HttpClient(handler);
        var client = new NodetoolClient(httpClient);

        client.Dispose();

        Assert.False(handler.IsDisposed);
    }

    [Fact]
    public void DisposingAssetManager_DoesNotDisposeInjectedHttpClient()
    {
        var handler = new TrackingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK));
        using var httpClient = new HttpClient(handler);
        var cacheDirectory = Path.Combine(
            Path.GetTempPath(),
            $"nodetool-sdk-cache-{Guid.NewGuid():N}");

        try
        {
            using (var manager = new AssetManager(
                       cacheDirectory: cacheDirectory,
                       httpClient: httpClient))
            {
            }

            Assert.False(handler.IsDisposed);
        }
        finally
        {
            if (Directory.Exists(cacheDirectory))
                Directory.Delete(cacheDirectory, recursive: true);
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task AssetManagerUpload_UsesCanonicalTypedAssetAndPreservesMimeType()
    {
        var handler = new TrackingHandler(request =>
        {
            var multipart = Assert.IsType<MultipartFormDataContent>(request.Content);
            var file = Assert.Single(multipart);
            Assert.Equal("audio/wav", file.Headers.ContentType?.MediaType);

            const string json = """
                {
                  "id": "audio-1",
                  "name": "sample.wav",
                  "content_type": "audio/wav",
                  "size": 4,
                  "get_url": "http://localhost/assets/audio-1.wav",
                  "created_at": "2026-07-24T00:00:00Z"
                }
                """;
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
        });
        using var httpClient = new HttpClient(handler);
        using var apiClient = new NodetoolClient(httpClient);
        apiClient.Configure("http://localhost:7777");
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"nodetool-sdk-assets-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var filePath = Path.Combine(directory, "sample.wav");
        await File.WriteAllBytesAsync(filePath, new byte[] { 1, 2, 3, 4 });

        try
        {
            using var manager = new AssetManager(
                cacheDirectory: Path.Combine(directory, "cache"),
                nodetoolClient: apiClient);

            var result = await manager.UploadAssetAsync(filePath, "audio/wav");

            var audio = Assert.IsType<AudioRef>(result);
            Assert.Equal("audio-1", audio.AssetId);
            Assert.Equal("http://localhost/assets/audio-1.wav", audio.Uri);
            Assert.Equal("audio/wav", audio.Metadata?["content_type"]);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class TrackingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        public bool IsDisposed { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(responseFactory(request));

        protected override void Dispose(bool disposing)
        {
            IsDisposed = true;
            base.Dispose(disposing);
        }
    }
}
