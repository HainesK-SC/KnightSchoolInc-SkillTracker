import {
  useEffect,
  useState,
  type FormEvent,
} from "react";
import {
  Link,
  useNavigate,
  useParams,
} from "react-router";

import { ApiError } from "../api/apiClient";
import {
  getAdminDisciplines,
  getAdminSkill,
  updateAdminSkill,
  type Discipline,
  type Skill,
} from "../api/adminSkillsApi";
import mainLogo from "../assets/branding/main-logo.svg";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";

function AdminEditSkillPage() {
  const navigate = useNavigate();
  const { skillId } = useParams();

  const numericSkillId = Number(skillId);

  const [skill, setSkill] = useState<Skill | null>(null);
  const [disciplines, setDisciplines] = useState<Discipline[]>(
    [],
  );
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  useEffect(() => {
    async function loadPage() {
      if (!Number.isInteger(numericSkillId)) {
        setErrorMessage("The skill address is invalid.");
        setIsLoading(false);
        return;
      }

      try {
        const [skillResponse, disciplineResponse] =
          await Promise.all([
            getAdminSkill(numericSkillId),
            getAdminDisciplines(),
          ]);

        setSkill(skillResponse);
        setDisciplines(disciplineResponse);
      } catch (error) {
        if (error instanceof ApiError) {
          setErrorMessage(error.message);
        } else {
          setErrorMessage(
            "The skill information could not be loaded.",
          );
        }
      } finally {
        setIsLoading(false);
      }
    }

    void loadPage();
  }, [numericSkillId]);

  async function handleSubmit(
    event: FormEvent<HTMLFormElement>,
  ) {
    event.preventDefault();

    if (!skill) {
      return;
    }

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
      await updateAdminSkill(skill.skillId, {
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
          "The skill could not be updated.",
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
        <h1 className="admin-title">Edit Skill</h1>

        {errorMessage && (
          <p className="admin-form-error" role="alert">
            {errorMessage}
          </p>
        )}

        {isLoading && (
          <section className="admin-panel" role="status">
            <h2>Loading skill…</h2>
          </section>
        )}

        {!isLoading && skill && (
          <section
            className="admin-panel admin-form-panel"
            aria-labelledby="edit-skill-heading"
          >
            <h2 id="edit-skill-heading">
              {skill.name}
            </h2>

            <form
              className="admin-form"
              onSubmit={handleSubmit}
            >
              <div className="admin-form-group">
                <label htmlFor="editSkillName">
                  Skill name
                </label>

                <input
                  id="editSkillName"
                  name="name"
                  type="text"
                  maxLength={50}
                  defaultValue={skill.name}
                  required
                />
              </div>

              <div className="admin-form-group">
                <label htmlFor="editSkillDescription">
                  Description
                </label>

                <textarea
                  id="editSkillDescription"
                  name="description"
                  rows={4}
                  maxLength={200}
                  defaultValue={skill.description}
                  required
                />
              </div>

              <div className="admin-form-group">
                <label htmlFor="editSkillBenefits">
                  Benefits
                </label>

                <textarea
                  id="editSkillBenefits"
                  name="benefits"
                  rows={3}
                  maxLength={150}
                  defaultValue={skill.benefits ?? ""}
                />
              </div>

              <div className="admin-form-group">
                <label htmlFor="editSkillDiscipline">
                  Discipline
                </label>

                <select
                  id="editSkillDiscipline"
                  name="disciplineId"
                  defaultValue={skill.disciplineId}
                  required
                >
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

              <div className="admin-form-actions">
                <button
                  className="admin-button"
                  type="submit"
                  disabled={isSubmitting}
                >
                  {isSubmitting
                    ? "Saving changes…"
                    : "Save changes"}
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
        )}
      </main>
    </div>
  );
}

export default AdminEditSkillPage;