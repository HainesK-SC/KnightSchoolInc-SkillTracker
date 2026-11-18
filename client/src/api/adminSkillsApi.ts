import { apiRequest } from "./apiClient";

export type Skill = {
  skillId: number;
  name: string;
  description: string;
  benefits: string | null;
  isArchived: boolean;
  disciplineId: number;
};

export type SkillRequest = {
  name: string;
  description: string;
  benefits: string | null;
  disciplineId: number;
};

export type Discipline = {
  disciplineId: number;
  name: string;
};

export type DisciplineCatalog = Discipline & {
  skills: Skill[];
};

export type DisciplineRequest = {
  name: string;
};

export function getAdminDisciplines() {
  return apiRequest<Discipline[]>("/api/admin/disciplines");
}

export function getAdminDiscipline(disciplineId: number) {
  return apiRequest<DisciplineCatalog>(
    `/api/admin/disciplines/${disciplineId}`,
  );
}

export function createAdminDiscipline(
  request: DisciplineRequest,
) {
  return apiRequest<Discipline>("/api/admin/disciplines", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function updateAdminDiscipline(
  disciplineId: number,
  request: DisciplineRequest,
) {
  return apiRequest<Discipline>(
    `/api/admin/disciplines/${disciplineId}`,
    {
      method: "PUT",
      body: JSON.stringify(request),
    },
  );
}

export function deleteAdminDiscipline(
  disciplineId: number,
) {
  return apiRequest<void>(
    `/api/admin/disciplines/${disciplineId}`,
    {
      method: "DELETE",
    },
  );
}

export function getAdminSkillCatalog() {
  return apiRequest<DisciplineCatalog[]>("/api/admin/skills");
}

export function getAdminSkill(skillId: number) {
  return apiRequest<Skill>(`/api/admin/skills/${skillId}`);
}

export function createAdminSkill(request: SkillRequest) {
  return apiRequest<Skill>("/api/admin/skills", {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export function updateAdminSkill(
  skillId: number,
  request: SkillRequest,
) {
  return apiRequest<Skill>(`/api/admin/skills/${skillId}`, {
    method: "PUT",
    body: JSON.stringify(request),
  });
}

export function archiveAdminSkill(skillId: number) {
  return apiRequest<Skill>(
    `/api/admin/skills/${skillId}/archive`,
    {
      method: "POST",
    },
  );
}

export function unarchiveAdminSkill(skillId: number) {
  return apiRequest<Skill>(
    `/api/admin/skills/${skillId}/unarchive`,
    {
      method: "POST",
    },
  );
}

export function deleteAdminSkill(skillId: number) {
  return apiRequest<void>(`/api/admin/skills/${skillId}`, {
    method: "DELETE",
  });
}