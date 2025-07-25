export interface User {
  id: string;
  email: string;
  name: string;
}

export const APP_NAME = "Member Organization";

export const formatDate = (date: Date): string => {
  return date.toLocaleDateString();
};