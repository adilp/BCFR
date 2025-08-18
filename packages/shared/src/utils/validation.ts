/**
 * Email validation
 */
export const validateEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return emailRegex.test(email);
};

/**
 * Phone number validation (US format)
 */
export const validatePhone = (phone: string): boolean => {
  const phoneRegex = /^(\+1|1)?[-.\s]?\(?[0-9]{3}\)?[-.\s]?[0-9]{3}[-.\s]?[0-9]{4}$/;
  return phoneRegex.test(phone.replace(/\s/g, ''));
};

/**
 * ZIP code validation (US format)
 */
export const validateZipCode = (zipCode: string): boolean => {
  const zipRegex = /^\d{5}(-\d{4})?$/;
  return zipRegex.test(zipCode);
};

/**
 * Password strength validation
 */
export const validatePassword = (password: string): {
  isValid: boolean;
  errors: string[];
} => {
  const errors: string[] = [];
  
  if (password.length < 8) {
    errors.push('Password must be at least 8 characters long');
  }
  
  if (!/[A-Z]/.test(password)) {
    errors.push('Password must contain at least one uppercase letter');
  }
  
  if (!/[a-z]/.test(password)) {
    errors.push('Password must contain at least one lowercase letter');
  }
  
  if (!/[0-9]/.test(password)) {
    errors.push('Password must contain at least one number');
  }
  
  if (!/[!@#$%^&*(),.?":{}|<>]/.test(password)) {
    errors.push('Password must contain at least one special character');
  }
  
  return {
    isValid: errors.length === 0,
    errors
  };
};

/**
 * Username validation
 */
export const validateUsername = (username: string): boolean => {
  // Username must be 3-20 characters, alphanumeric with underscores and hyphens
  const usernameRegex = /^[a-zA-Z0-9_-]{3,20}$/;
  return usernameRegex.test(username);
};

/**
 * Check if a string is empty or only whitespace
 */
export const isRequired = (value: string | null | undefined): boolean => {
  return value !== null && value !== undefined && value.trim().length > 0;
};

/**
 * Validate minimum length
 */
export const minLength = (value: string, min: number): boolean => {
  return value.length >= min;
};

/**
 * Validate maximum length
 */
export const maxLength = (value: string, max: number): boolean => {
  return value.length <= max;
};

/**
 * Validate age from date of birth
 */
export const validateAge = (dateOfBirth: string, minAge: number = 18): boolean => {
  const today = new Date();
  const birthDate = new Date(dateOfBirth);
  const age = today.getFullYear() - birthDate.getFullYear();
  const monthDiff = today.getMonth() - birthDate.getMonth();
  
  if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDate.getDate())) {
    return age - 1 >= minAge;
  }
  
  return age >= minAge;
};

/**
 * Validate URL
 */
export const validateUrl = (url: string): boolean => {
  try {
    new URL(url);
    return true;
  } catch {
    return false;
  }
};

/**
 * Form field validator builder
 */
export interface ValidationRule {
  validate: (value: any) => boolean;
  message: string;
}

export const createValidator = (rules: ValidationRule[]) => {
  return (value: any): { isValid: boolean; errors: string[] } => {
    const errors: string[] = [];
    
    for (const rule of rules) {
      if (!rule.validate(value)) {
        errors.push(rule.message);
      }
    }
    
    return {
      isValid: errors.length === 0,
      errors
    };
  };
};

/**
 * Common validation rules
 */
export const validationRules = {
  required: (message = 'This field is required'): ValidationRule => ({
    validate: isRequired,
    message
  }),
  
  email: (message = 'Please enter a valid email address'): ValidationRule => ({
    validate: validateEmail,
    message
  }),
  
  phone: (message = 'Please enter a valid phone number'): ValidationRule => ({
    validate: validatePhone,
    message
  }),
  
  zipCode: (message = 'Please enter a valid ZIP code'): ValidationRule => ({
    validate: validateZipCode,
    message
  }),
  
  minLength: (min: number, message?: string): ValidationRule => ({
    validate: (value: string) => minLength(value, min),
    message: message || `Must be at least ${min} characters long`
  }),
  
  maxLength: (max: number, message?: string): ValidationRule => ({
    validate: (value: string) => maxLength(value, max),
    message: message || `Must be no more than ${max} characters long`
  }),
  
  username: (message = 'Username must be 3-20 characters and contain only letters, numbers, underscores, and hyphens'): ValidationRule => ({
    validate: validateUsername,
    message
  }),
  
  age: (minAge: number = 18, message?: string): ValidationRule => ({
    validate: (value: string) => validateAge(value, minAge),
    message: message || `Must be at least ${minAge} years old`
  }),
  
  url: (message = 'Please enter a valid URL'): ValidationRule => ({
    validate: validateUrl,
    message
  })
};