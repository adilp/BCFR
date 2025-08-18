/**
 * Format currency values
 */
export const formatCurrency = (
  amount: number,
  currency: string = 'USD',
  locale: string = 'en-US'
): string => {
  return amount.toLocaleString(locale, {
    style: 'currency',
    currency: currency,
    minimumFractionDigits: 2,
    maximumFractionDigits: 2
  });
};

/**
 * Format currency without decimal places
 */
export const formatCurrencyCompact = (
  amount: number,
  currency: string = 'USD',
  locale: string = 'en-US'
): string => {
  return amount.toLocaleString(locale, {
    style: 'currency',
    currency: currency,
    minimumFractionDigits: 0,
    maximumFractionDigits: 0
  });
};

/**
 * Format percentage
 */
export const formatPercentage = (
  value: number,
  decimals: number = 2,
  locale: string = 'en-US'
): string => {
  return value.toLocaleString(locale, {
    style: 'percent',
    minimumFractionDigits: decimals,
    maximumFractionDigits: decimals
  });
};

/**
 * Format phone number (US format)
 */
export const formatPhone = (phone: string): string => {
  // Remove all non-numeric characters
  const cleaned = phone.replace(/\D/g, '');
  
  // Check if the number is valid
  if (cleaned.length < 10) return phone;
  
  // Remove country code if present
  const number = cleaned.length === 11 && cleaned[0] === '1' 
    ? cleaned.substring(1) 
    : cleaned;
  
  // Format as (XXX) XXX-XXXX
  if (number.length === 10) {
    return `(${number.slice(0, 3)}) ${number.slice(3, 6)}-${number.slice(6)}`;
  }
  
  return phone;
};

/**
 * Format ZIP code
 */
export const formatZipCode = (zipCode: string): string => {
  const cleaned = zipCode.replace(/\D/g, '');
  
  if (cleaned.length === 5) {
    return cleaned;
  }
  
  if (cleaned.length === 9) {
    return `${cleaned.slice(0, 5)}-${cleaned.slice(5)}`;
  }
  
  return zipCode;
};

/**
 * Format file size
 */
export const formatFileSize = (bytes: number): string => {
  if (bytes === 0) return '0 Bytes';
  
  const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(1024));
  
  return `${(bytes / Math.pow(1024, i)).toFixed(2)} ${sizes[i]}`;
};

/**
 * Format number with commas
 */
export const formatNumber = (
  num: number,
  locale: string = 'en-US'
): string => {
  return num.toLocaleString(locale);
};

/**
 * Format number with compact notation (1K, 1M, etc.)
 */
export const formatNumberCompact = (
  num: number,
  decimals: number = 1
): string => {
  if (num < 1000) return num.toString();
  
  const units = ['K', 'M', 'B', 'T'];
  const unitIndex = Math.floor(Math.log10(num) / 3) - 1;
  const value = num / Math.pow(1000, unitIndex + 1);
  
  return `${value.toFixed(decimals)}${units[unitIndex]}`;
};

/**
 * Truncate text with ellipsis
 */
export const truncateText = (
  text: string,
  maxLength: number,
  ellipsis: string = '...'
): string => {
  if (text.length <= maxLength) return text;
  return text.slice(0, maxLength - ellipsis.length) + ellipsis;
};

/**
 * Format name (capitalize first letter of each word)
 */
export const formatName = (name: string): string => {
  return name
    .toLowerCase()
    .split(' ')
    .map(word => word.charAt(0).toUpperCase() + word.slice(1))
    .join(' ');
};

/**
 * Format username (lowercase, remove spaces)
 */
export const formatUsername = (username: string): string => {
  return username
    .toLowerCase()
    .replace(/\s+/g, '_')
    .replace(/[^a-z0-9_-]/g, '');
};

/**
 * Format credit card number (mask all but last 4 digits)
 */
export const formatCardNumber = (cardNumber: string): string => {
  const cleaned = cardNumber.replace(/\D/g, '');
  
  if (cleaned.length < 4) return cardNumber;
  
  const last4 = cleaned.slice(-4);
  const masked = '*'.repeat(cleaned.length - 4);
  
  // Format as **** **** **** 1234
  const formatted = (masked + last4).match(/.{1,4}/g)?.join(' ') || cardNumber;
  
  return formatted;
};

/**
 * Format duration in milliseconds to human-readable string
 */
export const formatDuration = (milliseconds: number): string => {
  const seconds = Math.floor(milliseconds / 1000);
  const minutes = Math.floor(seconds / 60);
  const hours = Math.floor(minutes / 60);
  const days = Math.floor(hours / 24);
  
  if (days > 0) {
    return `${days} day${days !== 1 ? 's' : ''}`;
  }
  
  if (hours > 0) {
    return `${hours} hour${hours !== 1 ? 's' : ''}`;
  }
  
  if (minutes > 0) {
    return `${minutes} minute${minutes !== 1 ? 's' : ''}`;
  }
  
  return `${seconds} second${seconds !== 1 ? 's' : ''}`;
};

/**
 * Format address into a single line
 */
export const formatAddress = (
  address?: string,
  city?: string,
  state?: string,
  zipCode?: string,
  country?: string
): string => {
  const parts = [address, city, state, zipCode, country].filter(Boolean);
  return parts.join(', ');
};

/**
 * Format full name from first and last name
 */
export const formatFullName = (
  firstName?: string,
  lastName?: string,
  middleName?: string
): string => {
  const parts = [firstName, middleName, lastName].filter(Boolean);
  return parts.join(' ');
};

/**
 * Format initials from name
 */
export const formatInitials = (
  firstName?: string,
  lastName?: string
): string => {
  const first = firstName?.charAt(0).toUpperCase() || '';
  const last = lastName?.charAt(0).toUpperCase() || '';
  return first + last;
};