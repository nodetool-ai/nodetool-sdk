using Nodetool.SDK.Api;
using Nodetool.SDK.Assets;

namespace Nodetool.SDK.Workflows;

/// <summary>
/// Prepares one workflow invocation using a portable connection profile.
/// Hosts only supply an optional adapter for engine-specific media objects.
/// </summary>
public sealed class WorkflowInputPreparationService
{
    private readonly Uri? _apiBaseUrl;
    private readonly string? _authToken;
    private readonly long _inlineMediaLimitBytes;
    private readonly HttpClient? _httpClient;
    private readonly bool _useTemporaryAssetUploads;
    private readonly Func<
        string,
        string,
        object?,
        CancellationToken,
        ValueTask<object?>>? _adaptHostMediaValue;

    public WorkflowInputPreparationService(
        Uri? apiBaseUrl = null,
        string? authToken = null,
        long inlineMediaLimitBytes =
            MediaInputPreparer.DefaultInlineLimitBytes,
        HttpClient? httpClient = null,
        bool useTemporaryAssetUploads = false,
        Func<
            string,
            string,
            object?,
            CancellationToken,
            ValueTask<object?>>? adaptHostMediaValue = null)
    {
        if (inlineMediaLimitBytes < 0)
            throw new ArgumentOutOfRangeException(
                nameof(inlineMediaLimitBytes));
        _apiBaseUrl = apiBaseUrl;
        _authToken = authToken;
        _inlineMediaLimitBytes = inlineMediaLimitBytes;
        _httpClient = httpClient;
        _useTemporaryAssetUploads = useTemporaryAssetUploads;
        _adaptHostMediaValue = adaptHostMediaValue;
    }

    public async Task<Dictionary<string, object>> PrepareAsync(
        WorkflowDescriptor workflow,
        IReadOnlyDictionary<string, object?> inputs,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(inputs);

        NodetoolClient? apiClient = null;
        AssetUploader? assetUploader = null;
        try
        {
            if (_apiBaseUrl != null &&
                workflow.Inputs.Any(input =>
                    WorkflowInputPreparer.ContainsMedia(input.Type)))
            {
                apiClient = new NodetoolClient(
                    _apiBaseUrl,
                    _authToken,
                    _httpClient);
                assetUploader = new AssetUploader(
                    apiClient,
                    useTemporaryUploads: _useTemporaryAssetUploads);
            }

            return await new WorkflowInputPreparer(
                    new MediaInputPreparer(
                        assetUploader,
                        _inlineMediaLimitBytes),
                    _adaptHostMediaValue)
                .PrepareAsync(
                    workflow,
                    inputs,
                    cancellationToken);
        }
        finally
        {
            apiClient?.Dispose();
        }
    }
}
