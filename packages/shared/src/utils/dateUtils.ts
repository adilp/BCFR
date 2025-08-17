/**
 * Date utilities for consistent date handling across the application
 * All dates are handled in Central Time Zone (America/Chicago)
 */

export const CENTRAL_TIMEZONE = 'America/Chicago';

/**
 * Format a date to ISO string for API communication
 * Ensures consistent format: YYYY-MM-DD
 */
export function formatDateForApi(date: Date | string): string {
  const d = typeof date === 'string' ? new Date(date) : date;
  const year = d.getFullYear();
  const month = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

/**
 * Format a datetime to ISO string for API communication
 * Returns full ISO string with timezone info
 */
export function formatDateTimeForApi(date: Date | string): string {
  const d = typeof date === 'string' ? new Date(date) : date;
  return d.toISOString();
}

/**
 * Parse date from API (handles both DateTime and DateOnly from C#)
 * Returns a Date object in local timezone
 */
export function parseDateFromApi(dateString: string | null | undefined): Date | null {
  if (!dateString) return null;
  
  // Handle DateOnly format (YYYY-MM-DD)
  if (dateString.match(/^\d{4}-\d{2}-\d{2}$/)) {
    // Parse as local date (not UTC) to avoid timezone shift
    const [year, month, day] = dateString.split('-').map(Number);
    return new Date(year, month - 1, day);
  }
  
  // Handle full DateTime format
  return new Date(dateString);
}

/**
 * Format date for display in UI
 * @param date - Date to format
 * @param options - Formatting options
 */
export function formatDateForDisplay(
  date: Date | string | null | undefined,
  options: {
    includeTime?: boolean;
    includeYear?: boolean;
    format?: 'short' | 'long' | 'full';
  } = {}
): string {
  if (!date) return '';
  
  const d = typeof date === 'string' ? parseDateFromApi(date) : date;
  if (!d) return '';
  
  const { includeTime = false, includeYear = true, format = 'long' } = options;
  
  if (format === 'short') {
    // MM/DD/YYYY format
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const day = String(d.getDate()).padStart(2, '0');
    const year = d.getFullYear();
    return includeYear ? `${month}/${day}/${year}` : `${month}/${day}`;
  }
  
  const dateOptions: Intl.DateTimeFormatOptions = {
    month: format === 'full' ? 'long' : 'short',
    day: 'numeric',
    ...(includeYear && { year: 'numeric' }),
    ...(includeTime && { 
      hour: 'numeric', 
      minute: '2-digit',
      hour12: true 
    })
  };
  
  return d.toLocaleDateString('en-US', dateOptions);
}

/**
 * Format relative time (e.g., "2 hours ago", "in 3 days")
 */
export function formatRelativeTime(date: Date | string): string {
  const d = typeof date === 'string' ? parseDateFromApi(date) : date;
  if (!d) return '';
  
  const now = new Date();
  const diffMs = now.getTime() - d.getTime();
  const diffSeconds = Math.floor(diffMs / 1000);
  const diffMinutes = Math.floor(diffSeconds / 60);
  const diffHours = Math.floor(diffMinutes / 60);
  const diffDays = Math.floor(diffHours / 24);
  
  if (Math.abs(diffSeconds) < 60) {
    return 'just now';
  } else if (Math.abs(diffMinutes) < 60) {
    const mins = Math.abs(diffMinutes);
    return diffMs > 0 
      ? `${mins} minute${mins !== 1 ? 's' : ''} ago`
      : `in ${mins} minute${mins !== 1 ? 's' : ''}`;
  } else if (Math.abs(diffHours) < 24) {
    const hours = Math.abs(diffHours);
    return diffMs > 0
      ? `${hours} hour${hours !== 1 ? 's' : ''} ago`
      : `in ${hours} hour${hours !== 1 ? 's' : ''}`;
  } else if (Math.abs(diffDays) < 30) {
    const days = Math.abs(diffDays);
    return diffMs > 0
      ? `${days} day${days !== 1 ? 's' : ''} ago`
      : `in ${days} day${days !== 1 ? 's' : ''}`;
  }
  
  return formatDateForDisplay(d, { format: 'short' });
}

/**
 * Convert time string (HH:mm) to TimeSpan format for C# API
 */
export function timeToTimeSpan(timeString: string): string {
  const [hours, minutes] = timeString.split(':');
  return `${hours.padStart(2, '0')}:${minutes.padStart(2, '0')}:00`;
}

/**
 * Convert TimeSpan from C# API to time string (HH:mm)
 */
export function timeSpanToTime(timeSpan: string): string {
  const parts = timeSpan.split(':');
  return `${parts[0]}:${parts[1]}`;
}

/**
 * Calculate age from date of birth
 */
export function calculateAge(dateOfBirth: Date | string | null | undefined): number | null {
  if (!dateOfBirth) return null;
  
  const dob = typeof dateOfBirth === 'string' ? parseDateFromApi(dateOfBirth) : dateOfBirth;
  if (!dob) return null;
  
  const today = new Date();
  let age = today.getFullYear() - dob.getFullYear();
  const monthDiff = today.getMonth() - dob.getMonth();
  
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < dob.getDate())) {
    age--;
  }
  
  return age;
}

/**
 * Check if a date is in the past
 */
export function isDateInPast(date: Date | string): boolean {
  const d = typeof date === 'string' ? parseDateFromApi(date) : date;
  if (!d) return false;
  
  return d < new Date();
}

/**
 * Check if a date is today
 */
export function isToday(date: Date | string): boolean {
  const d = typeof date === 'string' ? parseDateFromApi(date) : date;
  if (!d) return false;
  
  const today = new Date();
  return d.getDate() === today.getDate() &&
    d.getMonth() === today.getMonth() &&
    d.getFullYear() === today.getFullYear();
}

/**
 * Get the start of day for a date (00:00:00)
 */
export function startOfDay(date: Date | string): Date {
  const d = typeof date === 'string' ? parseDateFromApi(date) : date;
  if (!d) return new Date();
  
  const result = new Date(d);
  result.setHours(0, 0, 0, 0);
  return result;
}

/**
 * Get the end of day for a date (23:59:59)
 */
export function endOfDay(date: Date | string): Date {
  const d = typeof date === 'string' ? parseDateFromApi(date) : date;
  if (!d) return new Date();
  
  const result = new Date(d);
  result.setHours(23, 59, 59, 999);
  return result;
}

/**
 * Add days to a date
 */
export function addDays(date: Date | string, days: number): Date {
  const d = typeof date === 'string' ? parseDateFromApi(date) : date;
  if (!d) return new Date();
  
  const result = new Date(d);
  result.setDate(result.getDate() + days);
  return result;
}

/**
 * Format date for HTML input[type="date"]
 */
export function formatForDateInput(date: Date | string | null | undefined): string {
  if (!date) return '';
  
  const d = typeof date === 'string' ? parseDateFromApi(date) : date;
  if (!d) return '';
  
  return formatDateForApi(d);
}

/**
 * Parse date from HTML input[type="date"]
 */
export function parseFromDateInput(value: string): Date | null {
  if (!value) return null;
  return parseDateFromApi(value);
}

/**
 * Validate if a string is a valid date
 */
export function isValidDate(dateString: string): boolean {
  const date = parseDateFromApi(dateString);
  return date !== null && !isNaN(date.getTime());
}

/**
 * Get Central Time offset from UTC
 */
export function getCentralTimeOffset(): string {
  // Central Time is UTC-6 (CST) or UTC-5 (CDT)
  const now = new Date();
  const jan = new Date(now.getFullYear(), 0, 1);
  const jul = new Date(now.getFullYear(), 6, 1);
  const janOffset = jan.getTimezoneOffset();
  const julOffset = jul.getTimezoneOffset();
  const isDST = now.getTimezoneOffset() < Math.max(janOffset, julOffset);
  
  return isDST ? '-05:00' : '-06:00';
}

/**
 * Convert local time to Central Time
 */
export function toCentralTime(date: Date | string): Date {
  const d = typeof date === 'string' ? new Date(date) : date;
  
  // This is a simplified version - for production, consider using a library like date-fns-tz
  const centralOffset = getCentralTimeOffset();
  const offsetHours = parseInt(centralOffset.substring(1, 3));
  const offsetMinutes = parseInt(centralOffset.substring(4, 6));
  const offsetMs = (offsetHours * 60 + offsetMinutes) * 60 * 1000;
  
  return new Date(d.getTime() + (centralOffset.startsWith('-') ? -offsetMs : offsetMs));
}