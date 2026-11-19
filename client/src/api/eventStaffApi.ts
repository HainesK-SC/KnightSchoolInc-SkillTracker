import { apiRequest } from "./apiClient";

export type EventStaffDto = {
  id: number;
  eventId: number;
  userId: string;
  firstName: string;
  lastName: string;
};

function staffPath(eventId: number) {
  return `/api/events/${eventId}/staff`;
}

export function getEventStaff(eventId: number) {
  return apiRequest<EventStaffDto[]>(staffPath(eventId));
}

export async function assignEventStaff(eventId: number, userId: string) {
  await apiRequest<unknown>(
    `${staffPath(eventId)}/${encodeURIComponent(userId)}`,
    { method: "POST" },
  );
}

export async function unassignEventStaff(eventId: number, userId: string) {
  await apiRequest<unknown>(
    `${staffPath(eventId)}/${encodeURIComponent(userId)}`,
    { method: "DELETE" },
  );
}