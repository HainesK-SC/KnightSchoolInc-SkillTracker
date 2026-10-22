import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router";

import { ApiError } from "../api/apiClient";
import { createAdminDiscipline } from "../api/adminSkillsApi";
import mainLogo from "../assets/branding/main-logo.svg";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";

function AdminCreateDisciplinePage() {
  const navigate = useNavigate();

  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    const form = new FormData(event.currentTarget);
    const name = String(form.get("name") ?? "").trim();

    setErrorMessage("");

    if (!name) {
      setErrorMessage("Enter a discipline name.");
      return;
    }

    setIsSubmitting(true);

    try {
      await createAdminDiscipline({ name });
      navigate("/admin/skills", { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "The discipline could not be created.",
        );
      }
    } finally {
      setIsSubmitting(false);
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
            <Link to="/admin/skills">Skills</Link>
            <Link to="/profile">My profile</Link>
          </nav>
        </div>
      </header>

      <main className="admin-main">
        <Link
          className="admin-back-link"
          to="/admin/skills"
        >
          ← Back to skills
        </Link>

        <p className="admin-eyebrow">Training catalogue</p>
        <h1 className="admin-title">Add Discipline</h1>

        <p className="admin-description">
          Create a training discipline that skills can be assigned
          to.
        </p>

        <section
          className="admin-panel admin-form-panel"
          aria-labelledby="discipline-heading"
        >
          <h2 id="discipline-heading">
            Discipline information
          </h2>

          <form
            className="admin-form"
            onSubmit={handleSubmit}
          >
            <div className="admin-form-group">
              <label htmlFor="disciplineName">
                Discipline name
              </label>

              <input
                id="disciplineName"
                name="name"
                type="text"
                maxLength={50}
                placeholder="For example, Swordplay"
                autoFocus
                required
              />

              <p className="admin-field-help">
                The name must be unique and no more than 50
                characters.
              </p>
            </div>

            {errorMessage && (
              <p className="admin-form-error" role="alert">
                {errorMessage}
              </p>
            )}

            <div className="admin-form-actions">
              <button
                className="admin-button"
                type="submit"
                disabled={isSubmitting}
              >
                {isSubmitting
                  ? "Creating discipline…"
                  : "Create discipline"}
              </button>

              <Link
                className="admin-secondary-button"
                to="/admin/skills"
              >
                Cancel
              </Link>
            </div>
          </form>
        </section>
      </main>
    </div>
  );
}

export default AdminCreateDisciplinePage;