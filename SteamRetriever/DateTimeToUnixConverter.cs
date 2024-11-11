using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace SteamRetriever;

internal class DateTimeToUnixMillisecondsConverter : DateTimeConverterBase
{
    internal static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static void ArgumentNotNull([NotNull] object? value, string parameterName)
    {
        if (value == null)
        {
            throw new ArgumentNullException(parameterName);
        }
    }

    public static bool IsNullableType(Type t)
    {
        ArgumentNotNull(t, nameof(t));

        return (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
    }

    public static bool IsNullable(Type t)
    {
        ArgumentNotNull(t, nameof(t));

        if (t.IsValueType)
        {
            return IsNullableType(t);
        }

        return true;
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        bool nullable = IsNullable(objectType);
        if (reader.TokenType == JsonToken.Null)
        {
            if (!nullable)
                throw new JsonSerializationException($"Cannot convert null value to {objectType}.");

            return 0;
        }

        long milliSeconds;

        if (reader.TokenType == JsonToken.Integer)
        {
            milliSeconds = (long)reader.Value!;
        }
        else if (reader.TokenType == JsonToken.String)
        {
            if (!long.TryParse((string)reader.Value!, out milliSeconds))
                throw new JsonSerializationException($"Cannot convert invalid value to {objectType}.");
        }
        else
        {
            throw new JsonSerializationException($"Unexpected token parsing date. Expected Integer or String, got {reader.TokenType}.");
        }

        if (milliSeconds >= 0)
        {
            DateTime d = UnixEpoch.AddMilliseconds(milliSeconds);

            Type t = (nullable)
                ? Nullable.GetUnderlyingType(objectType)
                : objectType;
            if (t == typeof(DateTimeOffset))
                return new DateTimeOffset(d, TimeSpan.Zero);

            return d;
        }
        else
        {
            throw new JsonSerializationException($"Cannot convert value that is before Unix epoch of 00:00:00 UTC on 1 January 1970 to {objectType}.");
        }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        long milliSeconds;

        if (value is DateTime dateTime)
        {
            milliSeconds = (long)(dateTime.ToUniversalTime() - UnixEpoch).TotalMilliseconds;
        }
        else if (value is DateTimeOffset dateTimeOffset)
        {
            milliSeconds = (long)(dateTimeOffset.ToUniversalTime() - UnixEpoch).TotalMilliseconds;
        }
        else
        {
            throw new JsonSerializationException("Expected date object value.");
        }

        if (milliSeconds < 0)
        {
            throw new JsonSerializationException("Cannot convert date value that is before Unix epoch of 00:00:00 UTC on 1 January 1970.");
        }

        writer.WriteValue(milliSeconds);
    }
}
