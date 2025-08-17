export interface User {
  id: string;
  username: string;
  email: string;
  firstName: string;
  lastName: string;
  role: string;
  dateOfBirth?: string;
}

export const APP_NAME = "Member Organization";

export const formatDate = (date: Date): string => {
  return date.toLocaleDateString();
};