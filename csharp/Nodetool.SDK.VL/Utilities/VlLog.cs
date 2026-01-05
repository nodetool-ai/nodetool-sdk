using System;

namespace Nodetool.SDK.VL.Utilities;

internal static class VlLog
{
    private const string Prefix = "Nodetool.SDK.VL:";

    public static bool Verbose =>
        string.Equals(Environment.GetEnvironmentVariable("NODETOOL_VL_VERBOSE"), "1", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(Environment.GetEnvironmentVariable("NODETOOL_VL_VERBOSE"), "true", StringComparison.OrdinalIgnoreCase);

    public static void Info(string message)
        => Console.WriteLine($"{Prefix} {message}");

    public static void Debug(string message)
    {
        if (Verbose)
            Info(message);
    }

    public static void Error(string message)
        => Console.WriteLine($"{Prefix} ERROR: {message}");
}


