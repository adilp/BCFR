using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using MemberOrgApi.Helpers;

namespace MemberOrgApi.Converters;

/// <summary>
/// JSON converter that handles DateTime serialization/deserialization
/// with Central Time Zone awareness
/// </summary>
public class CentralTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        if (string.IsNullOrEmpty(dateString))
        {
            return DateTime.MinValue;
        }

        // Parse the incoming date
        if (DateTime.TryParse(dateString, out var parsed))
        {
            // If the date has no timezone info, assume it's in Central Time
            if (parsed.Kind == DateTimeKind.Unspecified)
            {
                // Convert Central Time to UTC for storage
                return DateTimeHelper.ConvertCentralToUtc(parsed);
            }
            
            // If it's already UTC, keep it as is
            if (parsed.Kind == DateTimeKind.Utc)
            {
                return parsed;
            }
            
            // If it's local, convert to UTC
            return parsed.ToUniversalTime();
        }

        throw new JsonException($"Unable to parse date: {dateString}");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        // Always store as UTC internally, but format for Central Time display
        if (value.Kind != DateTimeKind.Utc)
        {
            value = value.ToUniversalTime();
        }
        
        // Convert to Central Time for client display
        var centralTime = DateTimeHelper.ConvertUtcToCentral(value);
        
        // Write in ISO format without timezone offset (client assumes Central)
        writer.WriteStringValue(centralTime.ToString("yyyy-MM-dd'T'HH:mm:ss"));
    }
}

/// <summary>
/// JSON converter for DateOnly fields
/// </summary>
public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        if (string.IsNullOrEmpty(dateString))
        {
            return DateOnly.MinValue;
        }

        return DateTimeHelper.ParseDateOnly(dateString);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
    }
}

/// <summary>
/// JSON converter for nullable DateOnly fields
/// </summary>
public class NullableDateOnlyJsonConverter : JsonConverter<DateOnly?>
{
    public override DateOnly? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dateString = reader.GetString();
        if (string.IsNullOrEmpty(dateString))
        {
            return null;
        }

        return DateTimeHelper.ParseDateOnly(dateString);
    }

    public override void Write(Utf8JsonWriter writer, DateOnly? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
        {
            writer.WriteStringValue(value.Value.ToString("yyyy-MM-dd"));
        }
        else
        {
            writer.WriteNullValue();
        }
    }
}