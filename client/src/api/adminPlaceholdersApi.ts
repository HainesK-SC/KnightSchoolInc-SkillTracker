import { apiRequest } from "./apiClient";
import type { SkillStatus } from "./participantProgressApi";

export type MergeAccountSummaryDto = {
  userId: string;
  firstName: string;
  lastName: string;
  displayName: string;
  email: string | null;
  accountStatus: string;
};

export type MergeProgressConflictDto = {
  skillId: number;
  placeholderStatus: SkillStatus;
  targetStatus: SkillStatus;
  keptStatus: SkillStatus;
};

export type MergePreviewDto = {
  placeholder: MergeAccountSummaryDto;
  target: MergeAccountSummaryDto;
  progressToMove: number;
  progressConflicts: MergeProgressConflictDto[];
  enrollmentsToMove: number;
  duplicateEnrollmentsDropped: number;
  overlappingEventIds: number[];
  canMerge: boolean;
};

export function previewPlaceholderMerge(
  placeholderUserId: string,
  targetUserId: string,
) {
  const source = encodeURIComponent(placeholderUserId);
  const target = encodeURIComponent(targetUserId);

  return apiRequest<MergePreviewDto>(
    `/api/admin/placeholders/${source}/merge/${target}`,
  );
}

export function confirmPlaceholderMerge(
  placeholderUserId: string,
  targetUserId: string,
) {
  const source = encodeURIComponent(placeholderUserId);
  const target = encodeURIComponent(targetUserId);

  return apiRequest<void>(
    `/api/admin/placeholders/${source}/merge/${target}`,
    { method: "POST" },
  );
}