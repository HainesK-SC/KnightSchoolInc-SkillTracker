import { apiRequest } from "./apiClient";
import type { SkillStatus } from "./participantProgressApi";

export type RosterEntryDto = {
  userId: string;
  firstName: string;
  lastName: string;
  displayName: string;
  email: string | null;
  accountStatus: string;
  enrolledAtUtc: string;
};

export type RegistrantLookupDto = {
  found: boolean;
  userId: string | null;
  firstName: string | null;
  lastName: string | null;
  displayName: string | null;
  accountStatus: string | null;
  alreadyEnrolled: boolean;
};

export type NewRegistrantRequest = {
  firstName: string;
  lastName: string;
  email: string | null;
};

function rosterPath(eventId: number) {
  return `/api/events/${eventId}/participants`;
}

export function getEventRoster(eventId: number, search = "") {
  const term = search.trim();
  const query = term ? `?${new URLSearchParams({ search: term })}` : "";

  return apiRequest<RosterEntryDto[]>(`${rosterPath(eventId)}${query}`);
}

export function lookupEventRegistrant(eventId: number, email: string) {
  const query = new URLSearchParams({ email: email.trim() });

  return apiRequest<RegistrantLookupDto>(
    `${rosterPath(eventId)}/lookup?${query}`,
  );
}

export async function enrollExistingRegistrant(
  eventId: number,
  userId: string,
) {
  await apiRequest<unknown>(
    `${rosterPath(eventId)}/${encodeURIComponent(userId)}`,
    { method: "POST" },
  );
}

export async function enrollNewRegistrant(
  eventId: number,
  request: NewRegistrantRequest,
) {
  await apiRequest<unknown>(`${rosterPath(eventId)}/new`, {
    method: "POST",
    body: JSON.stringify({
      ...request,
      email: request.email?.trim() || null,
    }),
  });
}

export type EventSkillStatusChange = {
  skillId: number;
  status: SkillStatus;
};

export type ParticipantEventSkillTree = {
  userId: string;
  disciplines: {
    disciplineId: number;
    name: string;
    skills: {
      skillId: number;
      name: string;
      description: string;
      benefits: string | null;
      status: SkillStatus;
      updatedAtUtc: string | null;
    }[];
  }[];
};

function participantSkillsPath(eventId: number, userId: string) {
  return `${rosterPath(eventId)}/${encodeURIComponent(userId)}/skills`;
}

export function getParticipantEventSkills(eventId: number, userId: string) {
  return apiRequest<ParticipantEventSkillTree>(
    participantSkillsPath(eventId, userId),
  );
}

export function setParticipantEventSkillStatuses(
  eventId: number,
  userId: string,
  changes: EventSkillStatusChange[],
) {
  return apiRequest<ParticipantEventSkillTree>(
    `${participantSkillsPath(eventId, userId)}/progress`,
    {
      method: "PUT",
      body: JSON.stringify({ changes }),
    },
  );
}