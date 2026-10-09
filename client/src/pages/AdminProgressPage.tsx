import { useState, type FormEvent } from "react";
import { Link } from "react-router";

import mainLogo from "../assets/branding/main-logo.svg";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";

function AdminProgressPage() {
  const [email, setEmail] = useState("");
  const [statusMessage, setStatusMessage] = useState("");

  function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setStatusMessage(
      "The participant-progress search is ready. Progress will be displayed when the skill-tree endpoint is connected.",
    );
  }

  function clearSearch() {
    setEmail("");
    setStatusMessage("");
  }

  return (
    <div className="admin-page">
      <header className="admin-header">
        <div className="admin-header-content">
          <Link to="/admin" aria-label="Knight School admin dashboard">
            <img
              className="admin-logo"
              src={mainLogo}
              alt="Knight School Inc."
            />
          </Link>

          <nav className="admin-navigation" aria-label="Admin navigation">
            <Link to="/admin">Dashboard</Link>
            <Link to="/admin/users">Manage users</Link>
            <Link to="/profile">My profile</Link>
          </nav>
        </div>
      </header>

      <main className="admin-main">
        <Link className="admin-back-link" to="/admin">
          ← Back to dashboard
        </Link>

        <p className="admin-eyebrow">Training progress</p>
        <h1 className="admin-title">Participant Progress</h1>

        <p className="admin-description">
          Search for a participant to view their completed, in-progress,
          available and locked skills.
        </p>

        <section
          className="admin-panel admin-search-panel"
          aria-labelledby="progress-search-heading"
        >
          <h2 id="progress-search-heading">Find a participant</h2>

          <form className="admin-search-form" onSubmit={handleSearch}>
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
                onChange={(event) => setEmail(event.target.value)}
                required
              />
            </div>

            <div className="admin-form-actions">
              <button className="admin-button" type="submit">
                Search
              </button>

              <button
                className="admin-secondary-button"
                type="button"
                onClick={clearSearch}
              >
                Clear
              </button>
            </div>
          </form>

          {statusMessage && (
            <p className="admin-form-status" role="status">
              {statusMessage}
            </p>
          )}
        </section>

        <section
          className="admin-panel admin-user-result"
          aria-labelledby="progress-result-heading"
        >
          <h2 id="progress-result-heading">Skill-tree progress</h2>

          <div className="admin-empty-result">
            <span className="admin-empty-icon" aria-hidden="true">
              ⚔
            </span>

            <h3>No participant selected</h3>

            <p>
              The participant’s skill tree and recorded progress will
              appear here after a successful search.
            </p>
          </div>
        </section>
      </main>
    </div>
  );
}

export default AdminProgressPage;