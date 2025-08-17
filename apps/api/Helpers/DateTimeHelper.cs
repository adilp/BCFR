using System;

namespace MemberOrgApi.Helpers;

public static class DateTimeHelper
{
    // Central Time Zone for all users
    private static readonly TimeZoneInfo CentralTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
    
    /// <summary>
    /// Convert a UTC DateTime to Central Time
    /// </summary>
    public static DateTime ConvertUtcToCentral(DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
        }
        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, CentralTimeZone);
    }
    
    /// <summary>
    /// Convert a Central Time DateTime to UTC
    /// </summary>
    public static DateTime ConvertCentralToUtc(DateTime centralDateTime)
    {
        if (centralDateTime.Kind == DateTimeKind.Utc)
        {
            return centralDateTime;
        }
        
        var unspecified = DateTime.SpecifyKind(centralDateTime, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, CentralTimeZone);
    }
    
    /// <summary>
    /// Get current time in Central Time Zone
    /// </summary>
    public static DateTime GetCentralTimeNow()
    {
        return ConvertUtcToCentral(DateTime.UtcNow);
    }
    
    /// <summary>
    /// Parse a date string as Central Time and convert to UTC for storage
    /// </summary>
    public static DateTime ParseAsClientalAndConvertToUtc(string dateString)
    {
        if (DateTime.TryParse(dateString, out var parsed))
        {
            // Assume the parsed date is in Central Time
            return ConvertCentralToUtc(parsed);
        }
        throw new ArgumentException($"Invalid date format: {dateString}");
    }
    
    /// <summary>
    /// Format a UTC DateTime for client display (converts to Central Time)
    /// </summary>
    public static string FormatForClient(DateTime utcDateTime)
    {
        var centralTime = ConvertUtcToCentral(utcDateTime);
        return centralTime.ToString("yyyy-MM-ddTHH:mm:ss");
    }
    
    /// <summary>
    /// Format a DateOnly for client display
    /// </summary>
    public static string FormatDateOnlyForClient(DateOnly date)
    {
        return date.ToString("yyyy-MM-dd");
    }
    
    /// <summary>
    /// Parse a date string to DateOnly
    /// </summary>
    public static DateOnly ParseDateOnly(string dateString)
    {
        if (DateOnly.TryParse(dateString, out var parsed))
        {
            return parsed;
        }
        throw new ArgumentException($"Invalid date format: {dateString}");
    }
    
    /// <summary>
    /// Combine date and time for event scheduling
    /// </summary>
    public static DateTime CombineDateAndTime(DateTime date, TimeSpan time)
    {
        var combined = date.Date + time;
        // Treat as Central Time and convert to UTC
        return ConvertCentralToUtc(combined);
    }
    
    /// <summary>
    /// Get start of day in Central Time, converted to UTC
    /// </summary>
    public static DateTime GetStartOfDayUtc(DateTime date)
    {
        var centralDate = ConvertUtcToCentral(date).Date;
        return ConvertCentralToUtc(centralDate);
    }
    
    /// <summary>
    /// Get end of day in Central Time, converted to UTC
    /// </summary>
    public static DateTime GetEndOfDayUtc(DateTime date)
    {
        var centralDate = ConvertUtcToCentral(date).Date.AddDays(1).AddTicks(-1);
        return ConvertCentralToUtc(centralDate);
    }
}