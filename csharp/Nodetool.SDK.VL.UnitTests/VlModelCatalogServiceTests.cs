using Nodetool.SDK.VL.Services;
using Xunit;

namespace Nodetool.SDK.VL.UnitTests;

public sealed class VlModelCatalogServiceTests
{
    [Fact]
    public void CacheScope_IsolatesApiKeysWithoutEmbeddingSecrets()
    {
        var endpoint = new Uri("https://models.example.test/");

        var first = VlModelCatalogService.CreateCacheScope(
            endpoint,
            "secret-one",
            "local");
        var second = VlModelCatalogService.CreateCacheScope(
            endpoint,
            "secret-two",
            "local");

        Assert.NotEqual(first, second);
        Assert.DoesNotContain("secret-one", first, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-two", second, StringComparison.Ordinal);
        Assert.EndsWith("|local", first, StringComparison.Ordinal);
    }

    [Fact]
    public void CacheScope_IsolatesExecutionTargets()
    {
        var endpoint = new Uri("https://models.example.test/");

        var local = VlModelCatalogService.CreateCacheScope(
            endpoint,
            null,
            "local");
        var worker = VlModelCatalogService.CreateCacheScope(
            endpoint,
            null,
            "worker");

        Assert.NotEqual(local, worker);
    }
}
