import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router";

import { ApiError } from "../api/apiClient";
import {
  archiveAdminSkill,
  deleteAdminSkill,
  getAdminSkillCatalog,
  unarchiveAdminSkill,
  type DisciplineCatalog,
  type Skill,
} from "../api/adminSkillsApi";
import mainLogo from "../assets/branding/main-logo.svg";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";
import "./AdminSkillsPage.css";

function AdminSkillsPage() {
  const [catalog, setCatalog] = useState<DisciplineCatalog[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [activeSkillId, setActiveSkillId] = useState<number | null>(
    null,
  );
  const [skillPendingDelete, setSkillPendingDelete] =
    useState<Skill | null>(null);
  const [errorMessage, setErrorMessage] = useState("");
  const [statusMessage, setStatusMessage] = useState("");

  const loadCatalog = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const response = await getAdminSkillCatalog();
      setCatalog(response);
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "The skill catalogue could not be loaded.",
        );
      }
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadCatalog();
  }, [loadCatalog]);

  async function handleArchive(skill: Skill) {
    setActiveSkillId(skill.skillId);
    setErrorMessage("");
    setStatusMessage("");

    try {
      if (skill.isArchived) {
        await unarchiveAdminSkill(skill.skillId);
        setStatusMessage(`${skill.name} was restored.`);
      } else {
        await archiveAdminSkill(skill.skillId);
        setStatusMessage(`${skill.name} was archived.`);
      }

      await loadCatalog();
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage("The skill could not be updated.");
      }
    } finally {
      setActiveSkillId(null);
    }
  }

  async function confirmDelete() {
    if (!skillPendingDelete) {
      return;
    }

    const skill = skillPendingDelete;

    setActiveSkillId(skill.skillId);
    setErrorMessage("");
    setStatusMessage("");

    try {
      await deleteAdminSkill(skill.skillId);

      setStatusMessage(`${skill.name} was deleted.`);
      setSkillPendingDelete(null);

      await loadCatalog();
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage("The skill could not be deleted.");
      }
    } finally {
      setActiveSkillId(null);
    }
  }

  return (
    <div className="admin-page">
      <header className="admin-header">
        <div className="admin-header-content">
          <Link to="/admin" aria-label="Knight School dashboard">
            <img
              className="admin-logo"
              src={mainLogo}
              alt="Knight School Inc."
            />
          </Link>

          <nav
            className="admin-navigation"
            aria-label="Administrator navigation"
          >
            <Link to="/admin">Dashboard</Link>
            <Link to="/admin/skills" aria-current="page">
              Skills
            </Link>
            <Link to="/profile">My profile</Link>
          </nav>
        </div>
      </header>

      <main className="admin-main">
        <Link className="admin-back-link" to="/admin">
          ← Back to dashboard
        </Link>

        <p className="admin-eyebrow">Training management</p>
        <h1 className="admin-title">Skills Management</h1>

        <p className="admin-description">
          Add, update, archive or remove Knight School skills and
          course content.
        </p>

        <div className="admin-form-actions">
          <Link
            className="admin-button admin-action-link"
            to="/admin/skills/new"
          >
            Add new skill
          </Link>

          <Link
            className="admin-secondary-button"
            to="/admin/disciplines/new"
            >
            Add discipline
        </Link>

          <button
            className="admin-secondary-button"
            type="button"
            onClick={() => void loadCatalog()}
            disabled={isLoading}
          >
            {isLoading ? "Loading…" : "Refresh"}
          </button>
        </div>

        {errorMessage && (
          <p className="admin-form-error" role="alert">
            {errorMessage}
          </p>
        )}

        {statusMessage && (
          <p className="admin-form-status" role="status">
            {statusMessage}
          </p>
        )}

        {isLoading && (
          <section className="admin-panel" role="status">
            <h2>Loading skill catalogue…</h2>
          </section>
        )}

        {!isLoading && catalog.length === 0 && (
          <section className="admin-panel">
            <h2>No skills available</h2>
            <p>
              Create the first discipline and skill to begin the
              Knight School catalogue.
            </p>
          </section>
        )}

        {!isLoading &&
  catalog.map((discipline) => (
    <section
      className="admin-panel admin-skills-section"
      key={discipline.disciplineId}
    >
      <div className="admin-panel-heading">
        <div>
          <p className="admin-card-category">
            Discipline
          </p>

          <h2>{discipline.name}</h2>
        </div>

        <span className="admin-skill-count">
          {discipline.skills.length}{" "}
          {discipline.skills.length === 1
            ? "skill"
            : "skills"}
        </span>
      </div>

      {discipline.skills.length === 0 ? (
        <p className="admin-skills-empty">
          No skills have been added to this discipline.
        </p>
      ) : (
        <div className="admin-table-wrapper">
          <table className="admin-table admin-skills-table">
            <thead>
              <tr>
                <th scope="col">Skill</th>
                <th scope="col">Description</th>
                <th scope="col">Status</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>

            <tbody>
              {discipline.skills.map((skill) => (
                <tr key={skill.skillId}>
                  <td>
                    <strong>{skill.name}</strong>
                  </td>

                  <td>{skill.description}</td>

                  <td>
                    <span
                      className={`admin-skill-status ${
                        skill.isArchived
                          ? "admin-skill-status-archived"
                          : "admin-skill-status-active"
                      }`}
                    >
                      {skill.isArchived
                        ? "Archived"
                        : "Active"}
                    </span>
                  </td>

                  <td>
                    <div className="admin-skill-actions">
                      <Link
                        className="admin-secondary-button"
                        to={`/admin/skills/${skill.skillId}/edit`}
                      >
                        Edit
                      </Link>

                      <button
                        className="admin-secondary-button"
                        type="button"
                        disabled={
                          activeSkillId === skill.skillId
                        }
                        onClick={() =>
                          void handleArchive(skill)
                        }
                      >
                        {skill.isArchived
                          ? "Restore"
                          : "Archive"}
                      </button>

                      <button
                        className="admin-danger-button"
                        type="button"
                        disabled={
                          activeSkillId === skill.skillId
                        }
                        onClick={() => setSkillPendingDelete(skill)}
                      >
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  ))}
      </main>
        {skillPendingDelete && (
        <div className="admin-dialog-backdrop">
          <section
            className="admin-confirm-dialog"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="delete-skill-title"
            aria-describedby="delete-skill-description"
          >
            <p className="admin-card-category">
              Permanent action
            </p>

            <h2 id="delete-skill-title">
              Delete {skillPendingDelete.name}?
            </h2>

            <p id="delete-skill-description">
              This permanently removes the skill from the Knight
              School catalogue. This action cannot be undone.
            </p>

            <div className="admin-dialog-actions">
              <button
                className="admin-secondary-button"
                type="button"
                onClick={() => setSkillPendingDelete(null)}
                disabled={activeSkillId !== null}
              >
                Cancel
              </button>

              <button
                className="admin-danger-button"
                type="button"
                onClick={() => void confirmDelete()}
                disabled={activeSkillId !== null}
              >
                {activeSkillId !== null
                  ? "Deleting…"
                  : "Delete skill"}
              </button>
            </div>
          </section>
        </div>
      )}


    </div>
  );
}

export default AdminSkillsPage;