import { useEffect, useState, type FormEvent } from "react";

import {
  getAdminSkillCatalog,
  type DisciplineCatalog,
} from "../api/adminSkillsApi";
import { getEventSkills, setEventSkills } from "../api/eventSkillsApi";

function getSkillIds(disciplines: DisciplineCatalog[]) {
  return disciplines.flatMap((discipline) =>
    discipline.skills.map((skill) => skill.skillId),
  );
}

export default function EventSkillsSection({
  eventId,
}: {
  eventId: number;
}) {
  const [catalog, setCatalog] = useState<DisciplineCatalog[] | null>(null);
  const [savedIds, setSavedIds] = useState<number[]>([]);
  const [selectedIds, setSelectedIds] = useState<number[]>([]);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");
  const [status, setStatus] = useState("");

  useEffect(() => {
    let ignore = false;

    async function loadSkills() {
      try {
        const [allSkills, assignedSkills] = await Promise.all([
          getAdminSkillCatalog(),
          getEventSkills(eventId),
        ]);

        if (ignore) return;

        const ids = getSkillIds(assignedSkills);
        setCatalog(allSkills);
        setSavedIds(ids);
        setSelectedIds(ids);
      } catch (err) {
        if (!ignore) {
          setError(
            err instanceof Error ? err.message : "Could not load event skills.",
          );
        }
      }
    }

    void loadSkills();

    return () => {
      ignore = true;
    };
  }, [eventId]);

  const hasChanges =
    selectedIds.length !== savedIds.length ||
    selectedIds.some((id) => !savedIds.includes(id));

  function toggleSkill(skillId: number) {
    setStatus("");
    setSelectedIds((current) =>
      current.includes(skillId)
        ? current.filter((id) => id !== skillId)
        : [...current, skillId],
    );
  }

  async function handleSave(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!hasChanges || isSaving) return;

    setIsSaving(true);
    setError("");
    setStatus("");

    try {
      const result = await setEventSkills(eventId, selectedIds);
      const ids = getSkillIds(result);

      setSavedIds(ids);
      setSelectedIds(ids);
      setStatus("Event skills saved.");
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Could not save event skills.",
      );
    } finally {
      setIsSaving(false);
    }
  }

  // Show archived skills only if they are already assigned, so the admin
  // can see and remove them without being able to add archived skills.
  const groups =
    catalog
      ?.map((discipline) => ({
        ...discipline,
        skills: discipline.skills.filter(
          (skill) => !skill.isArchived || savedIds.includes(skill.skillId),
        ),
      }))
      .filter((discipline) => discipline.skills.length > 0) ?? [];

  return (
    <section className="admin-panel" aria-labelledby="event-skills-heading">
      <h2 id="event-skills-heading">Event Skills</h2>

      {error && <p className="admin-form-error" role="alert">{error}</p>}
      {status && <p className="admin-form-status" role="status">{status}</p>}
      {catalog === null && !error && <p role="status">Loading event skills…</p>}

      {catalog !== null && (
        <form className="admin-form" onSubmit={handleSave}>
          {groups.length === 0 ? (
            <p>No catalogue skills are available to select.</p>
          ) : (
            <p>Select the skills this event covers. You can choose from multiple disciplines.</p>
          )}

          {groups.map((discipline) => (
            <fieldset
              className="admin-role-group"
              key={discipline.disciplineId}
              disabled={isSaving}
            >
              <legend>{discipline.name}</legend>

              {discipline.skills.map((skill) => {
                const checked = selectedIds.includes(skill.skillId);

                return (
                  <label className="admin-role-option" key={skill.skillId}>
                    <input
                      type="checkbox"
                      checked={checked}
                      disabled={skill.isArchived && !checked}
                      onChange={() => toggleSkill(skill.skillId)}
                    />
                    <span>
                      {skill.name}
                      {skill.isArchived && " (Archived)"}
                    </span>
                  </label>
                );
              })}
            </fieldset>
          ))}

          <p>{selectedIds.length} selected</p>

          <div className="admin-form-actions">
            <button
              className="site-button site-button-primary"
              type="submit"
              disabled={isSaving || !hasChanges}
            >
              {isSaving ? "Saving…" : "Save Skills"}
            </button>

            <button
              className="site-button site-button-secondary"
              type="button"
              disabled={isSaving || !hasChanges}
              onClick={() => {
                setSelectedIds(savedIds);
                setError("");
                setStatus("");
              }}
            >
              Discard changes
            </button>
          </div>
        </form>
      )}
    </section>
  );
}