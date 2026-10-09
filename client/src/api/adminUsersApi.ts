import { apiRequest } from "./apiClient";

export type AdminUser = {
  id: string;
  firstName: string;
  lastName: string;
  email: string | null;
  salutation: string;
  modifier: string;
  displayName: string;
  avatarImagePath: string | null;
  status: string | number;
  roles: string[];
};

export type DisplayNameOption = {
  value: string;
  text: string;
};

export type DisplayNameOptions = {
  salutations: DisplayNameOption[];
  modifiers: DisplayNameOption[];
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
  salutation: string;
  modifier: string;
  email: string | null;
  roles: string[];
};

export function getAdminRoles(): Promise<string[]> {
  return apiRequest<string[]>("/api/admin/roles");
}

export function getDisplayNameOptions(): Promise<DisplayNameOptions> {
  return apiRequest<DisplayNameOptions>(
    "/api/admin/display-name-options",
  );
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

export function deleteAdminUser(userId: string): Promise<void> {
  return apiRequest<void>(`/api/admin/users/${userId}`, {
    method: "DELETE",
  });
}