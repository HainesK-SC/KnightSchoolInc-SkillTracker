import { apiRequest } from "./apiClient";

export type SkillStatus =
  | "InProgress"
  | "Completed"
  | "Locked";

export type ParticipantSkillProgress = {
  userId: string;
  skillId: number;
  status: SkillStatus;
  updatedAtUtc: string | null;
  updatedById: string | null;
};

type SetSkillStatusRequest = {
  status: SkillStatus;
};

export function setParticipantSkillStatus(
  userId: string,
  skillId: number,
  status: SkillStatus,
) {
  const request: SetSkillStatusRequest = {
    status,
  };

  return apiRequest<ParticipantSkillProgress>(
    `/api/users/${encodeURIComponent(userId)}/skills/${skillId}/progress`,
    {
      method: "PUT",
      body: JSON.stringify(request),
    },
  );
}