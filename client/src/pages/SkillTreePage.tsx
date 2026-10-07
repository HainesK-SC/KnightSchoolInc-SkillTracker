import { Link } from "react-router";

import "./SkillTreePage.css";

export default function SkillTreePage() {
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
            <p className="ks-eyebrow">Your training journey</p>
            <h2 id="skill-tree-title">Skill Map</h2>
          </div>

          <div className="ks-legend" aria-label="Skill status legend">
            <span>
              <i className="completed" />
              Completed
            </span>

            <span>
              <i className="in-progress" />
              In Progress
            </span>

            <span>
              <i className="available" />
              Available
            </span>

            <span>
              <i className="locked" />
              Locked
            </span>
          </div>
        </div>

        <div className="ks-empty-tree" role="status">
          <div className="ks-empty-content">
            <div className="ks-empty-medallion" aria-hidden="true">
              ⚔
            </div>

            <p className="ks-eyebrow">Your journey awaits</p>

            <h3>Your skill tree is not available yet</h3>

            <p>
              Once Knight School adds a training path to your account,
              your completed, in-progress, available, and locked skills
              will appear here.
            </p>

            <p className="ks-read-only">
              Skill progress can only be approved by an authorized
              instructor or administrator.
            </p>

            <Link className="ks-profile-button" to="/profile">
              Return to profile
            </Link>
          </div>
        </div>
      </section>
    </main>
  );
}