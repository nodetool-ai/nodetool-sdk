using System.Globalization;
using System.Text.Json;

namespace Nodetool.SDK.Workflows;

public static class WorkflowInputValidationCodes
{
    public const string MissingRequiredInput = "missing_required_input";
    public const string NullRequiredInput = "null_required_input";
    public const string BelowMinimum = "input_below_minimum";
    public const string AboveMaximum = "input_above_maximum";
    public const string UnknownInput = "unknown_input";
}

public sealed record WorkflowInputValidationIssue(
    string Code,
    string InputName,
    string Message,
    bool Blocking);

public sealed record WorkflowInputValidationResult(
    IReadOnlyList<WorkflowInputValidationIssue> Issues)
{
    public bool IsValid => Issues.All(issue => !issue.Blocking);
}

public sealed class WorkflowInputValidationException(
    IReadOnlyList<WorkflowInputValidationIssue> issues)
    : ArgumentException(CreateMessage(issues), "inputs")
{
    public IReadOnlyList<WorkflowInputValidationIssue> Issues { get; } =
        issues;

    private static string CreateMessage(
        IReadOnlyList<WorkflowInputValidationIssue> issues)
    {
        var blocking = issues
            .Where(issue => issue.Blocking)
            .Select(issue => $"{issue.InputName}: {issue.Code}")
            .ToArray();
        return blocking.Length == 0
            ? "Workflow inputs are invalid."
            : $"Workflow inputs are invalid ({string.Join(", ", blocking)}).";
    }
}

/// <summary>
/// Performs fast, conservative checks using the graph-derived workflow
/// descriptor. Server preflight remains authoritative.
/// </summary>
public static class WorkflowInputValidator
{
    public static WorkflowInputValidationResult Validate(
        WorkflowDescriptor workflow,
        IReadOnlyDictionary<string, object?> inputs)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(inputs);

        var issues = new List<WorkflowInputValidationIssue>();
        var descriptors = workflow.Inputs.ToDictionary(
            input => input.Name,
            StringComparer.Ordinal);

        foreach (var input in workflow.Inputs)
        {
            if (!inputs.TryGetValue(input.Name, out var value))
            {
                if (input.Required &&
                    !HasUsableDefault(input.DefaultValue))
                {
                    issues.Add(new(
                        WorkflowInputValidationCodes.MissingRequiredInput,
                        input.Name,
                        $"Required input '{input.Name}' is missing.",
                        Blocking: true));
                }
                continue;
            }

            if (value is null)
            {
                if (input.Required && !input.Type.Optional)
                {
                    issues.Add(new(
                        WorkflowInputValidationCodes.NullRequiredInput,
                        input.Name,
                        $"Required input '{input.Name}' cannot be null.",
                        Blocking: true));
                }
                continue;
            }

            if (!TryConvertFiniteNumber(value, out var number))
                continue;
            if (input.Minimum is { } minimum && number < minimum)
            {
                issues.Add(new(
                    WorkflowInputValidationCodes.BelowMinimum,
                    input.Name,
                    $"Input '{input.Name}' is below its minimum.",
                    Blocking: true));
            }
            if (input.Maximum is { } maximum && number > maximum)
            {
                issues.Add(new(
                    WorkflowInputValidationCodes.AboveMaximum,
                    input.Name,
                    $"Input '{input.Name}' is above its maximum.",
                    Blocking: true));
            }
        }

        foreach (var name in inputs.Keys)
        {
            if (!descriptors.ContainsKey(name))
            {
                issues.Add(new(
                    WorkflowInputValidationCodes.UnknownInput,
                    name,
                    $"Input '{name}' is not present in the current workflow interface.",
                    Blocking: false));
            }
        }

        return new WorkflowInputValidationResult(issues);
    }

    public static void ValidateOrThrow(
        WorkflowDescriptor workflow,
        IReadOnlyDictionary<string, object?> inputs)
    {
        var result = Validate(workflow, inputs);
        if (!result.IsValid)
            throw new WorkflowInputValidationException(result.Issues);
    }

    private static bool HasUsableDefault(JsonElement? value)
        => value is { ValueKind: not (
            JsonValueKind.Undefined or
            JsonValueKind.Null) };

    private static bool TryConvertFiniteNumber(
        object value,
        out double number)
    {
        if (value is bool)
        {
            number = 0;
            return false;
        }
        try
        {
            number = Convert.ToDouble(
                value,
                CultureInfo.InvariantCulture);
            return double.IsFinite(number);
        }
        catch (Exception exception) when (
            exception is FormatException or
            InvalidCastException or
            OverflowException)
        {
            number = 0;
            return false;
        }
    }
}
