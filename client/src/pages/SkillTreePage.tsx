import { useState } from "react";
import "./SkillTreePage.css";

type SkillStatus = "completed" | "in-progress" | "available" | "locked";

type Skill = {
  id: string;
  name: string;
  category: string;
  description: string;
  status: SkillStatus;
  icon: string;
  x: number;
  y: number;
  xp: number;
  progress?: number;
  prerequisites: string[];
  unlocks: string[];
  completedDate?: string;
  approvedBy?: string;
};

const skills: Skill[] = [
  {
    id: "foundations",
    name: "Combat Foundations",
    category: "Foundational Training",
    description:
      "Learn safe stance, movement, distance, control, and stage-combat awareness.",
    status: "completed",
    icon: "⚔",
    x: 50,
    y: 85,
    xp: 100,
    prerequisites: [],
    unlocks: ["Mid Strike", "High Block", "Combat Footwork"],
    completedDate: "September 10, 2026",
    approvedBy: "Instructor Taylor",
  },
  {
    id: "mid-strike",
    name: "Mid Strike",
    category: "Weapon Technique",
    description:
      "Deliver a controlled mid-level strike using correct distance and recovery.",
    status: "completed",
    icon: "✦",
    x: 22,
    y: 275,
    xp: 150,
    prerequisites: ["Combat Foundations"],
    unlocks: ["Level 1 Knight Test", "Advanced Choreography"],
    completedDate: "September 18, 2026",
    approvedBy: "Instructor Taylor",
  },
  {
    id: "high-block",
    name: "High Block",
    category: "Defensive Technique",
    description:
      "Safely defend against an incoming high strike with proper timing and control.",
    status: "in-progress",
    icon: "🛡",
    x: 50,
    y: 275,
    xp: 150,
    progress: 60,
    prerequisites: ["Combat Foundations"],
    unlocks: ["Level 1 Knight Test", "Advanced Choreography"],
  },
  {
    id: "footwork",
    name: "Combat Footwork",
    category: "Movement",
    description:
      "Use controlled advances, retreats, pivots, and safe performance spacing.",
    status: "completed",
    icon: "◆",
    x: 78,
    y: 275,
    xp: 125,
    prerequisites: ["Combat Foundations"],
    unlocks: ["Mounted Combat"],
    completedDate: "September 21, 2026",
    approvedBy: "Instructor Morgan",
  },
  {
    id: "knight-test",
    name: "Level 1 Knight Test",
    category: "Assessment",
    description:
      "Complete the first formal assessment of foundational combat techniques.",
    status: "locked",
    icon: "♜",
    x: 22,
    y: 500,
    xp: 300,
    prerequisites: ["Mid Strike", "High Block"],
    unlocks: ["Knight Performance"],
  },
  {
    id: "choreography",
    name: "Advanced Choreography",
    category: "Performance",
    description:
      "Combine approved offensive and defensive techniques into a staged sequence.",
    status: "locked",
    icon: "⚔",
    x: 50,
    y: 500,
    xp: 350,
    prerequisites: ["Mid Strike", "High Block"],
    unlocks: ["Knight Performance"],
  },
  {
    id: "mounted-combat",
    name: "Mounted Combat",
    category: "Specialized Training",
    description:
      "Learn the foundations of safe mounted movement and combat performance.",
    status: "available",
    icon: "♞",
    x: 78,
    y: 500,
    xp: 300,
    prerequisites: ["Combat Footwork"],
    unlocks: ["Knight Performance"],
  },
  {
    id: "performance",
    name: "Knight Performance",
    category: "Mastery Milestone",
    description:
      "Perform a complete choreographed duel using approved Knight School skills.",
    status: "locked",
    icon: "♛",
    x: 50,
    y: 720,
    xp: 500,
    prerequisites: [
      "Level 1 Knight Test",
      "Advanced Choreography",
      "Mounted Combat",
    ],
    unlocks: [],
  },
];

const connections = [
  ["foundations", "mid-strike"],
  ["foundations", "high-block"],
  ["foundations", "footwork"],
  ["mid-strike", "knight-test"],
  ["mid-strike", "choreography"],
  ["high-block", "knight-test"],
  ["high-block", "choreography"],
  ["footwork", "mounted-combat"],
  ["knight-test", "performance"],
  ["choreography", "performance"],
  ["mounted-combat", "performance"],
];

const statusLabels: Record<SkillStatus, string> = {
  completed: "Completed",
  "in-progress": "In Progress",
  available: "Available",
  locked: "Locked",
};

const statusSymbols: Record<SkillStatus, string> = {
  completed: "✓",
  "in-progress": "◐",
  available: "!",
  locked: "🔒",
};

function getSkill(id: string) {
  return skills.find((skill) => skill.id === id)!;
}

export default function SkillTreePage() {
  const [selectedId, setSelectedId] = useState("high-block");
  const selectedSkill = getSkill(selectedId);

  return (
    <main className="ks-skill-page">
      <header className="ks-skill-header">
        <div className="ks-brand">
          <div className="ks-crest" aria-hidden="true">
            KS
          </div>

          <div>
            <p className="ks-eyebrow">Knight School Progression</p>
            <h1>Path of the Knight</h1>
            <p className="ks-header-description">
              Master techniques, complete trials, and unlock your next path.
            </p>
          </div>
        </div>

        <a className="ks-profile-button" href="/profile">
          Back to Profile
        </a>
      </header>

      <section className="ks-player-hud" aria-label="Player progress">
        <div className="ks-rank">
          <span className="ks-rank-label">Current Rank</span>
          <strong>Knight in Training</strong>
          <span>Level 2</span>
        </div>

        <div className="ks-xp">
          <div className="ks-xp-heading">
            <span>Experience</span>
            <strong>650 / 1,000 XP</strong>
          </div>

          <progress value="650" max="1000">
            650 of 1000 experience points
          </progress>

          <small>350 XP until Level 3</small>
        </div>

        <div className="ks-stat">
          <strong>4</strong>
          <span>Skills mastered</span>
        </div>

        <div className="ks-stat">
          <strong>3</strong>
          <span>Class streak</span>
        </div>
      </section>

      <div className="ks-tree-layout">
        <section className="ks-tree-panel" aria-labelledby="skill-tree-title">
          <div className="ks-panel-heading">
            <div>
              <p className="ks-eyebrow">Choose your path</p>
              <h2 id="skill-tree-title">Skill Map</h2>
            </div>

            <div className="ks-legend" aria-label="Skill status legend">
              <span><i className="completed" />Completed</span>
              <span><i className="in-progress" />In Progress</span>
              <span><i className="available" />Available</span>
              <span><i className="locked" />Locked</span>
            </div>
          </div>

          <div className="ks-tree-scroll">
            <div className="ks-game-tree">
              <svg
                className="ks-paths"
                viewBox="0 0 1000 810"
                preserveAspectRatio="none"
                aria-hidden="true"
              >
                {connections.map(([startId, endId]) => {
                  const start = getSkill(startId);
                  const end = getSkill(endId);
                  const middleY = (start.y + end.y) / 2;

                  return (
                    <path
                      key={`${startId}-${endId}`}
                      className={`ks-path ks-path-${end.status}`}
                      d={`M ${start.x * 10} ${start.y + 45}
                          C ${start.x * 10} ${middleY},
                            ${end.x * 10} ${middleY},
                            ${end.x * 10} ${end.y - 45}`}
                    />
                  );
                })}
              </svg>

              {skills.map((skill) => (
                <button
                  key={skill.id}
                  type="button"
                  className={`ks-skill-node ks-${skill.status} ${
                    selectedId === skill.id ? "ks-selected" : ""
                  }`}
                  style={{
                    left: `${skill.x}%`,
                    top: `${skill.y}px`,
                  }}
                  onClick={() => setSelectedId(skill.id)}
                  aria-pressed={selectedId === skill.id}
                  aria-label={`${skill.name}. ${
                    statusLabels[skill.status]
                  }. Select to view details.`}
                >
                  <span className="ks-node-status" aria-hidden="true">
                    {statusSymbols[skill.status]}
                  </span>

                  <span className="ks-node-medallion" aria-hidden="true">
                    {skill.icon}
                  </span>

                  <span className="ks-node-name">{skill.name}</span>
                  <span className="ks-node-label">
                    {statusLabels[skill.status]}
                  </span>
                </button>
              ))}
            </div>
          </div>

          <p className="ks-scroll-message">
            Select any skill to view its requirements and rewards.
          </p>
        </section>

        <aside className="ks-details" aria-live="polite">
          <div className="ks-details-top">
            <span className={`ks-status-pill ks-${selectedSkill.status}`}>
              {statusSymbols[selectedSkill.status]}{" "}
              {statusLabels[selectedSkill.status]}
            </span>

            <span className="ks-xp-reward">+{selectedSkill.xp} XP</span>
          </div>

          <p className="ks-detail-category">{selectedSkill.category}</p>
          <h2>{selectedSkill.name}</h2>
          <p className="ks-detail-description">
            {selectedSkill.description}
          </p>

          {selectedSkill.progress !== undefined && (
            <div className="ks-detail-section">
              <div className="ks-detail-progress-heading">
                <h3>Training Progress</h3>
                <strong>{selectedSkill.progress}%</strong>
              </div>

              <progress value={selectedSkill.progress} max="100">
                {selectedSkill.progress}%
              </progress>

              <p className="ks-objective">
                Next objective: demonstrate this technique safely for an
                instructor.
              </p>
            </div>
          )}

          <div className="ks-detail-section">
            <h3>Prerequisites</h3>

            {selectedSkill.prerequisites.length === 0 ? (
              <p>No prerequisites. This is a foundational skill.</p>
            ) : (
              <ul className="ks-requirement-list">
                {selectedSkill.prerequisites.map((requirement) => {
                  const requirementSkill = skills.find(
                    (skill) => skill.name === requirement,
                  );

                  return (
                    <li key={requirement}>
                      <span
                        className={`ks-requirement-icon ks-${
                          requirementSkill?.status ?? "locked"
                        }`}
                      >
                        {requirementSkill
                          ? statusSymbols[requirementSkill.status]
                          : "•"}
                      </span>

                      <span>
                        <strong>{requirement}</strong>
                        <small>
                          {requirementSkill
                            ? statusLabels[requirementSkill.status]
                            : "Required"}
                        </small>
                      </span>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>

          {selectedSkill.unlocks.length > 0 && (
            <div className="ks-detail-section">
              <h3>Unlocks</h3>
              <p>{selectedSkill.unlocks.join(" • ")}</p>
            </div>
          )}

          {selectedSkill.completedDate && (
            <div className="ks-completion-card">
              <strong>Mastery Recorded</strong>
              <span>{selectedSkill.completedDate}</span>
              <span>Approved by {selectedSkill.approvedBy}</span>
            </div>
          )}

          {selectedSkill.status === "locked" && (
            <div className="ks-locked-notice">
              Complete all prerequisites to unlock this trial.
            </div>
          )}

          {selectedSkill.status === "available" && (
            <div className="ks-available-notice">
              This training path is available to begin.
            </div>
          )}

          <p className="ks-read-only">
            Skill progress is read-only. Only an authorized instructor or
            administrator can approve completed skills.
          </p>
        </aside>
      </div>
    </main>
  );
}