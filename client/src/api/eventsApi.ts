import { apiRequest } from "./apiClient";

export type EventRegistrationType = "PUBLIC" | "PRIVATE";

export type EventDto = {
  eventId: number;
  eventName: string;
  eventLocation: string;
  description: string | null;
  eventStart: string;
  eventEnd: string;
  registrationType: EventRegistrationType;
  isCancelled: boolean;
};

export type CreateEventRequest = {
  eventName: string;
  eventLocation: string;
  description?: string | null;
  eventStart: string;
  eventEnd: string;
  registrationType?: EventRegistrationType | null;
};

export type UpdateEventRequest = CreateEventRequest & {
  description: string;
};

export type EventSearchFilters = {
  search?: string;
  from?: string;
  to?: string;
  includeCancelled?: boolean;
};

const EVENTS_PATH = "/api/events";

export function searchEvents(filters: EventSearchFilters = {}) {
  const params = new URLSearchParams();
  const search = filters.search?.trim();

  if (search) params.set("search", search);
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);

  if (filters.includeCancelled !== undefined) {
    params.set("includeCancelled", String(filters.includeCancelled));
  }

  const query = params.toString();
  return apiRequest<EventDto[]>(
    query ? `${EVENTS_PATH}?${query}` : EVENTS_PATH,
  );
}

export function getEvent(eventId: number) {
  return apiRequest<EventDto>(`${EVENTS_PATH}/${eventId}`);
}

export function createEvent(request: CreateEventRequest) {
  return apiRequest<EventDto>(EVENTS_PATH, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function updateEvent(eventId: number, request: UpdateEventRequest) {
  return apiRequest<EventDto>(`${EVENTS_PATH}/${eventId}`, {
    method: "PUT",
    body: JSON.stringify(request),
  });
}

export function cancelEvent(eventId: number) {
  return apiRequest<EventDto>(`${EVENTS_PATH}/${eventId}/cancel`, {
    method: "PUT",
  });
}

export function deleteEvent(eventId: number) {
  return apiRequest<void>(`${EVENTS_PATH}/${eventId}`, {
    method: "DELETE",
  });
}