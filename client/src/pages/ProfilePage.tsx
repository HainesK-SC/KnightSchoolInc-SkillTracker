import { Link } from "react-router";

import mainLogo from "../assets/branding/main-logo.svg";
import "./ProfilePage.css";

function ProfilePage() {
  return (
    <div className="profile-page">
      <header className="profile-header">
        <div className="profile-header-content">
          <Link to="/profile" aria-label="Knight School profile home">
            <img
              className="profile-logo"
              src={mainLogo}
              alt="Knight School Inc."
            />
          </Link>

          <nav className="profile-navigation" aria-label="Main navigation">
            <Link to="/profile" aria-current="page">
              My profile
            </Link>
            <Link to="/login">Sign out</Link>
          </nav>
        </div>
      </header>

      <main className="profile-main">
        <section className="profile-hero" aria-labelledby="profile-title">
          <p className="profile-avatar" aria-hidden="true">
            AM
          </p>

          <div>
            <h1 id="profile-title">Alex Morgan</h1>
            <p className="profile-role">Knight in Training · Level 2</p>

            <div className="profile-progress-label">
              <span>Progress to Level 3</span>
              <span>65%</span>
            </div>

            <progress
              className="profile-progress"
              value="65"
              max="100"
              aria-label="65 percent progress toward Level 3"
            >
              65%
            </progress>
          </div>
        </section>

        <div className="profile-grid">
          <section className="profile-card" aria-labelledby="account-heading">
            <h2 id="account-heading">Account details</h2>

            <dl className="profile-details">
              <div className="profile-detail">
                <dt>Email address</dt>
                <dd>alex.morgan@example.com</dd>
              </div>

              <div className="profile-detail">
                <dt>Membership status</dt>
                <dd>Active participant</dd>
              </div>

              <div className="profile-detail">
                <dt>Primary instructor</dt>
                <dd>Instructor Taylor</dd>
              </div>

              <div className="profile-detail">
                <dt>Training program</dt>
                <dd>Foundations of Stage Combat</dd>
              </div>
            </dl>

            <button className="profile-action" type="button">
              Edit profile
            </button>
          </section>

          <section className="profile-card" aria-labelledby="skills-heading">
            <h2 id="skills-heading">Recent skills</h2>

            <ul className="skill-list">
              <li className="skill-item">
                <span className="skill-name">Mid Strike</span>
                <span className="skill-status">Completed</span>
              </li>

              <li className="skill-item">
                <span className="skill-name">High Block</span>
                <span className="skill-status">Completed</span>
              </li>

              <li className="skill-item">
                <span className="skill-name">
                  Introduction to War Horses
                </span>
                <span className="skill-status">In progress</span>
              </li>

              <li className="skill-item">
                <span className="skill-name">Level 1 Knight Test</span>
                <span className="skill-status">Completed</span>
              </li>
            </ul>

            <button className="profile-action" type="button">
              View skill tree
            </button>
          </section>
        </div>
      </main>
    </div>
  );
}

export default ProfilePage;