import { apiRequest } from "./apiClient";

export type AdminUser = {
  id: string;
  firstName: string;
  lastName: string;
  email: string | null;
  displayName: string;
  avatarImagePath: string | null;
  status: string | number;
  salutation?: string | null;
  modifier?: string | null;
  roles: string[];
};

export type CreateAdminUserRequest = {
  firstName: string;
  lastName: string;
  email: string | null;
  roles: string[];
};

export type UpdateAdminUserRequest = {
  firstName: string;
  lastName: string;
  salutation: string | number;
  modifier: string | number;
  email: string | null;
  roles: string[];
};

export function getAdminRoles(): Promise<string[]> {
  return apiRequest<string[]>("/api/admin/roles");
}

export function getAdminUsers(): Promise<AdminUser[]> {
  return apiRequest<AdminUser[]>("/api/admin/users");
}

export function getAdminUser(userId: string): Promise<AdminUser> {
  return apiRequest<AdminUser>(`/api/admin/users/${userId}`);
}

export function createAdminUser(
  request: CreateAdminUserRequest,
): Promise<AdminUser> {
  return apiRequest<AdminUser>("/api/admin/users", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function updateAdminUser(
  userId: string,
  request: UpdateAdminUserRequest,
): Promise<AdminUser> {
  return apiRequest<AdminUser>(`/api/admin/users/${userId}`, {
    method: "PUT",
    body: JSON.stringify(request),
  });
}