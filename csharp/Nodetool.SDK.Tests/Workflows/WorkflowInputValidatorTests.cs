using System.Text.Json;
using Nodetool.SDK.Workflows;

namespace Nodetool.SDK.Tests.Workflows;

public sealed class WorkflowInputValidatorTests
{
    [Fact]
    public void Validate_ReportsRequiredBoundsAndUnknownInputs()
    {
        var workflow = Descriptor(
            Input("prompt", required: true),
            Input("count", required: true, minimum: 1, maximum: 4));

        var result = WorkflowInputValidator.Validate(
            workflow,
            new Dictionary<string, object?>
            {
                ["count"] = 7,
                ["future"] = "allowed through"
            });

        Assert.False(result.IsValid);
        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                    WorkflowInputValidationCodes.MissingRequiredInput &&
                issue.InputName == "prompt" &&
                issue.Blocking);
        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                    WorkflowInputValidationCodes.AboveMaximum &&
                issue.InputName == "count" &&
                issue.Blocking);
        Assert.Contains(
            result.Issues,
            issue =>
                issue.Code ==
                    WorkflowInputValidationCodes.UnknownInput &&
                issue.InputName == "future" &&
                !issue.Blocking);
    }

    [Fact]
    public void Validate_AcceptsMissingInputWithConcreteDefault()
    {
        using var document = JsonDocument.Parse("\"hello\"");
        var input = Input("prompt", required: true) with
        {
            DefaultValue = document.RootElement.Clone()
        };

        var result = WorkflowInputValidator.Validate(
            Descriptor(input),
            new Dictionary<string, object?>());

        Assert.True(result.IsValid);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void ValidateOrThrow_DoesNotIncludeInputValues()
    {
        var workflow = Descriptor(
            Input("secret", required: true, minimum: 5));

        var exception = Assert.Throws<WorkflowInputValidationException>(
            () => WorkflowInputValidator.ValidateOrThrow(
                workflow,
                new Dictionary<string, object?>
                {
                    ["secret"] = 1
                }));

        Assert.DoesNotContain("1", exception.Message);
        Assert.Contains("secret", exception.Message);
        Assert.Single(exception.Issues);
    }

    private static WorkflowInputDescriptor Input(
        string name,
        bool required,
        double? minimum = null,
        double? maximum = null)
        => new(
            $"{name}-node",
            name,
            "",
            new WorkflowTypeDescriptor(
                name == "count" ? "int" : "str",
                Optional: false,
                TypeName: null,
                Values: [],
                TypeArguments: []),
            required,
            DefaultValue: null,
            minimum,
            maximum);

    private static WorkflowDescriptor Descriptor(
        params WorkflowInputDescriptor[] inputs)
        => new(
            "workflow-1",
            "Workflow",
            "",
            "revision",
            null,
            null,
            1,
            null,
            "server",
            inputs,
            [],
            []);
}
