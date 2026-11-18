import { apiRequest } from "./apiClient";

export type SkillStatus =
  | "Locked"
  | "InProgress"
  | "Completed";

export type ParticipantSkillProgress = {
  userId: string;
  skillId: number;
  status: SkillStatus;
  updatedAtUtc: string | null;
  updatedById: string | null;
};

export type SkillTreeSkill = {
  skillId: number;
  name: string;
  description: string;
  benefits: string | null;
  status: SkillStatus;
  updatedAtUtc: string | null;
};

export type SkillTreeDiscipline = {
  disciplineId: number;
  name: string;
  skills: SkillTreeSkill[];
};

export type SkillTree = {
  userId: string;
  disciplines: SkillTreeDiscipline[];
};

type SetSkillStatusRequest = {
  status: SkillStatus;
};

type ResultEnvelope<T> = {
  succeeded: boolean;
  data: T | null;
  error: string | null;
  errorType: string | number;
};

function normalizeSkillTree(tree: SkillTree): SkillTree {
  return {
    ...tree,
    disciplines: (tree.disciplines ?? []).map((discipline) => ({
      ...discipline,
      skills: discipline.skills ?? [],
    })),
  };
}

export async function getMySkillTree(): Promise<SkillTree> {
  const tree = await apiRequest<SkillTree>(
    "/api/skills/tree",
    {
      cache: "no-store",
    },
  );

  return normalizeSkillTree(tree);
}

export async function getParticipantSkillTree(
  userId: string,
): Promise<SkillTree> {
  const tree = await apiRequest<SkillTree>(
    `/api/users/${encodeURIComponent(userId)}/skill-tree`,
    {
      cache: "no-store",
    },
  );

  return normalizeSkillTree(tree);
}

export async function setParticipantSkillStatus(
  userId: string,
  skillId: number,
  status: SkillStatus,
): Promise<ParticipantSkillProgress> {
  const request: SetSkillStatusRequest = {
    status,
  };

  const response = await apiRequest<
    ParticipantSkillProgress |
      ResultEnvelope<ParticipantSkillProgress>
  >(
    `/api/users/${encodeURIComponent(userId)}/skills/${skillId}/progress`,
    {
      method: "PUT",
      body: JSON.stringify(request),
    },
  );

  if ("data" in response) {
    if (!response.data) {
      throw new Error(
        response.error ||
          "The server did not return the updated progress.",
      );
    }

    return response.data;
  }

  return response;
}