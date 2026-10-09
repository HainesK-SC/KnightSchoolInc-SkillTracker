import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router";

import { ApiError } from "../api/apiClient";
import {
  createAdminUser,
  getAdminRoles,
} from "../api/adminUsersApi";
import mainLogo from "../assets/branding/main-logo.svg";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";

const roleInformation: Record<
  string,
  { label: string; description: string }
> = {
  REGULAR_USER: {
    label: "Participant",
    description: "Can access their profile and skill tree.",
  },
  INSTRUCTOR: {
    label: "Instructor",
    description: "Can review and approve participant progress.",
  },
  ADMINISTRATOR: {
    label: "Administrator",
    description: "Can access administrative management features.",
  },
};

function AdminCreateUserPage() {
  const [errorMessage, setErrorMessage] = useState("");
  const [statusMessage, setStatusMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [availableRoles, setAvailableRoles] = useState<string[]>([]);
  const [isLoadingRoles, setIsLoadingRoles] = useState(true);

  useEffect(() => {
    let isCurrent = true;

    async function loadRoles() {
      try {
        const roles = await getAdminRoles();

        if (isCurrent) {
          setAvailableRoles(roles);
        }
      } catch (error) {
        if (!isCurrent) {
          return;
        }

        if (error instanceof ApiError) {
          setErrorMessage(error.message);
        } else {
          setErrorMessage(
            "The available account roles could not be loaded.",
          );
        }
      } finally {
        if (isCurrent) {
          setIsLoadingRoles(false);
        }
      }
    }

    void loadRoles();

    return () => {
      isCurrent = false;
    };
  }, []);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const formElement = event.currentTarget;
    const form = new FormData(formElement);

    const firstName = String(form.get("firstName") ?? "").trim();
    const lastName = String(form.get("lastName") ?? "").trim();
    const email = String(form.get("email") ?? "").trim();
    const roles = form.getAll("roles").map(String);

    setErrorMessage("");
    setStatusMessage("");

    if (!firstName || !lastName) {
      setErrorMessage("Enter the user’s first and last name.");
      return;
    }

    if (roles.length === 0) {
      setErrorMessage("Select at least one account role.");
      return;
    }

    setIsSubmitting(true);

    try {
      const createdUser = await createAdminUser({
        firstName,
        lastName,
        email: email || null,
        roles,
      });

      setStatusMessage(
        `${createdUser.displayName} was created successfully.`,
      );

      formElement.reset();
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "The user could not be created. Please try again.",
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

            <fieldset
              className="admin-role-group"
              disabled={isLoadingRoles}
            >
              <legend>Account roles</legend>

              <p className="admin-field-help">
                Select one or more roles for this account.
              </p>

              {isLoadingRoles && (
                <p className="admin-form-status" role="status">
                  Loading account roles…
                </p>
              )}

              {!isLoadingRoles &&
                availableRoles.map((role) => {
                  const information = roleInformation[role];

                  return (
                    <label
                      className="admin-role-option"
                      key={role}
                    >
                      <input
                        name="roles"
                        type="checkbox"
                        value={role}
                      />

                      <span>
                        <strong>
                          {information?.label ??
                            role.replaceAll("_", " ")}
                        </strong>

                        <small>
                          {information?.description ??
                            "Application account role."}
                        </small>
                      </span>
                    </label>
                  );
                })}

              {!isLoadingRoles && availableRoles.length === 0 && (
                <p className="admin-form-error" role="alert">
                  No account roles are currently available.
                </p>
              )}
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
              <button
                className="admin-button"
                type="submit"
                disabled={
                  isSubmitting ||
                  isLoadingRoles ||
                  availableRoles.length === 0
                }
              >
                {isSubmitting ? "Creating user…" : "Create user"}
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