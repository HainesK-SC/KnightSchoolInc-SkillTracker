import { useEffect, useState, type FormEvent } from "react";

import {
  getParticipantEventSkills,
  setParticipantEventSkillStatuses,
  type EventSkillStatusChange,
  type ParticipantEventSkillTree,
} from "../api/eventParticipantsApi";
import type { SkillStatus } from "../api/participantProgressApi";

const statusLabels: Record<SkillStatus, string> = {
  Locked: "Locked",
  InProgress: "In Progress",
  Completed: "Completed",
};

const statuses: SkillStatus[] = ["Locked", "InProgress", "Completed"];

type Props = {
  eventId: number;
  userId: string;
  participantName: string;
  onClose: () => void;
};

export default function ParticipantEventSkillsEditor({
  eventId,
  userId,
  participantName,
  onClose,
}: Props) {
  const [tree, setTree] = useState<ParticipantEventSkillTree | null>(null);
  const [draft, setDraft] = useState<Partial<Record<number, SkillStatus>>>({});
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");
  const [status, setStatus] = useState("");

  useEffect(() => {
    let ignore = false;

    getParticipantEventSkills(eventId, userId)
      .then((result) => {
        if (!ignore) setTree(result);
      })
      .catch((err: unknown) => {
        if (!ignore) {
          setError(err instanceof Error ? err.message : "Could not load event skills.");
        }
      });

    return () => {
      ignore = true;
    };
  }, [eventId, userId]);

  const groups = tree?.disciplines ?? [];
  const skills = groups.flatMap((group) => group.skills);
  const changes: EventSkillStatusChange[] = skills.flatMap((skill) => {
    const nextStatus = draft[skill.skillId];
    return nextStatus && nextStatus !== skill.status
      ? [{ skillId: skill.skillId, status: nextStatus }]
      : [];
  });

  async function handleSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSaving || changes.length === 0) return;

    if (!window.confirm(`Save skill progress for ${participantName}?`)) return;

    setIsSaving(true);
    setError("");
    setStatus("");

    try {
      const updated = await setParticipantEventSkillStatuses(
        eventId,
        userId,
        changes,
      );

      setTree(updated);
      setDraft({});
      setStatus("Skill progress saved.");
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not save skill progress.");
    } finally {
      setIsSaving(false);
    }
  }

  return (
    <section aria-labelledby="participant-event-skills-heading">
      <h3 id="participant-event-skills-heading">
        Manage skills: {participantName}
      </h3>

      {error && <p className="admin-form-error" role="alert">{error}</p>}
      {status && <p className="admin-form-status" role="status">{status}</p>}
      {!tree && !error && <p role="status">Loading event skills…</p>}
      {tree && skills.length === 0 && <p>No skills are assigned to this event.</p>}

      {skills.length > 0 && (
        <form className="admin-form" onSubmit={handleSave}>
          {groups.map((group) => (
            <fieldset key={group.disciplineId} disabled={isSaving}>
              <legend>{group.name}</legend>

              {group.skills.map((skill) => (
                <div className="admin-form-group" key={skill.skillId}>
                  <label htmlFor={`participant-skill-${skill.skillId}`}>
                    {skill.name}
                  </label>

                  <select
                    id={`participant-skill-${skill.skillId}`}
                    value={draft[skill.skillId] ?? skill.status}
                    onChange={(event) => {
                      const nextStatus = event.target.value as SkillStatus;
                      setDraft((current) => ({
                        ...current,
                        [skill.skillId]: nextStatus,
                      }));
                      setStatus("");
                    }}
                  >
                    {statuses.map((option) => (
                      <option key={option} value={option}>
                        {statusLabels[option]}
                      </option>
                    ))}
                  </select>
                </div>
              ))}
            </fieldset>
          ))}

          <p>{changes.length} unsaved {changes.length === 1 ? "change" : "changes"}</p>

          <button
            className="site-button site-button-primary"
            type="submit"
            disabled={isSaving || changes.length === 0}
          >
            {isSaving ? "Saving…" : "Save progress"}
          </button>
        </form>
      )}

      <button
        className="site-button site-button-secondary"
        type="button"
        disabled={isSaving}
        onClick={() => {
          if (changes.length > 0 && !window.confirm("Discard unsaved changes?")) {
            return;
          }
          onClose();
        }}
      >
        Close skill editor
      </button>
    </section>
  );
}