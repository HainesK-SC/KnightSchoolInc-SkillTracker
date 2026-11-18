import { useState, type FormEvent } from "react";
import { Link } from "react-router";

import { ApiError } from "../api/apiClient";
import {
  getAdminUsers,
  type AdminUser,
} from "../api/adminUsersApi";
import {
  getParticipantSkillTree,
  setParticipantSkillStatus,
  type SkillStatus,
  type SkillTree,
} from "../api/participantProgressApi";
import mainLogo from "../assets/branding/main-logo.svg";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";
import "./AdminProgressPage.css";

const skillStatuses: SkillStatus[] = [
  "Locked",
  "InProgress",
  "Completed",
];

function formatStatus(status: SkillStatus) {
  if (status === "InProgress") {
    return "In Progress";
  }

  return status;
}

function getStatusClass(status: SkillStatus) {
  switch (status) {
    case "Completed":
      return "admin-progress-status-completed";
    case "InProgress":
      return "admin-progress-status-in-progress";
    default:
      return "admin-progress-status-locked";
  }
}

function formatUpdatedDate(value: string | null) {
  if (!value) {
    return "No progress recorded";
  }

  return new Date(value).toLocaleString();
}

function AdminProgressPage() {
  const [email, setEmail] = useState("");
  const [selectedUser, setSelectedUser] =
    useState<AdminUser | null>(null);
  const [skillTree, setSkillTree] =
    useState<SkillTree | null>(null);

  const [selectedStatuses, setSelectedStatuses] = useState<
    Record<number, SkillStatus>
  >({});

  const [isSearching, setIsSearching] = useState(false);
  const [activeSkillId, setActiveSkillId] = useState<
    number | null
  >(null);

  const [statusMessage, setStatusMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  function applySkillTree(tree: SkillTree) {
    const statuses: Record<number, SkillStatus> = {};

    tree.disciplines.forEach((discipline) => {
      discipline.skills.forEach((skill) => {
        statuses[skill.skillId] = skill.status;
      });
    });

    setSkillTree(tree);
    setSelectedStatuses(statuses);
  }

  async function handleSearch(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    const normalizedEmail = email.trim().toLowerCase();

    setSelectedUser(null);
    setSkillTree(null);
    setSelectedStatuses({});
    setStatusMessage("");
    setErrorMessage("");
    setIsSearching(true);

    try {
      const users = await getAdminUsers();

      const matchingUser = users.find(
        (user) =>
          user.email?.trim().toLowerCase() === normalizedEmail,
      );

      if (!matchingUser) {
        setStatusMessage(
          "No user account was found with that email address.",
        );
        return;
      }

      const tree = await getParticipantSkillTree(
        matchingUser.id,
      );

      setSelectedUser(matchingUser);
      applySkillTree(tree);
      setStatusMessage(
        `${matchingUser.displayName} was loaded successfully.`,
      );
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "The participant’s progress could not be loaded.",
        );
      }
    } finally {
      setIsSearching(false);
    }
  }

  function clearSearch() {
    setEmail("");
    setSelectedUser(null);
    setSkillTree(null);
    setSelectedStatuses({});
    setStatusMessage("");
    setErrorMessage("");
  }

  function handleStatusChange(
    skillId: number,
    status: SkillStatus,
  ) {
    setSelectedStatuses((currentStatuses) => ({
      ...currentStatuses,
      [skillId]: status,
    }));
  }

  async function handleSaveStatus(
    skillId: number,
    skillName: string,
  ) {
    if (!selectedUser) {
      return;
    }

    const selectedStatus = selectedStatuses[skillId];

    if (!selectedStatus) {
      setErrorMessage("Select a skill status before saving.");
      return;
    }

    setActiveSkillId(skillId);
    setStatusMessage("");
    setErrorMessage("");

    try {
      await setParticipantSkillStatus(
        selectedUser.id,
        skillId,
        selectedStatus,
      );

      const refreshedTree = await getParticipantSkillTree(
        selectedUser.id,
      );

      applySkillTree(refreshedTree);

      setStatusMessage(
        `${skillName} was marked as ${formatStatus(
          selectedStatus,
        )}.`,
      );
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else if (error instanceof Error) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "The skill status could not be updated.",
        );
      }
    } finally {
      setActiveSkillId(null);
    }
  }

  const allSkills =
    skillTree?.disciplines.flatMap(
      (discipline) => discipline.skills,
    ) ?? [];

  const completedCount = allSkills.filter(
    (skill) => skill.status === "Completed",
  ).length;

  const inProgressCount = allSkills.filter(
    (skill) => skill.status === "InProgress",
  ).length;

  const lockedCount = allSkills.filter(
    (skill) => skill.status === "Locked",
  ).length;

  return (
    <div className="admin-page admin-progress-page">
      <header className="admin-header">
        <div className="admin-header-content">
          <Link
            to="/admin"
            aria-label="Knight School admin dashboard"
          >
            <img
              className="admin-logo"
              src={mainLogo}
              alt="Knight School Inc."
            />
          </Link>

          <nav
            className="admin-navigation"
            aria-label="Admin navigation"
          >
            <Link to="/admin">Dashboard</Link>
            <Link to="/admin/users">Manage users</Link>
            <Link to="/admin/skills">Manage skills</Link>
            <Link to="/profile">My profile</Link>
          </nav>
        </div>
      </header>

      <main className="admin-main">
        <Link
          className="admin-back-link"
          to="/admin"
        >
          ← Back to dashboard
        </Link>

        <p className="admin-eyebrow">
          Training progress
        </p>

        <h1 className="admin-title">
          Participant Progress
        </h1>

        <p className="admin-description">
          Find a participant and update their skills as completed,
          in progress or locked.
        </p>

        <section
          className="admin-panel admin-search-panel"
          aria-labelledby="progress-search-heading"
        >
          <h2 id="progress-search-heading">
            Find a participant
          </h2>

          <form
            className="admin-search-form"
            onSubmit={handleSearch}
          >
            <div className="admin-form-group">
              <label htmlFor="progressSearchEmail">
                Participant email address
              </label>

              <input
                id="progressSearchEmail"
                name="email"
                type="email"
                autoComplete="off"
                maxLength={256}
                value={email}
                onChange={(event) =>
                  setEmail(event.target.value)
                }
                required
              />
            </div>

            <div className="admin-form-actions">
              <button
                className="admin-button"
                type="submit"
                disabled={isSearching}
              >
                {isSearching
                  ? "Searching…"
                  : "Search"}
              </button>

              <button
                className="admin-secondary-button"
                type="button"
                onClick={clearSearch}
                disabled={isSearching}
              >
                Clear
              </button>
            </div>
          </form>
        </section>

        {errorMessage && (
          <p
            className="admin-form-error"
            role="alert"
          >
            {errorMessage}
          </p>
        )}

        {statusMessage && (
          <p
            className="admin-form-status"
            role="status"
          >
            {statusMessage}
          </p>
        )}

        {!selectedUser || !skillTree ? (
          <section
            className="admin-panel admin-user-result"
            aria-labelledby="progress-result-heading"
          >
            <h2 id="progress-result-heading">
              Skill-tree progress
            </h2>

            <div className="admin-empty-result">
              <span
                className="admin-empty-icon"
                aria-hidden="true"
              >
                ⚔
              </span>

              <h3>No participant selected</h3>

              <p>
                Search using a participant’s email address to view
                and update their skill progress.
              </p>
            </div>
          </section>
        ) : (
          <>
            <section
              className="admin-progress-participant"
              aria-labelledby="participant-progress-name"
            >
              <div className="admin-progress-avatar">
                {selectedUser.firstName.charAt(0)}
                {selectedUser.lastName.charAt(0)}
              </div>

              <div>
                <p className="admin-card-category">
                  Selected participant
                </p>

                <h2 id="participant-progress-name">
                  {selectedUser.displayName}
                </h2>

                <p>
                  {selectedUser.email ??
                    "No email address provided"}
                </p>
              </div>
            </section>

            <section
              className="admin-progress-summary"
              aria-label="Progress summary"
            >
              <article>
                <strong>{allSkills.length}</strong>
                <span>Total skills</span>
              </article>

              <article>
                <strong>{completedCount}</strong>
                <span>Completed</span>
              </article>

              <article>
                <strong>{inProgressCount}</strong>
                <span>In progress</span>
              </article>

              <article>
                <strong>{lockedCount}</strong>
                <span>Locked</span>
              </article>
            </section>

            {skillTree.disciplines.length === 0 ? (
              <section className="admin-panel">
                <h2>No active skills available</h2>

                <p>
                  There are no active skills available for this
                  participant.
                </p>
              </section>
            ) : (
              <div className="admin-progress-disciplines">
                {skillTree.disciplines.map((discipline) => (
                  <section
                    className="admin-panel admin-progress-discipline"
                    key={discipline.disciplineId}
                    aria-labelledby={`discipline-${discipline.disciplineId}`}
                  >
                    <div className="admin-progress-discipline-heading">
                      <div>
                        <p className="admin-card-category">
                          Discipline
                        </p>

                        <h2
                          id={`discipline-${discipline.disciplineId}`}
                        >
                          {discipline.name}
                        </h2>
                      </div>

                      <span className="admin-skill-count">
                        {discipline.skills.length}{" "}
                        {discipline.skills.length === 1
                          ? "skill"
                          : "skills"}
                      </span>
                    </div>

                    <div className="admin-progress-skill-list">
                      {discipline.skills.map((skill) => (
                        <article
                          className="admin-progress-skill"
                          key={skill.skillId}
                        >
                          <div className="admin-progress-skill-content">
                            <div className="admin-progress-skill-heading">
                              <h3>{skill.name}</h3>

                              <span
                                className={`admin-progress-status ${getStatusClass(
                                  skill.status,
                                )}`}
                              >
                                {formatStatus(skill.status)}
                              </span>
                            </div>

                            <p>{skill.description}</p>

                            {skill.benefits && (
                              <p className="admin-progress-benefits">
                                <strong>Benefits:</strong>{" "}
                                {skill.benefits}
                              </p>
                            )}

                            <p className="admin-progress-updated">
                              {formatUpdatedDate(
                                skill.updatedAtUtc,
                              )}
                            </p>
                          </div>

                          <div className="admin-progress-controls">
                            <label
                              htmlFor={`skill-status-${skill.skillId}`}
                            >
                              Update status
                            </label>

                            <select
                              id={`skill-status-${skill.skillId}`}
                              value={
                                selectedStatuses[
                                  skill.skillId
                                ] ?? skill.status
                              }
                              onChange={(event) =>
                                handleStatusChange(
                                  skill.skillId,
                                  event.target
                                    .value as SkillStatus,
                                )
                              }
                              disabled={
                                activeSkillId === skill.skillId
                              }
                            >
                              {skillStatuses.map((status) => (
                                <option
                                  key={status}
                                  value={status}
                                >
                                  {formatStatus(status)}
                                </option>
                              ))}
                            </select>

                            <button
                              className="admin-button"
                              type="button"
                              disabled={
                                activeSkillId === skill.skillId
                              }
                              onClick={() =>
                                void handleSaveStatus(
                                  skill.skillId,
                                  skill.name,
                                )
                              }
                            >
                              {activeSkillId === skill.skillId
                                ? "Saving…"
                                : "Save status"}
                            </button>
                          </div>
                        </article>
                      ))}
                    </div>
                  </section>
                ))}
              </div>
            )}
          </>
        )}
      </main>
    </div>
  );
}

export default AdminProgressPage;