using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Nodetool.SDK.Utilities.Execution;

/// <summary>
/// Computes a cheap, stable "signature" string for a set of inputs.
/// Useful for "execute on change" style scheduling in UI hosts (VL/WPF/etc.).
/// </summary>
public static class InputSignature
{
    public static string Compute(
        IReadOnlyDictionary<string, object?> inputs,
        IEnumerable<string>? excludeKeys = null)
    {
        if (inputs == null) throw new ArgumentNullException(nameof(inputs));

        var exclude = excludeKeys != null
            ? new HashSet<string>(excludeKeys, StringComparer.Ordinal)
            : null;

        var sb = new StringBuilder();
        foreach (var kvp in inputs.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            if (exclude != null && exclude.Contains(kvp.Key))
                continue;

            sb.Append(kvp.Key);
            sb.Append('=');
            sb.Append(ValueFragment(kvp.Value));
            sb.Append(';');
        }
        return sb.ToString();
    }

    public static string ValueFragment(object? value)
    {
        if (value == null) return "null";

        switch (value)
        {
            case string s:
                return $"str:{s}";
            case bool b:
                return b ? "bool:true" : "bool:false";
            case int i:
                return $"int:{i}";
            case long l:
                return $"long:{l}";
            case float f:
                return $"float:{f.ToString(CultureInfo.InvariantCulture)}";
            case double d:
                return $"double:{d.ToString(CultureInfo.InvariantCulture)}";
            case decimal m:
                return $"decimal:{m.ToString(CultureInfo.InvariantCulture)}";
            case byte[] bytes:
                unchecked
                {
                    int hash = 17;
                    for (int i = 0; i < Math.Min(bytes.Length, 64); i++)
                        hash = (hash * 31) + bytes[i];
                    return $"bytes:{bytes.Length}:{hash}";
                }
        }

        if (value is System.Collections.IEnumerable enumerable && value is not string)
        {
            // Sample a few items to avoid enumerating huge sequences.
            var items = new List<string>();
            int count = 0;
            foreach (var item in enumerable)
            {
                items.Add(ValueFragment(item));
                count++;
                if (count >= 10)
                    break;
            }
            return $"seq:{value.GetType().FullName}:{string.Join(",", items)}";
        }

        return $"{value.GetType().FullName}:{value}";
    }
}


