import { apiRequest } from "./apiClient";
import type { DisciplineCatalog } from "./adminSkillsApi";

function eventSkillsPath(eventId: number) {
  return `/api/events/${eventId}/skills`;
}

export function getEventSkills(eventId: number) {
  return apiRequest<DisciplineCatalog[]>(eventSkillsPath(eventId));
}

export function setEventSkills(eventId: number, skillIds: number[]) {
  return apiRequest<DisciplineCatalog[]>(eventSkillsPath(eventId), {
    method: "PUT",
    body: JSON.stringify({ skillIds }),
  });
}