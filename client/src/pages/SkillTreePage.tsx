import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";
import { Link, useNavigate } from "react-router";

import { ApiError } from "../api/apiClient";
import {
  getMySkillTree,
  type SkillStatus,
  type SkillTree,
} from "../api/participantProgressApi";
import {
  DisciplineIcon,
  SkillIcon,
} from "../components/SkillIcons";
import "./SkillTreePage.css";

const PATH_THEMES = [
  "crimson",
  "gold",
  "blue",
  "green",
  "purple",
];

function formatStatus(status: SkillStatus) {
  switch (status) {
    case "InProgress":
      return "In Progress";
    case "Completed":
      return "Completed";
    case "Locked":
      return "Locked";
    default:
      return status;
  }
}

function getStatusClass(status: SkillStatus) {
  switch (status) {
    case "Completed":
      return "completed";
    case "InProgress":
      return "in-progress";
    case "Locked":
      return "locked";
    default:
      return "locked";
  }
}

function formatUpdatedDate(updatedAtUtc: string | null) {
  if (!updatedAtUtc) {
    return "No progress recorded";
  }

  const parsedDate = new Date(updatedAtUtc);

  if (Number.isNaN(parsedDate.getTime())) {
    return "Progress date unavailable";
  }

  return new Intl.DateTimeFormat(undefined, {
    year: "numeric",
    month: "long",
    day: "numeric",
  }).format(parsedDate);
}

function SkillTreePage() {
  const navigate = useNavigate();

  const [skillTree, setSkillTree] =
    useState<SkillTree | null>(null);
  const [selectedSkillId, setSelectedSkillId] =
    useState<number | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");

  const loadSkillTree = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const response = await getMySkillTree();
      setSkillTree(response);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        navigate("/login", { replace: true });
        return;
      }

      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "Your skill tree could not be loaded. Please try again.",
        );
      }
    } finally {
      setIsLoading(false);
    }
  }, [navigate]);

  useEffect(() => {
    void loadSkillTree();
  }, [loadSkillTree]);

  const allSkills = useMemo(
    () =>
      skillTree?.disciplines.flatMap(
        (discipline) => discipline.skills,
      ) ?? [],
    [skillTree],
  );

  useEffect(() => {
    if (allSkills.length === 0) {
      setSelectedSkillId(null);
      return;
    }

    const selectedSkillStillExists = allSkills.some(
      (skill) => skill.skillId === selectedSkillId,
    );

    if (!selectedSkillStillExists) {
      setSelectedSkillId(allSkills[0].skillId);
    }
  }, [allSkills, selectedSkillId]);

  const selectedSkill = useMemo(
    () =>
      allSkills.find(
        (skill) => skill.skillId === selectedSkillId,
      ) ?? null,
    [allSkills, selectedSkillId],
  );

  const selectedDiscipline = useMemo(
    () =>
      skillTree?.disciplines.find((discipline) =>
        discipline.skills.some(
          (skill) => skill.skillId === selectedSkillId,
        ),
      ) ?? null,
    [skillTree, selectedSkillId],
  );

  const completedCount = allSkills.filter(
    (skill) => skill.status === "Completed",
  ).length;

  const inProgressCount = allSkills.filter(
    (skill) => skill.status === "InProgress",
  ).length;

  const lockedCount = allSkills.filter(
    (skill) => skill.status === "Locked",
  ).length;

  const completionPercentage =
    allSkills.length > 0
      ? Math.round((completedCount / allSkills.length) * 100)
      : 0;

  return (
    <main className="ks-skill-page">
      <header className="ks-skill-header">
        <div className="ks-brand">
          <div className="ks-crest" aria-hidden="true">
            KS
          </div>

          <div>
            <p className="ks-eyebrow">
              Knight School Progression
            </p>

            <h1>Path of the Knight</h1>

            <p className="ks-header-description">
              Master techniques, complete trials, and unlock
              your next path.
            </p>
          </div>
        </div>

        <Link className="ks-profile-button" to="/profile">
          Back to profile
        </Link>
      </header>

      <section
        className="ks-tree-panel"
        aria-labelledby="skill-tree-title"
      >
        <div className="ks-panel-heading">
          <div>
            <p className="ks-eyebrow">
              Your training journey
            </p>

            <h2 id="skill-tree-title">Hall of Disciplines</h2>
          </div>

          <div
            className="ks-legend"
            aria-label="Skill status legend"
          >
            <span>
              <i className="completed" />
              Completed
            </span>

            <span>
              <i className="in-progress" />
              In Progress
            </span>

            <span>
              <i className="locked" />
              Locked
            </span>
          </div>
        </div>

        {isLoading && (
          <div className="ks-state-panel" role="status">
            <div
              className="ks-loading-medallion"
              aria-hidden="true"
            >
              ⚔
            </div>

            <h3>Opening the Hall of Disciplines…</h3>

            <p>
              Please wait while Knight School retrieves your
              training record.
            </p>
          </div>
        )}

        {!isLoading && errorMessage && (
          <div
            className="ks-state-panel ks-error-panel"
            role="alert"
          >
            <div
              className="ks-empty-medallion"
              aria-hidden="true"
            >
              !
            </div>

            <h3>Your skill tree could not be opened</h3>

            <p>{errorMessage}</p>

            <button
              className="ks-retry-button"
              type="button"
              onClick={() => void loadSkillTree()}
            >
              Try again
            </button>
          </div>
        )}

        {!isLoading &&
          !errorMessage &&
          allSkills.length === 0 && (
            <div className="ks-empty-tree" role="status">
              <div className="ks-empty-content">
                <div
                  className="ks-empty-medallion"
                  aria-hidden="true"
                >
                  ⚔
                </div>

                <p className="ks-eyebrow">
                  Your journey awaits
                </p>

                <h3>Your skill tree is not available yet</h3>

                <p>
                  Once Knight School adds a training path to
                  your account, your completed, in-progress,
                  and locked skills will appear here.
                </p>

                <p className="ks-read-only">
                  Skill progress can only be approved by an
                  authorized instructor or administrator.
                </p>

                <Link
                  className="ks-profile-button"
                  to="/profile"
                >
                  Return to profile
                </Link>
              </div>
            </div>
          )}

        {!isLoading &&
          !errorMessage &&
          skillTree &&
          allSkills.length > 0 && (
            <>
              <section
                className="ks-summary"
                aria-label="Skill-tree progress summary"
              >
                <div className="ks-summary-introduction">
                  <div>
                    <p className="ks-summary-label">
                      Overall mastery
                    </p>

                    <strong className="ks-summary-percentage">
                      {completionPercentage}%
                    </strong>
                  </div>

                  <div
                    className="ks-progress-bar"
                    role="progressbar"
                    aria-label="Overall skill completion"
                    aria-valuemin={0}
                    aria-valuemax={100}
                    aria-valuenow={completionPercentage}
                  >
                    <span
                      style={{
                        width: `${completionPercentage}%`,
                      }}
                    />
                  </div>
                </div>

                <div className="ks-summary-stat">
                  <strong>{allSkills.length}</strong>
                  <span>Total skills</span>
                </div>

                <div className="ks-summary-stat">
                  <strong>{completedCount}</strong>
                  <span>Completed</span>
                </div>

                <div className="ks-summary-stat">
                  <strong>{inProgressCount}</strong>
                  <span>In progress</span>
                </div>

                <div className="ks-summary-stat">
                  <strong>{lockedCount}</strong>
                  <span>Locked</span>
                </div>
              </section>

              <section
                className="ks-path-board"
                aria-label="Skill progression paths"
              >
                <span
                  className="ks-path-board-decoration"
                  aria-hidden="true"
                />

                <div className="ks-path-scroll">
                  <div className="ks-path-columns">
                    {skillTree.disciplines.map(
                      (discipline, disciplineIndex) => {
                        const theme =
                          PATH_THEMES[
                            disciplineIndex %
                              PATH_THEMES.length
                          ];

                        const disciplineCompletedCount =
                          discipline.skills.filter(
                            (skill) =>
                              skill.status === "Completed",
                          ).length;

                        return (
                          <article
                            className={`ks-path ks-path-${theme}`}
                            key={discipline.disciplineId}
                          >
                            <header className="ks-path-header">
                              <div
                                className="ks-path-crest"
                                aria-hidden="true"
                              >
                                <DisciplineIcon
                                  name={discipline.name}
                                  className="ks-discipline-icon"
                                />
                              </div>

                              <div>
                                <p>Discipline path</p>
                                <h3>{discipline.name}</h3>

                                <span className="ks-path-progress">
                                  {disciplineCompletedCount} of{" "}
                                  {discipline.skills.length} mastered
                                </span>
                              </div>
                            </header>

                            <div className="ks-path-track">
                              {discipline.skills.map(
                                (skill, skillIndex) => {
                                  const statusClass =
                                    getStatusClass(
                                      skill.status,
                                    );

                                  const isSelected =
                                    selectedSkillId ===
                                    skill.skillId;

                                  return (
                                    <div
                                      className="ks-path-step"
                                      key={skill.skillId}
                                    >
                                      {skillIndex > 0 && (
                                        <span
                                          className="ks-path-connector"
                                          aria-hidden="true"
                                        />
                                      )}

                                      <button
                                        className={[
                                          "ks-path-node",
                                          `ks-path-node-${statusClass}`,
                                          isSelected
                                            ? "ks-path-node-selected"
                                            : "",
                                        ]
                                          .filter(Boolean)
                                          .join(" ")}
                                        type="button"
                                        aria-pressed={isSelected}
                                        aria-label={`${skill.name}. ${formatStatus(
                                          skill.status,
                                        )}.`}
                                        onClick={() =>
                                          setSelectedSkillId(
                                            skill.skillId,
                                          )
                                        }
                                      >
                                        <span
                                          className="ks-path-node-ring"
                                          aria-hidden="true"
                                        >
                                          <span>
                                            <SkillIcon
                                              name={skill.name}
                                              className="ks-skill-icon"
                                            />
                                          </span>
                                        </span>

                                        <strong>
                                          {skill.name}
                                        </strong>

                                        <span
                                          className={`ks-path-status ks-path-status-${statusClass}`}
                                        >
                                          {formatStatus(
                                            skill.status,
                                          )}
                                        </span>
                                      </button>
                                    </div>
                                  );
                                },
                              )}
                            </div>
                          </article>
                        );
                      },
                    )}
                  </div>
                </div>
              </section>

              {selectedSkill && (
                <section
                  className="ks-selected-skill"
                  aria-live="polite"
                  aria-labelledby="selected-skill-title"
                >
                  <div className="ks-selected-identity">
                    <div
                      className={`ks-selected-medallion ks-selected-medallion-${getStatusClass(
                        selectedSkill.status,
                      )}`}
                      aria-hidden="true"
                    >
                      <SkillIcon
                        name={selectedSkill.name}
                        className="ks-skill-icon"
                      />
                    </div>

                    <div>
                      <p className="ks-detail-eyebrow">
                        {selectedDiscipline?.name ??
                          "Knight School"}{" "}
                        discipline
                      </p>

                      <h3 id="selected-skill-title">
                        {selectedSkill.name}
                      </h3>

                      <span
                        className={`ks-detail-status ks-path-status-${getStatusClass(
                          selectedSkill.status,
                        )}`}
                      >
                        {formatStatus(selectedSkill.status)}
                      </span>
                    </div>
                  </div>

                  <div className="ks-selected-information">
                    <div className="ks-detail-section">
                      <h4>Skill description</h4>
                      <p>{selectedSkill.description}</p>
                    </div>

                    <div className="ks-detail-section">
                      <h4>
                        Therapeutic and developmental benefits
                      </h4>

                      <p>
                        {selectedSkill.benefits ||
                          "No benefits have been added for this skill yet."}
                      </p>
                    </div>

                    <div className="ks-selected-footer">
                      <p className="ks-detail-updated">
                        Last updated:{" "}
                        {formatUpdatedDate(
                          selectedSkill.updatedAtUtc,
                        )}
                      </p>

                      <p className="ks-read-only">
                        Progress is read-only on this page.
                        Instructors and administrators can update
                        your skill status.
                      </p>
                    </div>
                  </div>
                </section>
              )}

              <p className="ks-path-note">
                Select any skill medallion to view its
                description, benefits, current status, and latest
                progress date.
              </p>
            </>
          )}
      </section>
    </main>
  );
}

export default SkillTreePage;