import { useState, type FormEvent } from "react";
import { Link } from "react-router";

import { ApiError } from "../api/apiClient";
import {
  deleteAdminUser,
  getAdminUsers,
  type AdminUser,
} from "../api/adminUsersApi";
import mainLogo from "../assets/branding/main-logo.svg";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";

function AdminUsersPage() {
  const [email, setEmail] = useState("");
  const [selectedUser, setSelectedUser] =
    useState<AdminUser | null>(null);
  const [statusMessage, setStatusMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");
  const [isSearching, setIsSearching] = useState(false);
  const [isDeleting, setIsDeleting] = useState(false);
  const [isConfirmingDelete, setIsConfirmingDelete] =
    useState(false);
  const [hasSearched, setHasSearched] = useState(false);

  async function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const normalizedEmail = email.trim().toLowerCase();

    setSelectedUser(null);
    setStatusMessage("");
    setErrorMessage("");
    setIsConfirmingDelete(false);
    setHasSearched(true);
    setIsSearching(true);

    try {
      const users = await getAdminUsers();

      const matchingUser = users.find(
        (user) =>
          user.email?.trim().toLowerCase() === normalizedEmail,
      );

      if (!matchingUser) {
        setStatusMessage(
          "No user account was found with that email address.",
        );
        return;
      }

      setSelectedUser(matchingUser);
      setStatusMessage("User account found.");
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "The user search could not be completed. Please try again.",
        );
      }
    } finally {
      setIsSearching(false);
    }
  }

  async function handleDeleteUser() {
    if (!selectedUser) {
      return;
    }

    const deletedDisplayName = selectedUser.displayName;

    setErrorMessage("");
    setStatusMessage("");
    setIsDeleting(true);

    try {
      await deleteAdminUser(selectedUser.id);

      setSelectedUser(null);
      setEmail("");
      setHasSearched(false);
      setIsConfirmingDelete(false);
      setStatusMessage(
        `${deletedDisplayName} was deleted successfully.`,
      );
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "The user account could not be deleted. Please try again.",
        );
      }
    } finally {
      setIsDeleting(false);
    }
  }

  function clearSearch() {
    setEmail("");
    setSelectedUser(null);
    setStatusMessage("");
    setErrorMessage("");
    setIsConfirmingDelete(false);
    setHasSearched(false);
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
            <Link to="/admin/users/new">Create user</Link>
            <Link to="/profile">My profile</Link>
          </nav>
        </div>
      </header>

      <main className="admin-main">
        <Link className="admin-back-link" to="/admin">
          ← Back to dashboard
        </Link>

        <p className="admin-eyebrow">User management</p>
        <h1 className="admin-title">Manage Existing Users</h1>

        <p className="admin-description">
          Search for a user by email address to view, edit or delete their
          account.
        </p>

        <section
          className="admin-panel admin-search-panel"
          aria-labelledby="search-user-heading"
        >
          <h2 id="search-user-heading">Search for a user</h2>

          <form className="admin-search-form" onSubmit={handleSearch}>
            <div className="admin-form-group">
              <label htmlFor="userSearchEmail">Email address</label>

              <input
                id="userSearchEmail"
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
              <button
                className="admin-button"
                type="submit"
                disabled={isSearching || isDeleting}
              >
                {isSearching ? "Searching…" : "Search"}
              </button>

              <button
                className="admin-secondary-button"
                type="button"
                onClick={clearSearch}
                disabled={isDeleting}
              >
                Clear
              </button>
            </div>
          </form>

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
        </section>

        <section
          className="admin-panel admin-user-result"
          aria-labelledby="user-result-heading"
        >
          <h2 id="user-result-heading">User account</h2>

          {selectedUser ? (
            <article className="admin-user-card">
              <header className="admin-user-card-header">
                <div
                  className="admin-user-avatar"
                  aria-hidden="true"
                >
                  {selectedUser.firstName.charAt(0)}
                  {selectedUser.lastName.charAt(0)}
                </div>

                <div className="admin-user-identity">
                  <p className="admin-user-label">
                    Knight School account
                  </p>

                  <h3>{selectedUser.displayName}</h3>

                  <p>
                    {selectedUser.email ?? "No email provided"}
                  </p>
                </div>

                <span className="admin-account-status">
                  {String(selectedUser.status)}
                </span>
              </header>

              <div className="admin-user-information">
                <div className="admin-user-information-item">
                  <span className="admin-user-information-label">
                    First name
                  </span>

                  <strong>{selectedUser.firstName}</strong>
                </div>

                <div className="admin-user-information-item">
                  <span className="admin-user-information-label">
                    Last name
                  </span>

                  <strong>{selectedUser.lastName}</strong>
                </div>

                <div className="admin-user-information-item admin-user-role-item">
                  <span className="admin-user-information-label">
                    Assigned roles
                  </span>

                  <div className="admin-role-badges">
                    {selectedUser.roles.length > 0 ? (
                      selectedUser.roles.map((role) => (
                        <span
                          className="admin-role-badge"
                          key={role}
                        >
                          {role.replaceAll("_", " ")}
                        </span>
                      ))
                    ) : (
                      <span>No roles assigned</span>
                    )}
                  </div>
                </div>
              </div>

              <footer className="admin-user-card-footer">
                {!isConfirmingDelete ? (
                  <div className="admin-form-actions">
                    <Link
                      className="admin-button admin-action-link"
                      to={`/admin/users/${selectedUser.id}/edit`}
                    >
                      Edit user
                    </Link>

                    <button
                      className="admin-danger-button"
                      type="button"
                      onClick={() => setIsConfirmingDelete(true)}
                    >
                      Delete user
                    </button>
                  </div>
                ) : (
                  <div
                    className="admin-delete-confirmation"
                    role="alertdialog"
                    aria-labelledby="delete-confirmation-heading"
                    aria-describedby="delete-confirmation-description"
                  >
                    <div>
                      <h3 id="delete-confirmation-heading">
                        Delete this account?
                      </h3>

                      <p id="delete-confirmation-description">
                        This will permanently delete{" "}
                        <strong>{selectedUser.displayName}</strong>{" "}
                        and all associated account data. This action
                        cannot be undone.
                      </p>
                    </div>

                    <div className="admin-form-actions">
                      <button
                        className="admin-danger-confirm-button"
                        type="button"
                        onClick={() => void handleDeleteUser()}
                        disabled={isDeleting}
                      >
                        {isDeleting
                          ? "Deleting account…"
                          : "Confirm deletion"}
                      </button>

                      <button
                        className="admin-secondary-button"
                        type="button"
                        onClick={() =>
                          setIsConfirmingDelete(false)
                        }
                        disabled={isDeleting}
                      >
                        Cancel
                      </button>
                    </div>
                  </div>
                )}
              </footer>
            </article>
          ) : (
            <div className="admin-empty-result">
              <span className="admin-empty-icon" aria-hidden="true">
                ?
              </span>

              <h3>
                {hasSearched
                  ? "No matching user"
                  : "No user selected"}
              </h3>

              <p>
                {hasSearched
                  ? "Check the email address and try searching again."
                  : "A user’s account information and account actions will appear here after a successful search."}
              </p>
            </div>
          )}
        </section>
      </main>
    </div>
  );
}

export default AdminUsersPage;