using System.Collections;
using System.Globalization;
using System.Text.Json;
using Nodetool.SDK.Types.Assets;

namespace Nodetool.SDK.Values;

public sealed record NodeToolValueConversionError(
    string Code,
    string Message,
    Type TargetType,
    Type? SourceType);

public sealed class NodeToolValueConversionException : InvalidCastException
{
    public NodeToolValueConversionError Error { get; }

    public NodeToolValueConversionException(NodeToolValueConversionError error)
        : base(error.Message)
    {
        Error = error;
    }
}

/// <summary>
/// Host-neutral conversion for NodeTool/JSON value trees. Host adapters remain
/// responsible for native collection, path, image, and UI types.
/// </summary>
public static class NodeToolValueConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool TryConvert(
        object? value,
        Type targetType,
        out object? converted,
        out NodeToolValueConversionError? error)
    {
        try
        {
            converted = Convert(value, targetType);
            error = null;
            return true;
        }
        catch (NodeToolValueConversionException ex)
        {
            converted = null;
            error = ex.Error;
            return false;
        }
    }

    public static object? Convert(object? value, Type targetType)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        value = Normalize(value);

        var nullableType = Nullable.GetUnderlyingType(targetType);
        if (value is null)
        {
            if (!targetType.IsValueType || nullableType is not null)
                return null;
            throw Error("null_not_allowed", value, targetType);
        }

        targetType = nullableType ?? targetType;
        if (targetType == typeof(object))
            return value;
        if (targetType.IsInstanceOfType(value))
            return value;
        if (targetType == typeof(string))
            return value is IDictionary or IEnumerable and not string
                ? JsonSerializer.Serialize(value, JsonOptions)
                : System.Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
        if (targetType == typeof(bool))
            return ConvertBoolean(value, targetType);
        if (IsNumericType(targetType))
            return ConvertNumeric(value, targetType);
        if (targetType.IsEnum)
            return ConvertEnum(value, targetType);
        if (TryGetCollectionElementType(targetType, out var elementType))
            return ConvertCollection(value, targetType, elementType);

        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            return JsonSerializer.Deserialize(json, targetType, JsonOptions)
                ?? throw Error("structured_null", value, targetType);
        }
        catch (NodeToolValueConversionException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Error("structured_conversion_failed", value, targetType, ex);
        }
    }

    /// <summary>
    /// Normalizes arbitrary host collections and SDK asset references into the
    /// primitive/map/list tree accepted by NodeTool transports.
    /// </summary>
    public static object? NormalizeForTransport(object? value)
    {
        value = Normalize(value);
        return value switch
        {
            null => null,
            byte[] bytes => bytes,
            AssetRef asset => NormalizeForTransport(asset.ToDict()),
            IDictionary dictionary => NormalizeDictionary(dictionary),
            IEnumerable enumerable and not string => enumerable
                .Cast<object?>()
                .Select(NormalizeForTransport)
                .ToArray(),
            _ => value
        };
    }

    private static Dictionary<string, object?> NormalizeDictionary(
        IDictionary dictionary)
    {
        var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in dictionary)
        {
            var key = System.Convert.ToString(
                entry.Key,
                CultureInfo.InvariantCulture) ?? "";
            normalized[key] = NormalizeForTransport(entry.Value);
        }
        return normalized;
    }

    private static object? Normalize(object? value)
        => value switch
        {
            NodeToolValue nodeToolValue => nodeToolValue.ToObject(),
            JsonElement json => JsonToObject(json),
            _ => value
        };

    private static object ConvertBoolean(object value, Type targetType)
    {
        if (value is bool boolean)
            return boolean;
        if (value is string text && bool.TryParse(text, out var parsed))
            return parsed;
        throw Error("invalid_boolean", value, targetType);
    }

    private static object ConvertNumeric(object value, Type targetType)
    {
        if (value is string text)
        {
            if (!decimal.TryParse(
                    text,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var parsed))
            {
                throw Error("invalid_number", value, targetType);
            }
            value = parsed;
        }

        try
        {
            if (targetType == typeof(float))
            {
                var number = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (!double.IsFinite(number) ||
                    number is > float.MaxValue or < -float.MaxValue)
                {
                    throw Error("numeric_overflow", value, targetType);
                }
                return (float)number;
            }
            if (targetType == typeof(double))
            {
                var number = System.Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (!double.IsFinite(number))
                    throw Error("non_finite_number", value, targetType);
                return number;
            }
            if (targetType == typeof(decimal))
                return System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);

            var decimalValue = System.Convert.ToDecimal(value, CultureInfo.InvariantCulture);
            if (decimal.Truncate(decimalValue) != decimalValue)
                throw Error("fractional_integer", value, targetType);

            return targetType == typeof(byte) ? checked((byte)decimalValue) :
                targetType == typeof(sbyte) ? checked((sbyte)decimalValue) :
                targetType == typeof(short) ? checked((short)decimalValue) :
                targetType == typeof(ushort) ? checked((ushort)decimalValue) :
                targetType == typeof(int) ? checked((int)decimalValue) :
                targetType == typeof(uint) ? checked((uint)decimalValue) :
                targetType == typeof(long) ? checked((long)decimalValue) :
                targetType == typeof(ulong) ? checked((ulong)decimalValue) :
                throw Error("unsupported_number", value, targetType);
        }
        catch (NodeToolValueConversionException)
        {
            throw;
        }
        catch (OverflowException ex)
        {
            throw Error("numeric_overflow", value, targetType, ex);
        }
        catch (Exception ex)
        {
            throw Error("invalid_number", value, targetType, ex);
        }
    }

    private static object ConvertEnum(object value, Type targetType)
    {
        try
        {
            return value is string text
                ? Enum.Parse(targetType, text, ignoreCase: true)
                : Enum.ToObject(
                    targetType,
                    ConvertNumeric(value, Enum.GetUnderlyingType(targetType)));
        }
        catch (Exception ex)
        {
            throw Error("invalid_enum", value, targetType, ex);
        }
    }

    private static object ConvertCollection(
        object value,
        Type targetType,
        Type elementType)
    {
        if (value is not IEnumerable enumerable || value is string)
            enumerable = new[] { value };

        var values = enumerable.Cast<object?>().ToArray();
        var array = Array.CreateInstance(elementType, values.Length);
        for (var index = 0; index < values.Length; index++)
        {
            try
            {
                array.SetValue(Convert(values[index], elementType), index);
            }
            catch (NodeToolValueConversionException ex)
            {
                throw new NodeToolValueConversionException(ex.Error with
                {
                    Code = "collection_item_" + ex.Error.Code,
                    Message = $"Collection item {index}: {ex.Error.Message}"
                });
            }
        }

        if (targetType.IsArray || targetType.IsAssignableFrom(array.GetType()))
            return array;

        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)(Activator.CreateInstance(listType)
            ?? throw Error("collection_creation_failed", value, targetType));
        foreach (var item in array)
            list.Add(item);
        if (targetType.IsAssignableFrom(listType))
            return list;

        throw Error("unsupported_collection", value, targetType);
    }

    private static bool TryGetCollectionElementType(
        Type type,
        out Type elementType)
    {
        if (type.IsArray && type.GetElementType() is { } arrayElement)
        {
            elementType = arrayElement;
            return true;
        }

        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() is var definition &&
            (definition == typeof(List<>) ||
             definition == typeof(IReadOnlyList<>) ||
             definition == typeof(IEnumerable<>) ||
             definition == typeof(ICollection<>)))
        {
            elementType = type.GetGenericArguments()[0];
            return true;
        }

        elementType = typeof(object);
        return false;
    }

    private static bool IsNumericType(Type type)
        => Type.GetTypeCode(type) is
            TypeCode.Byte or TypeCode.SByte or
            TypeCode.Int16 or TypeCode.UInt16 or
            TypeCode.Int32 or TypeCode.UInt32 or
            TypeCode.Int64 or TypeCode.UInt64 or
            TypeCode.Single or TypeCode.Double or TypeCode.Decimal;

    private static object? JsonToObject(JsonElement element)
        => element.ValueKind switch
        {
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(
                property => property.Name,
                property => JsonToObject(property.Value),
                StringComparer.Ordinal),
            JsonValueKind.Array => element.EnumerateArray()
                .Select(JsonToObject)
                .ToArray(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt32(out var integer) => integer,
            JsonValueKind.Number when element.TryGetInt64(out var longInteger) => longInteger,
            JsonValueKind.Number when element.TryGetDecimal(out var decimalNumber) => decimalNumber,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => element.ToString()
        };

    private static NodeToolValueConversionException Error(
        string code,
        object? value,
        Type targetType,
        Exception? inner = null)
        => new(new NodeToolValueConversionError(
            code,
            inner is null
                ? $"Cannot convert {value?.GetType().Name ?? "null"} to {targetType.Name}."
                : $"Cannot convert {value?.GetType().Name ?? "null"} to {targetType.Name}: {inner.Message}",
            targetType,
            value?.GetType()));
}
