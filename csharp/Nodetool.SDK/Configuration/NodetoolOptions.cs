namespace Nodetool.SDK.Configuration;

/// <summary>
/// Configuration options for the Nodetool SDK
/// </summary>
public class NodetoolOptions
{
    /// <summary>
    /// Base URL for the Nodetool API
    /// </summary>
    public string BaseUrl { get; set; } = NodetoolConstants.Defaults.BaseUrl;
    
    /// <summary>
    /// API key for authentication (optional)
    /// </summary>
    public string? ApiKey { get; set; }
    
    /// <summary>
    /// HTTP client timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = NodetoolConstants.Defaults.TimeoutSeconds;
    
    /// <summary>
    /// User agent string for HTTP requests
    /// </summary>
    public string UserAgent { get; set; } = NodetoolConstants.Defaults.UserAgent;
    
    /// <summary>
    /// Cache validity time in minutes for metadata requests
    /// </summary>
    public int CacheValidTimeMinutes { get; set; } = NodetoolConstants.Defaults.CacheValidTimeMinutes;
    
    /// <summary>
    /// Enable detailed logging for debugging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
    
    /// <summary>
    /// Maximum number of concurrent requests
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;
    
    /// <summary>
    /// Retry configuration
    /// </summary>
    public RetryOptions Retry { get; set; } = new();
}

/// <summary>
/// Retry configuration for failed requests
/// </summary>
public class RetryOptions
{
    /// <summary>
    /// Maximum number of retry attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 3;
    
    /// <summary>
    /// Base delay between retries in milliseconds
    /// </summary>
    public int BaseDelayMs { get; set; } = 1000;
    
    /// <summary>
    /// Maximum delay between retries in milliseconds
    /// </summary>
    public int MaxDelayMs { get; set; } = 10000;
    
    /// <summary>
    /// Enable exponential backoff
    /// </summary>
    public bool UseExponentialBackoff { get; set; } = true;
} 