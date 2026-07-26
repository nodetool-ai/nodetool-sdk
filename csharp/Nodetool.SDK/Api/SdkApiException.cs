using System.Net;

namespace Nodetool.SDK.Api;

public enum SdkApiTransport
{
    Http,
    WebSocket
}

public sealed class SdkApiException : InvalidOperationException
{
    public SdkApiException(
        HttpStatusCode statusCode,
        string? apiCode,
        bool retryable,
        string message)
        : base(message)
    {
        Transport = SdkApiTransport.Http;
        StatusCode = statusCode;
        ApiCode = apiCode;
        Retryable = retryable;
    }

    public SdkApiException(
        SdkApiTransport transport,
        string? apiCode,
        bool retryable,
        string message)
        : base(message)
    {
        Transport = transport;
        ApiCode = apiCode;
        Retryable = retryable;
    }

    public SdkApiTransport Transport { get; }

    public HttpStatusCode? StatusCode { get; }

    public string? ApiCode { get; }

    public bool Retryable { get; }
}
