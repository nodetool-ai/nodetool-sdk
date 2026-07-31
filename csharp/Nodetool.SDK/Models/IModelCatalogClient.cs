using Nodetool.SDK.Api.Models;

namespace Nodetool.SDK.Models;

public interface IModelCatalogClient
{
    Task<SdkModelCatalogResponse> GetModelCatalogAsync(
        SdkModelCatalogQuery? query = null,
        CancellationToken cancellationToken = default);
}
