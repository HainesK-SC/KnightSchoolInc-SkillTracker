import {
  useEffect,
  useState,
  type FormEvent,
} from "react";
import { Link, useParams } from "react-router";

import { ApiError } from "../api/apiClient";
import {
  getAdminRoles,
  getAdminUser,
  getDisplayNameOptions,
  updateAdminUser,
  type AdminUser,
  type DisplayNameOption,
} from "../api/adminUsersApi";
import mainLogo from "../assets/branding/main-logo.svg";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";

const roleLabels: Record<string, string> = {
  REGULAR_USER: "Participant",
  INSTRUCTOR: "Instructor",
  ADMINISTRATOR: "Administrator",
};

function AdminEditUserPage() {
  const { userId } = useParams<{ userId: string }>();

  const [user, setUser] = useState<AdminUser | null>(null);
  const [availableRoles, setAvailableRoles] = useState<string[]>([]);
  const [salutationOptions, setSalutationOptions] = useState<
    DisplayNameOption[]
  >([]);
  const [modifierOptions, setModifierOptions] = useState<
    DisplayNameOption[]
  >([]);

  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [email, setEmail] = useState("");
  const [salutation, setSalutation] = useState("");
  const [modifier, setModifier] = useState("");
  const [selectedRoles, setSelectedRoles] = useState<string[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [statusMessage, setStatusMessage] = useState("");

  useEffect(() => {
    let isCurrent = true;

    async function loadUser() {
      if (!userId) {
        setErrorMessage("No user account was selected.");
        setIsLoading(false);
        return;
      }

      try {
        const [loadedUser, roles, displayNameOptions] =
          await Promise.all([
            getAdminUser(userId),
            getAdminRoles(),
            getDisplayNameOptions(),
          ]);

        if (!isCurrent) {
          return;
        }

        setUser(loadedUser);
        setAvailableRoles(roles);
        setSalutationOptions(displayNameOptions.salutations);
        setModifierOptions(displayNameOptions.modifiers);

        setFirstName(loadedUser.firstName);
        setLastName(loadedUser.lastName);
        setEmail(loadedUser.email ?? "");
        setSalutation(loadedUser.salutation);
        setModifier(loadedUser.modifier);
        setSelectedRoles(loadedUser.roles);
      } catch (error) {
        if (!isCurrent) {
          return;
        }

        if (error instanceof ApiError) {
          setErrorMessage(error.message);
        } else {
          setErrorMessage(
            "The selected user account could not be loaded.",
          );
        }
      } finally {
        if (isCurrent) {
          setIsLoading(false);
        }
      }
    }

    void loadUser();

    return () => {
      isCurrent = false;
    };
  }, [userId]);

  function handleRoleChange(role: string, checked: boolean) {
    setSelectedRoles((currentRoles) => {
      if (checked) {
        return currentRoles.includes(role)
          ? currentRoles
          : [...currentRoles, role];
      }

      return currentRoles.filter(
        (currentRole) => currentRole !== role,
      );
    });
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setErrorMessage("");
    setStatusMessage("");

    if (!userId) {
      setErrorMessage("No user account was selected.");
      return;
    }

    if (!firstName.trim() || !lastName.trim()) {
      setErrorMessage("Enter the user’s first and last name.");
      return;
    }

    if (!salutation || !modifier) {
      setErrorMessage(
        "Select a salutation and display-name modifier.",
      );
      return;
    }

    if (selectedRoles.length === 0) {
      setErrorMessage("Select at least one account role.");
      return;
    }

    setIsSubmitting(true);

    try {
      const updatedUser = await updateAdminUser(userId, {
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        email: email.trim() || null,
        salutation,
        modifier,
        roles: selectedRoles,
      });

      setUser(updatedUser);
      setFirstName(updatedUser.firstName);
      setLastName(updatedUser.lastName);
      setEmail(updatedUser.email ?? "");
      setSalutation(updatedUser.salutation);
      setModifier(updatedUser.modifier);
      setSelectedRoles(updatedUser.roles);

      setStatusMessage(
        `${updatedUser.displayName} was updated successfully.`,
      );
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "The user account could not be updated. Please try again.",
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
            <Link to="/admin/users">Manage users</Link>
            <Link to="/profile">My profile</Link>
          </nav>
        </div>
      </header>

      <main className="admin-main">
        <Link className="admin-back-link" to="/admin/users">
          ← Back to user management
        </Link>

        <p className="admin-eyebrow">User management</p>
        <h1 className="admin-title">Edit User Account</h1>

        <p className="admin-description">
          Update account information and assigned application roles.
        </p>

        {isLoading && (
          <p className="admin-form-status" role="status">
            Loading user account…
          </p>
        )}

        {errorMessage && (
          <p className="admin-form-error" role="alert">
            {errorMessage}
          </p>
        )}

        {!isLoading && user && (
          <section
            className="admin-panel admin-form-panel"
            aria-labelledby="edit-user-heading"
          >
            <h2 id="edit-user-heading">{user.displayName}</h2>

            <form className="admin-form" onSubmit={handleSubmit}>
              <div className="admin-form-grid">
                <div className="admin-form-group">
                  <label htmlFor="editFirstName">First name</label>

                  <input
                    id="editFirstName"
                    type="text"
                    value={firstName}
                    onChange={(event) =>
                      setFirstName(event.target.value)
                    }
                    maxLength={50}
                    required
                    disabled={isSubmitting}
                  />
                </div>

                <div className="admin-form-group">
                  <label htmlFor="editLastName">Last name</label>

                  <input
                    id="editLastName"
                    type="text"
                    value={lastName}
                    onChange={(event) =>
                      setLastName(event.target.value)
                    }
                    maxLength={50}
                    required
                    disabled={isSubmitting}
                  />
                </div>
              </div>

              <div className="admin-form-group">
                <label htmlFor="editEmail">Email address</label>

                <input
                  id="editEmail"
                  type="email"
                  value={email}
                  onChange={(event) => setEmail(event.target.value)}
                  maxLength={256}
                  disabled={isSubmitting}
                />
              </div>

              <div className="admin-form-grid">
                <div className="admin-form-group">
                  <label htmlFor="editSalutation">Salutation</label>

                  <select
                    id="editSalutation"
                    value={salutation}
                    onChange={(event) =>
                      setSalutation(event.target.value)
                    }
                    required
                    disabled={isSubmitting}
                  >
                    <option value="">Select a salutation</option>

                    {salutationOptions.map((option) => (
                      <option
                        value={option.value}
                        key={option.value}
                      >
                        {option.text}
                      </option>
                    ))}
                  </select>
                </div>

                <div className="admin-form-group">
                  <label htmlFor="editModifier">
                    Display-name modifier
                  </label>

                  <select
                    id="editModifier"
                    value={modifier}
                    onChange={(event) =>
                      setModifier(event.target.value)
                    }
                    required
                    disabled={isSubmitting}
                  >
                    <option value="">Select a modifier</option>

                    {modifierOptions.map((option) => (
                      <option
                        value={option.value}
                        key={option.value}
                      >
                        {option.text}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              <fieldset
                className="admin-role-group"
                disabled={isSubmitting}
              >
                <legend>Account roles</legend>

                <p className="admin-field-help">
                  Select one or more roles for this account.
                </p>

                {availableRoles.map((role) => (
                  <label className="admin-role-option" key={role}>
                    <input
                      type="checkbox"
                      checked={selectedRoles.includes(role)}
                      onChange={(event) =>
                        handleRoleChange(
                          role,
                          event.target.checked,
                        )
                      }
                    />

                    <span>
                      <strong>
                        {roleLabels[role] ??
                          role.replaceAll("_", " ")}
                      </strong>
                    </span>
                  </label>
                ))}
              </fieldset>

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
                    salutationOptions.length === 0 ||
                    modifierOptions.length === 0
                  }
                >
                  {isSubmitting ? "Updating user…" : "Update user"}
                </button>

                <Link
                  className="admin-secondary-button"
                  to="/admin/users"
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

export default AdminEditUserPage;