import { useState, type FormEvent } from "react";
import { Link } from "react-router";

import mainLogo from "../assets/branding/main-logo.svg";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";

function AdminCreateUserPage() {
  const [errorMessage, setErrorMessage] = useState("");
  const [statusMessage, setStatusMessage] = useState("");

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const form = new FormData(event.currentTarget);
    const password = String(form.get("password") ?? "");
    const roles = form.getAll("roles");

    setErrorMessage("");
    setStatusMessage("");

    if (roles.length === 0) {
      setErrorMessage("Select at least one account role.");
      return;
    }

    const validPassword =
      password.length >= 8 &&
      /[a-z]/.test(password) &&
      /[A-Z]/.test(password) &&
      /\d/.test(password) &&
      /[^A-Za-z0-9]/.test(password);

    if (!validPassword) {
      setErrorMessage(
        "The temporary password must contain at least 8 characters, including uppercase, lowercase, number and symbol.",
      );
      return;
    }

    setStatusMessage(
      "The form is valid. No account was created because the administrator user-creation endpoint is not connected yet.",
    );
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
            <Link to="/profile">My profile</Link>
          </nav>
        </div>
      </header>

      <main className="admin-main">
        <Link className="admin-back-link" to="/admin">
          ← Back to dashboard
        </Link>

        <p className="admin-eyebrow">User management</p>
        <h1 className="admin-title">Create New User</h1>

        <p className="admin-description">
          Enter the user’s account information and assign the appropriate
          application roles.
        </p>

        <section
          className="admin-panel admin-form-panel"
          aria-labelledby="create-user-heading"
        >
          <h2 id="create-user-heading">Account information</h2>

          <form className="admin-form" onSubmit={handleSubmit}>
            <div className="admin-form-grid">
              <div className="admin-form-group">
                <label htmlFor="adminFirstName">First name</label>
                <input
                  id="adminFirstName"
                  name="firstName"
                  type="text"
                  autoComplete="given-name"
                  maxLength={50}
                  required
                />
              </div>

              <div className="admin-form-group">
                <label htmlFor="adminLastName">Last name</label>
                <input
                  id="adminLastName"
                  name="lastName"
                  type="text"
                  autoComplete="family-name"
                  maxLength={50}
                  required
                />
              </div>
            </div>

            <div className="admin-form-group">
              <label htmlFor="adminEmail">Email address</label>
              <input
                id="adminEmail"
                name="email"
                type="email"
                autoComplete="email"
                maxLength={256}
                required
              />
            </div>

            <div className="admin-form-group">
              <label htmlFor="temporaryPassword">
                Temporary password
              </label>

              <input
                id="temporaryPassword"
                name="password"
                type="password"
                autoComplete="new-password"
                minLength={8}
                aria-describedby="password-help"
                required
              />

              <p className="admin-field-help" id="password-help">
                Use at least 8 characters with uppercase, lowercase,
                number and symbol.
              </p>
            </div>

            <fieldset className="admin-role-group">
              <legend>Account roles</legend>

              <p className="admin-field-help">
                Select one or more roles for this account.
              </p>

              <label className="admin-role-option">
                <input
                  name="roles"
                  type="checkbox"
                  value="REGULAR_USER"
                />
                <span>
                  <strong>Participant</strong>
                  <small>Can access their profile and skill tree.</small>
                </span>
              </label>

              <label className="admin-role-option">
                <input
                  name="roles"
                  type="checkbox"
                  value="INSTRUCTOR"
                />
                <span>
                  <strong>Instructor</strong>
                  <small>
                    Can review and approve participant progress.
                  </small>
                </span>
              </label>

              <label className="admin-role-option">
                <input
                  name="roles"
                  type="checkbox"
                  value="ADMINISTRATOR"
                />
                <span>
                  <strong>Administrator</strong>
                  <small>
                    Can access administrative management features.
                  </small>
                </span>
              </label>
            </fieldset>

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

            <div className="admin-form-actions">
              <button className="admin-button" type="submit">
                Validate account form
              </button>

              <Link className="admin-secondary-button" to="/admin">
                Cancel
              </Link>
            </div>
          </form>
        </section>
      </main>
    </div>
  );
}

export default AdminCreateUserPage;