using System;
using Microsoft.Extensions.Logging;
using Nodetool.SDK.Diagnostics;

namespace Nodetool.SDK.VL.Utilities;

internal static class VlLog
{
    private const string Prefix = "Nodetool.SDK.VL:";
    private const string Category = "VL.Nodetool";
    private static ILogger? _logger;

    public static void Configure(ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);
        Volatile.Write(
            ref _logger,
            loggerFactory.CreateLogger(Category));
    }

    public static bool Verbose =>
        string.Equals(Environment.GetEnvironmentVariable("NODETOOL_VL_VERBOSE"), "1", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Environment.GetEnvironmentVariable("NODETOOL_VL_VERBOSE"), "true", StringComparison.OrdinalIgnoreCase);

    public static void Info(string message)
    {
        var rendered = $"{Prefix} {message}";
        var logger = Volatile.Read(ref _logger);
        if (logger != null)
            logger.LogInformation("{Message}", rendered);
        else
            Console.WriteLine(rendered);
    }

    public static void Debug(string message)
    {
        if (Verbose)
            Info(message);
    }

    public static void Error(string message)
    {
        var rendered = $"{Prefix} ERROR: {message}";
        var logger = Volatile.Read(ref _logger);
        if (logger != null)
            logger.LogError("{Message}", rendered);
        else
            Console.WriteLine(rendered);
    }

    public static string SafeError(
        Exception exception,
        params string?[] knownSecrets)
        => NodeToolDiagnosticRedactor.RedactText(
            exception.Message,
            knownSecrets);
}


