using System.Net;
using System.Net.Http.Headers;
using Nodetool.SDK.Api.Models;
using Nodetool.SDK.Assets;
using Nodetool.SDK.Types.Assets;
using Xunit;

namespace Nodetool.SDK.Tests.Assets;

public class AssetMaterializerTests
{
    [Fact]
    public async Task InlineAudioBytes_AreWrittenAndReusedFromCache()
    {
        var cacheDirectory = CreateTemporaryDirectory();
        try
        {
            var asset = new AudioRef
            {
                Data = new byte[] { 82, 73, 70, 70 },
                Metadata = new Dictionary<string, object?>
                {
                    ["content_type"] = "audio/wav"
                }
            };

            var materializer = new AssetMaterializer(cacheDirectory: cacheDirectory);
            var first = await materializer.MaterializeAsync(asset);
            var second = await materializer.MaterializeAsync(asset);

            Assert.EndsWith(".wav", first.Path, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(new byte[] { 82, 73, 70, 70 }, await File.ReadAllBytesAsync(first.Path));
            Assert.False(first.FromCache);
            Assert.True(second.FromCache);
            Assert.Equal(first.Path, second.Path);
        }
        finally
        {
            Directory.Delete(cacheDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ExistingLocalDocument_IsPassedThroughWithoutCopy()
    {
        var directory = CreateTemporaryDirectory();
        var sourcePath = Path.Combine(directory, "fixture.pdf");
        await File.WriteAllBytesAsync(sourcePath, new byte[] { 37, 80, 68, 70 });
        try
        {
            var materializer = new AssetMaterializer(
                cacheDirectory: Path.Combine(directory, "cache"));
            var result = await materializer.MaterializeAsync(
                new DocumentRef { Uri = sourcePath });

            Assert.Equal(Path.GetFullPath(sourcePath), result.Path);
            Assert.Equal("application/pdf", result.ContentType);
            Assert.False(result.FromCache);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task TextData_IsWrittenAsUtf8InsteadOfTreatedAsBase64()
    {
        var cacheDirectory = CreateTemporaryDirectory();
        try
        {
            var materializer = new AssetMaterializer(cacheDirectory: cacheDirectory);
            var result = await materializer.MaterializeAsync(
                new TextRef { Data = "test" });

            Assert.EndsWith(".txt", result.Path, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("test", await File.ReadAllTextAsync(result.Path));
            Assert.Equal("text/plain", result.ContentType);
        }
        finally
        {
            Directory.Delete(cacheDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task DataUriInUri_IsMaterializedWithoutNetworkAccess()
    {
        var cacheDirectory = CreateTemporaryDirectory();
        try
        {
            var materializer = new AssetMaterializer(cacheDirectory: cacheDirectory);
            var result = await materializer.MaterializeAsync(
                new DocumentRef
                {
                    Uri = "data:application/pdf;base64,JVBERg=="
                });

            Assert.EndsWith(".pdf", result.Path, StringComparison.OrdinalIgnoreCase);
            Assert.Equal("%PDF", await File.ReadAllTextAsync(result.Path));
            Assert.Equal("application/pdf", result.ContentType);
        }
        finally
        {
            Directory.Delete(cacheDirectory, recursive: true);
        }
    }

    [Fact]
    public void StoredAssetUri_MapsToCurrentStorageEndpoint()
    {
        var materializer = new AssetMaterializer(
            apiBaseUrl: new Uri("http://127.0.0.1:7777"));
        var uri = materializer.ResolveStoredAssetUri("asset://clip.webm");

        Assert.NotNull(uri);
        Assert.Equal("/api/storage/clip.webm", uri.AbsolutePath);
    }

    [Fact]
    public async Task AssetId_UsesInjectedResolverAndAuthenticatesSameOriginDownload()
    {
        var cacheDirectory = CreateTemporaryDirectory();
        HttpRequestMessage? capturedRequest = null;
        try
        {
            using var httpClient = new HttpClient(new DelegateHttpMessageHandler(request =>
            {
                capturedRequest = request;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 82, 73, 70, 70 })
                    {
                        Headers = { ContentType = new MediaTypeHeaderValue("audio/wav") }
                    }
                };
            }));
            var materializer = new AssetMaterializer(
                resolveAsset: (id, _) => Task.FromResult<AssetResponse?>(
                    new AssetResponse
                    {
                        Id = id,
                        Name = "clip.wav",
                        ContentType = "audio/wav",
                        GetUrl = "/api/storage/clip.wav"
                    }),
                apiBaseUrl: new Uri("http://127.0.0.1:7777"),
                authToken: "test-token",
                httpClient: httpClient,
                cacheDirectory: cacheDirectory);

            var result = await materializer.MaterializeAsync(
                new AudioRef { AssetId = "asset-1" });

            Assert.NotNull(capturedRequest);
            Assert.Equal(
                "http://127.0.0.1:7777/api/storage/clip.wav",
                capturedRequest.RequestUri?.ToString());
            Assert.Equal("Bearer", capturedRequest.Headers.Authorization?.Scheme);
            Assert.Equal("test-token", capturedRequest.Headers.Authorization?.Parameter);
            Assert.Equal("audio/wav", result.ContentType);
            Assert.Equal(new byte[] { 82, 73, 70, 70 }, await File.ReadAllBytesAsync(result.Path));
        }
        finally
        {
            Directory.Delete(cacheDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task CrossOriginDownload_DoesNotForwardConnectionToken()
    {
        var cacheDirectory = CreateTemporaryDirectory();
        AuthenticationHeaderValue? capturedAuthorization = null;
        try
        {
            using var httpClient = new HttpClient(new DelegateHttpMessageHandler(request =>
            {
                capturedAuthorization = request.Headers.Authorization;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(new byte[] { 1, 2, 3 })
                };
            }));
            var materializer = new AssetMaterializer(
                apiBaseUrl: new Uri("https://nodetool.example"),
                authToken: "must-not-leak",
                httpClient: httpClient,
                cacheDirectory: cacheDirectory);

            await materializer.MaterializeAsync(
                new GenericAssetRef { Uri = "https://cdn.example/file.bin" });

            Assert.Null(capturedAuthorization);
        }
        finally
        {
            Directory.Delete(cacheDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ForceRefresh_ReplacesCachedHttpContentAtomically()
    {
        var cacheDirectory = CreateTemporaryDirectory();
        var responses = new Queue<byte[]>(
        [
            new byte[] { 1, 2, 3 },
            new byte[] { 4, 5, 6 }
        ]);
        try
        {
            using var httpClient = new HttpClient(new DelegateHttpMessageHandler(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new ByteArrayContent(responses.Dequeue())
                }));
            var materializer = new AssetMaterializer(
                httpClient: httpClient,
                cacheDirectory: cacheDirectory);
            var asset = new GenericAssetRef
            {
                Uri = "https://cdn.example/file.bin"
            };

            var first = await materializer.MaterializeAsync(asset);
            var cached = await materializer.MaterializeAsync(asset);
            var refreshed = await materializer.MaterializeAsync(
                asset,
                forceRefresh: true);

            Assert.False(first.FromCache);
            Assert.True(cached.FromCache);
            Assert.False(refreshed.FromCache);
            Assert.Equal(first.Path, refreshed.Path);
            Assert.Equal(new byte[] { 4, 5, 6 }, await File.ReadAllBytesAsync(refreshed.Path));
            Assert.Empty(Directory.EnumerateFiles(cacheDirectory, "*.tmp"));
            Assert.Empty(responses);
        }
        finally
        {
            Directory.Delete(cacheDirectory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "nodetool-sdk-tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class DelegateHttpMessageHandler(
        Func<HttpRequestMessage, HttpResponseMessage> handler)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
            => Task.FromResult(handler(request));
    }
}
