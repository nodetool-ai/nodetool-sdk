namespace Nodetool.SDK.Configuration;

/// <summary>
/// Constants for Nodetool API endpoints and fixed values
/// </summary>
public static class NodetoolConstants
{
    /// <summary>
    /// API endpoint paths
    /// </summary>
    public static class Endpoints
    {
        // Node operations
        public const string NodesMetadata = "/api/nodes/metadata";
        public const string NodeExecute = "/api/nodes/execute"; // NOT IMPLEMENTED
        
        // Workflow operations  
        public const string Workflows = "/api/workflows/";
        public const string WorkflowById = "/api/workflows/{0}";
        public const string WorkflowRun = "/api/workflows/{0}/run";
        public const string WorkflowRunSyncPlus = "/api/workflows/{0}/run_sync_plus_fetch_outputs";
        
        // Asset operations
        public const string Assets = "/api/assets/";
        public const string AssetById = "/api/assets/{0}";
        public const string AssetDownload = "/api/assets/{0}/download";
        
        // Job operations
        public const string JobById = "/api/jobs/{0}";
        public const string JobCancel = "/api/jobs/{0}/cancel";
    }
    
    /// <summary>
    /// Default configuration values
    /// </summary>
    public static class Defaults
    {
        public const string BaseUrl = "http://localhost:7777";
        public const int TimeoutSeconds = 30;
        public const string UserAgent = "Nodetool.SDK/1.0";
        public const int CacheValidTimeMinutes = 5;
    }
    
    /// <summary>
    /// HTTP content types
    /// </summary>
    public static class ContentTypes
    {
        public const string Json = "application/json";
        public const string MultipartFormData = "multipart/form-data";
    }
    
    /// <summary>
    /// Schema and type constants
    /// </summary>
    public static class Schema
    {
        public const string ObjectType = "object";
        public const string ArrayType = "array";
        public const string StringType = "string";
        public const string IntegerType = "integer";
        public const string NumberType = "number";
        public const string BooleanType = "boolean";
    }
} 