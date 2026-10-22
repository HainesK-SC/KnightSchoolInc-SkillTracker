import {
  useEffect,
  useState,
  type FormEvent,
} from "react";
import { Link, useNavigate } from "react-router";

import { ApiError } from "../api/apiClient";
import {
  createAdminSkill,
  getAdminDisciplines,
  type Discipline,
} from "../api/adminSkillsApi";
import mainLogo from "../assets/branding/main-logo.svg";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";

function AdminCreateSkillPage() {
  const navigate = useNavigate();

  const [disciplines, setDisciplines] = useState<Discipline[]>(
    [],
  );
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    async function loadDisciplines() {
      try {
        const response = await getAdminDisciplines();
        setDisciplines(response);
      } catch (error) {
        if (error instanceof ApiError) {
          setErrorMessage(error.message);
        } else {
          setErrorMessage(
            "The disciplines could not be loaded.",
          );
        }
      } finally {
        setIsLoading(false);
      }
    }

    void loadDisciplines();
  }, []);

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    const form = new FormData(event.currentTarget);

    const name = String(form.get("name") ?? "").trim();
    const description = String(
      form.get("description") ?? "",
    ).trim();
    const benefits = String(
      form.get("benefits") ?? "",
    ).trim();
    const disciplineId = Number(form.get("disciplineId"));

    setErrorMessage("");

    if (!name || !description || !disciplineId) {
      setErrorMessage("Complete all required fields.");
      return;
    }

    setIsSubmitting(true);

    try {
      await createAdminSkill({
        name,
        description,
        benefits: benefits || null,
        disciplineId,
      });

      navigate("/admin/skills", { replace: true });
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "The skill could not be created. Please try again.",
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
        <h1 className="admin-title">Add New Skill</h1>

        <p className="admin-description">
          Create a skill and assign it to a Knight School
          discipline.
        </p>

        <section
          className="admin-panel admin-form-panel"
          aria-labelledby="skill-form-heading"
        >
          <h2 id="skill-form-heading">Skill information</h2>

          {errorMessage && (
            <p className="admin-form-error" role="alert">
              {errorMessage}
            </p>
          )}

          {isLoading ? (
            <p role="status">Loading disciplines…</p>
          ) : (
            <form
              className="admin-form"
              onSubmit={handleSubmit}
            >
              <div className="admin-form-group">
                <label htmlFor="skillName">Skill name</label>

                <input
                  id="skillName"
                  name="name"
                  type="text"
                  maxLength={50}
                  required
                />
              </div>

              <div className="admin-form-group">
                <label htmlFor="skillDescription">
                  Description
                </label>

                <textarea
                  id="skillDescription"
                  name="description"
                  rows={4}
                  maxLength={200}
                  required
                />
              </div>

              <div className="admin-form-group">
                <label htmlFor="skillBenefits">
                  Benefits
                </label>

                <textarea
                  id="skillBenefits"
                  name="benefits"
                  rows={3}
                  maxLength={150}
                />

                <p className="admin-field-help">
                  Optional. Explain what the participant gains by
                  completing this skill.
                </p>
              </div>

              <div className="admin-form-group">
                <label htmlFor="skillDiscipline">
                  Discipline
                </label>

                <select
                  id="skillDiscipline"
                  name="disciplineId"
                  defaultValue=""
                  required
                >
                  <option value="" disabled>
                    Select a discipline
                  </option>

                  {disciplines.map((discipline) => (
                    <option
                      key={discipline.disciplineId}
                      value={discipline.disciplineId}
                    >
                      {discipline.name}
                    </option>
                  ))}
                </select>
              </div>

              {disciplines.length === 0 && (
                <p className="admin-form-error" role="alert">
                  A discipline must be created before a skill can
                  be added.
                </p>
              )}

              <div className="admin-form-actions">
                <button
                  className="admin-button"
                  type="submit"
                  disabled={
                    isSubmitting || disciplines.length === 0
                  }
                >
                  {isSubmitting
                    ? "Creating skill…"
                    : "Create skill"}
                </button>

                <Link
                  className="admin-secondary-button"
                  to="/admin/skills"
                >
                  Cancel
                </Link>
              </div>
            </form>
          )}
        </section>
      </main>
    </div>
  );
}

export default AdminCreateSkillPage;