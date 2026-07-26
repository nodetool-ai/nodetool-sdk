namespace Nodetool.SDK.Connection;

/// <summary>
/// Supplies a bearer token at the point a NodeTool connection is opened.
/// Implementations may read secure host storage or refresh an expiring token.
/// </summary>
public interface INodeToolTokenProvider
{
    ValueTask<string?> GetTokenAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Token provider for a fixed token supplied by a host application.
/// </summary>
public sealed class StaticNodeToolTokenProvider(string? token) :
    INodeToolTokenProvider
{
    private readonly string? _token =
        string.IsNullOrWhiteSpace(token) ? null : token.Trim();

    public ValueTask<string?> GetTokenAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_token);
    }
}
