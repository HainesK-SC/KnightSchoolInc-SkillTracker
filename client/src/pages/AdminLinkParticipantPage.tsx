import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router";

import {
  confirmPlaceholderMerge,
  previewPlaceholderMerge,
  type MergeAccountSummaryDto,
  type MergePreviewDto,
} from "../api/adminPlaceholdersApi";
import { getAdminUsers, type AdminUser } from "../api/adminUsersApi";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";

function accountLabel(user: AdminUser) {
  const name = user.displayName || `${user.firstName} ${user.lastName}`;
  return `${name} — ${user.email || "No email"} — ${user.status} — ID: ${user.id}`;
}

function previewAccountLabel(account: MergeAccountSummaryDto) {
  const name = account.displayName || `${account.firstName} ${account.lastName}`;
  return `${name} — ${account.email || "No email"} — ${account.accountStatus} — ID: ${account.userId}`;
}

export default function AdminLinkParticipantPage() {
  const [users, setUsers] = useState<AdminUser[] | null>(null);
  const [loadError, setLoadError] = useState("");
  const [sourceId, setSourceId] = useState("");
  const [targetId, setTargetId] = useState("");
  const [preview, setPreview] = useState<MergePreviewDto | null>(null);
  const [previewError, setPreviewError] = useState("");
  const [mergeStatus, setMergeStatus] = useState("");
  const [isPreviewing, setIsPreviewing] = useState(false);
  const [isMerging, setIsMerging] = useState(false);

  const isBusy = isPreviewing || isMerging;
  const hasValidSelection = Boolean(sourceId && targetId && sourceId !== targetId);

  useEffect(() => {
    let ignore = false;

    async function loadUsers() {
      try {
        const result = await getAdminUsers();
        if (!ignore) setUsers(result);
      } catch (error) {
        if (!ignore) {
          setLoadError(
            error instanceof Error ? error.message : "Could not load accounts.",
          );
        }
      }
    }

    void loadUsers();
    return () => {
      ignore = true;
    };
  }, []);

  function clearPreview() {
    setPreview(null);
    setPreviewError("");
    setMergeStatus("");
  }

  async function handlePreview(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isBusy || !hasValidSelection) return;

    clearPreview();
    setIsPreviewing(true);

    try {
      setPreview(await previewPlaceholderMerge(sourceId, targetId));
    } catch (error) {
      setPreviewError(
        error instanceof Error ? error.message : "Could not preview the merge.",
      );
    } finally {
      setIsPreviewing(false);
    }
  }

  async function handleConfirmMerge() {
    if (
      isBusy ||
      !hasValidSelection ||
      !preview?.canMerge ||
      preview.overlappingEventIds.length > 0
    ) {
      return;
    }

    const { placeholder, target } = preview;

    if (placeholder.userId !== sourceId || target.userId !== targetId) {
      return;
    }

    const confirmed = window.confirm(
      `Source placeholder:\n${previewAccountLabel(placeholder)}\n\n` +
        `Destination full account:\n${previewAccountLabel(target)}\n\n` +
        "Only continue if you have verified that both accounts belong to the same person. " +
        "The source placeholder will be permanently deleted. This cannot be undone.",
    );

    if (!confirmed) return;

    setIsMerging(true);
    setPreviewError("");
    setMergeStatus("");

    try {
      await confirmPlaceholderMerge(placeholder.userId, target.userId);

      setUsers((current) =>
        current?.filter((user) => user.id !== placeholder.userId) ?? null,
      );
      setSourceId("");
      setTargetId("");
      setPreview(null);

      const targetName =
        target.displayName || `${target.firstName} ${target.lastName}`;
      setMergeStatus(`Merge complete. ${targetName} is the remaining account.`);
    } catch (error) {
      setPreview(null);
      const message =
        error instanceof Error ? error.message : "Could not confirm the merge result.";

      setPreviewError(
        `${message} Verify both accounts before running a new preview and trying again.`,
      );
    } finally {
      setIsMerging(false);
    }
  }

  return (
    <main className="admin-main">
      <Link className="admin-back-link" to="/admin">
        ← Back to dashboard
      </Link>

      <p className="admin-eyebrow">Participant management</p>
      <h1 className="admin-title">Link Participant Profile</h1>
      <p className="admin-description">
        Select the placeholder first and the full account second. Only link
        accounts that belong to the same person.
      </p>

      <section className="admin-panel" aria-label="Merge preview">
        {loadError ? (
          <p className="admin-form-error" role="alert">{loadError}</p>
        ) : users === null ? (
          <p role="status">Loading accounts…</p>
        ) : users.length < 2 ? (
          <p>At least two accounts are needed to preview a merge.</p>
        ) : (
          <form className="admin-form" onSubmit={handlePreview}>
            <div className="admin-form-group">
              <label htmlFor="merge-source">Source placeholder</label>
              <select
                id="merge-source"
                value={sourceId}
                disabled={isBusy}
                required
                onChange={(event) => {
                  setSourceId(event.target.value);
                  clearPreview();
                }}
              >
                <option value="">Select the placeholder account</option>
                {users.map((user) => (
                  <option
                    key={user.id}
                    value={user.id}
                    disabled={user.id === targetId}
                  >
                    {accountLabel(user)}
                  </option>
                ))}
              </select>
            </div>

            <div className="admin-form-group">
              <label htmlFor="merge-target">Destination full account</label>
              <select
                id="merge-target"
                value={targetId}
                disabled={isBusy}
                required
                onChange={(event) => {
                  setTargetId(event.target.value);
                  clearPreview();
                }}
              >
                <option value="">Select the full account</option>
                {users.map((user) => (
                  <option
                    key={user.id}
                    value={user.id}
                    disabled={user.id === sourceId}
                  >
                    {accountLabel(user)}
                  </option>
                ))}
              </select>
            </div>

            <button
              className="site-button site-button-primary"
              type="submit"
              disabled={isBusy || !hasValidSelection}
            >
              {isPreviewing ? "Loading preview…" : "Preview merge"}
            </button>
          </form>
        )}

        {previewError && (
          <p className="admin-form-error" role="alert">{previewError}</p>
        )}

        {mergeStatus && (
          <p className="admin-form-status" role="status">{mergeStatus}</p>
        )}

        {preview && (
          <div>
            <h2>Proposed changes</h2>
            <p role="status">
              {isMerging
                ? "Merge in progress…"
                : preview.canMerge && preview.overlappingEventIds.length === 0
                  ? "This pair is eligible to merge. No changes have been made."
                  : "This merge cannot proceed. No changes have been made."}
            </p>

            <dl>
              <dt>From placeholder</dt>
              <dd>{previewAccountLabel(preview.placeholder)}</dd>

              <dt>To full account</dt>
              <dd>{previewAccountLabel(preview.target)}</dd>

              <dt>Skill progress records to move</dt>
              <dd>{preview.progressToMove}</dd>

              <dt>Enrollments to move</dt>
              <dd>{preview.enrollmentsToMove}</dd>

              <dt>Duplicate enrollments to drop</dt>
              <dd>{preview.duplicateEnrollmentsDropped}</dd>

              <dt>Overlapping event IDs</dt>
              <dd>{preview.overlappingEventIds.join(", ") || "None"}</dd>
            </dl>

            <h3>Skill progress conflicts</h3>
            {preview.progressConflicts.length === 0 ? (
              <p>No skill progress conflicts.</p>
            ) : (
              <ul>
                {preview.progressConflicts.map((conflict) => (
                  <li key={conflict.skillId}>
                    Skill #{conflict.skillId}: placeholder {conflict.placeholderStatus},
                    {" destination "}
                    {conflict.targetStatus}. Keep:{" "}
                    <strong>{conflict.keptStatus}</strong>.
                  </li>
                ))}
              </ul>
            )}

            {preview.canMerge && preview.overlappingEventIds.length === 0 && (
              <>
                <p>
                  Merging permanently deletes the source placeholder. Verify that
                  both accounts belong to the same person before continuing.
                </p>
                <div className="admin-form-actions">
                  <button
                    className="site-button site-button-danger"
                    type="button"
                    disabled={isBusy}
                    onClick={() => void handleConfirmMerge()}
                  >
                    {isMerging ? "Merging…" : "Confirm merge"}
                  </button>
                </div>
              </>
            )}
          </div>
        )}
      </section>
    </main>
  );
}