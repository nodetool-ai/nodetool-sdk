using System.Net;

namespace Nodetool.SDK.Api;

public sealed class WorkflowInterfaceUnavailableException : InvalidOperationException
{
    public WorkflowInterfaceUnavailableException(
        HttpStatusCode statusCode,
        string? apiCode,
        string message)
        : base(message)
    {
        StatusCode = statusCode;
        ApiCode = apiCode;
    }

    public HttpStatusCode StatusCode { get; }

    public string? ApiCode { get; }
}
