using Nodetool.SDK.VL.Services;
using Nodetool.SDK.Execution;
using Xunit;

namespace Nodetool.SDK.VL.UnitTests;

public sealed class VlNodeToolHostSettingsTests
{
    [Fact]
    public void Instances_KeepConnectionAndExecutionSettingsIsolated()
    {
        var first = new VlNodeToolHostSettings();
        var second = new VlNodeToolHostSettings();

        first.Configure(
            "wss://first.example/root",
            "first-token");
        first.SetAutoReconnect(false);
        first.SetExecutionTimeoutSeconds(17);
        first.SetInlineMediaLimitBytes(2048);
        first.SetUseWebSocketDiscovery(true);
        first.SetLoadNodes(false);
        first.SetShowAllNodes(true);
        first.SetLoadWorkflows(false);
        first.SetWorkflowPersistence(WorkflowPersistence.Session);
        first.SetWorkflowEventDetail(WorkflowEventDetail.Outputs);
        first.SetWorkflowAssetPersistence(
            WorkflowAssetPersistence.Temporary);

        Assert.Equal(
            "wss://first.example/root",
            first.WorkerUrl);
        Assert.Equal(
            new Uri("https://first.example/root/"),
            first.ApiBaseUrl);
        Assert.Equal(17, first.ExecutionTimeoutSeconds);
        Assert.Equal(2048, first.InlineMediaLimitBytes);
        Assert.False(first.AutoReconnect);
        Assert.True(first.UseWebSocketDiscovery);
        Assert.False(first.LoadNodes);
        Assert.True(first.ShowAllNodes);
        Assert.False(first.LoadWorkflows);
        Assert.Equal(
            new WorkflowExecutionOptions(
                WorkflowPersistence.Session,
                WorkflowEventDetail.Outputs,
                WorkflowAssetPersistence.Temporary),
            first.ExecutionOptions);

        Assert.Equal(
            "ws://localhost:7777",
            second.WorkerUrl);
        Assert.Equal(
            NodeToolClientProvider.DefaultExecutionTimeoutSeconds,
            second.ExecutionTimeoutSeconds);
        Assert.True(second.AutoReconnect);
        Assert.False(second.UseWebSocketDiscovery);
        Assert.True(second.LoadNodes);
        Assert.False(second.ShowAllNodes);
        Assert.True(second.LoadWorkflows);
        Assert.Equal(
            new WorkflowExecutionOptions(),
            second.ExecutionOptions);
    }

    [Fact]
    public void InvalidEndpoint_DoesNotReplaceLastValidSettings()
    {
        var settings = new VlNodeToolHostSettings();
        settings.Configure(
            "https://valid.example/nodetool",
            null);

        Assert.Throws<ArgumentException>(
            () => settings.Configure(
                "file:///tmp/not-a-server",
                "secret"));

        Assert.Equal(
            "https://valid.example/nodetool",
            settings.WorkerUrl);
        Assert.Null(settings.ApiKey);
    }
}
