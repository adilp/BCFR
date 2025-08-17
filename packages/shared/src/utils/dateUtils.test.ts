import {
  formatDateForApi,
  parseDateFromApi,
  formatDateForDisplay,
  calculateAge,
  isDateInPast,
  formatForDateInput,
  parseFromDateInput,
  isValidDate,
  addDays
} from './dateUtils';

describe('Date Utilities', () => {
  describe('formatDateForApi', () => {
    it('should format date to YYYY-MM-DD', () => {
      const date = new Date(2025, 0, 15); // Jan 15, 2025
      expect(formatDateForApi(date)).toBe('2025-01-15');
    });

    it('should handle string input', () => {
      expect(formatDateForApi('2025-01-15')).toBe('2025-01-15');
    });
  });

  describe('parseDateFromApi', () => {
    it('should parse DateOnly format (YYYY-MM-DD)', () => {
      const date = parseDateFromApi('2025-01-15');
      expect(date?.getFullYear()).toBe(2025);
      expect(date?.getMonth()).toBe(0); // January is 0
      expect(date?.getDate()).toBe(15);
    });

    it('should parse full DateTime format', () => {
      const date = parseDateFromApi('2025-01-15T14:30:00');
      expect(date?.getFullYear()).toBe(2025);
      expect(date?.getMonth()).toBe(0);
      expect(date?.getDate()).toBe(15);
    });

    it('should return null for invalid input', () => {
      expect(parseDateFromApi(null)).toBeNull();
      expect(parseDateFromApi(undefined)).toBeNull();
      expect(parseDateFromApi('')).toBeNull();
    });
  });

  describe('formatDateForDisplay', () => {
    it('should format date with default options', () => {
      const date = new Date(2025, 0, 15);
      const formatted = formatDateForDisplay(date);
      expect(formatted).toContain('Jan');
      expect(formatted).toContain('15');
      expect(formatted).toContain('2025');
    });

    it('should format date without year', () => {
      const date = new Date(2025, 0, 15);
      const formatted = formatDateForDisplay(date, { includeYear: false });
      expect(formatted).toContain('Jan');
      expect(formatted).toContain('15');
      expect(formatted).not.toContain('2025');
    });

    it('should format date in short format', () => {
      const date = new Date(2025, 0, 15);
      const formatted = formatDateForDisplay(date, { format: 'short' });
      expect(formatted).toBe('01/15/2025');
    });

    it('should handle null/undefined dates', () => {
      expect(formatDateForDisplay(null)).toBe('');
      expect(formatDateForDisplay(undefined)).toBe('');
    });
  });

  describe('calculateAge', () => {
    it('should calculate age correctly', () => {
      const today = new Date();
      const birthYear = today.getFullYear() - 25;
      const dob = new Date(birthYear, today.getMonth(), today.getDate());
      
      expect(calculateAge(dob)).toBe(25);
    });

    it('should handle upcoming birthday this year', () => {
      const today = new Date();
      const birthYear = today.getFullYear() - 25;
      const dob = new Date(birthYear, today.getMonth() + 1, today.getDate());
      
      expect(calculateAge(dob)).toBe(24);
    });

    it('should return null for invalid input', () => {
      expect(calculateAge(null)).toBeNull();
      expect(calculateAge(undefined)).toBeNull();
    });
  });

  describe('isDateInPast', () => {
    it('should return true for past dates', () => {
      const yesterday = new Date();
      yesterday.setDate(yesterday.getDate() - 1);
      
      expect(isDateInPast(yesterday)).toBe(true);
    });

    it('should return false for future dates', () => {
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      
      expect(isDateInPast(tomorrow)).toBe(false);
    });
  });

  describe('addDays', () => {
    it('should add days correctly', () => {
      const date = new Date(2025, 0, 15);
      const result = addDays(date, 7);
      
      expect(result.getDate()).toBe(22);
      expect(result.getMonth()).toBe(0);
    });

    it('should handle month overflow', () => {
      const date = new Date(2025, 0, 30);
      const result = addDays(date, 5);
      
      expect(result.getDate()).toBe(4);
      expect(result.getMonth()).toBe(1); // February
    });

    it('should handle negative days', () => {
      const date = new Date(2025, 0, 15);
      const result = addDays(date, -5);
      
      expect(result.getDate()).toBe(10);
      expect(result.getMonth()).toBe(0);
    });
  });

  describe('formatForDateInput', () => {
    it('should format date for HTML input', () => {
      const date = new Date(2025, 0, 15);
      expect(formatForDateInput(date)).toBe('2025-01-15');
    });

    it('should handle null/undefined', () => {
      expect(formatForDateInput(null)).toBe('');
      expect(formatForDateInput(undefined)).toBe('');
    });
  });

  describe('parseFromDateInput', () => {
    it('should parse HTML input value', () => {
      const date = parseFromDateInput('2025-01-15');
      expect(date?.getFullYear()).toBe(2025);
      expect(date?.getMonth()).toBe(0);
      expect(date?.getDate()).toBe(15);
    });

    it('should return null for empty input', () => {
      expect(parseFromDateInput('')).toBeNull();
    });
  });

  describe('isValidDate', () => {
    it('should validate correct date strings', () => {
      expect(isValidDate('2025-01-15')).toBe(true);
      expect(isValidDate('2025-01-15T14:30:00')).toBe(true);
    });

    it('should invalidate incorrect date strings', () => {
      expect(isValidDate('invalid')).toBe(false);
      expect(isValidDate('2025-13-45')).toBe(false);
      expect(isValidDate('')).toBe(false);
    });
  });
});