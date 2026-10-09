import { apiRequest } from "./apiClient";

export type CurrentUser = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  avatarImagePath: string | null;
  roles: string[];
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
};

export function loginUser(request: LoginRequest) {
  return apiRequest<CurrentUser>("/api/auth/login", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function registerUser(request: RegisterRequest) {
  return apiRequest<CurrentUser>("/api/auth/register", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function getCurrentUser() {
  return apiRequest<CurrentUser>("/api/auth/me");
}

export function logoutUser() {
  return apiRequest<void>("/api/auth/logout", {
    method: "POST",
  });
}