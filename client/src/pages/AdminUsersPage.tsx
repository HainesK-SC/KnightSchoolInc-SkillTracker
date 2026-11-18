import {
  useCallback,
  useEffect,
  useMemo,
  useState,
} from "react";
import { Link, useNavigate } from "react-router";

import { ApiError } from "../api/apiClient";
import {
  deleteAdminUser,
  getAdminUsers,
  type AdminUser,
} from "../api/adminUsersApi";
import mainLogo from "../assets/branding/main-logo.svg";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";

function formatRole(role: string) {
  if (role === "REGULAR_USER") {
    return "Participant";
  }

  return role
    .toLowerCase()
    .split("_")
    .map(
      (word) =>
        word.charAt(0).toUpperCase() + word.slice(1),
    )
    .join(" ");
}

function formatAccountStatus(status: string | number) {
  if (typeof status === "number") {
    return `Status ${status}`;
  }

  return status
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .replaceAll("_", " ");
}

function getInitials(user: AdminUser) {
  return `${user.firstName.charAt(0)}${user.lastName.charAt(
    0,
  )}`.toUpperCase();
}

function AdminUsersPage() {
  const navigate = useNavigate();

  const [users, setUsers] = useState<AdminUser[]>([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [roleFilter, setRoleFilter] = useState("ALL");
  const [deleteTarget, setDeleteTarget] =
    useState<AdminUser | null>(null);

  const [isLoading, setIsLoading] = useState(true);
  const [isDeleting, setIsDeleting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [statusMessage, setStatusMessage] = useState("");

  const loadUsers = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const response = await getAdminUsers();

      const sortedUsers = [...response].sort((first, second) =>
        first.displayName.localeCompare(second.displayName),
      );

      setUsers(sortedUsers);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        navigate("/login", { replace: true });
        return;
      }

      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "The user directory could not be loaded. Please try again.",
        );
      }
    } finally {
      setIsLoading(false);
    }
  }, [navigate]);

  useEffect(() => {
    void loadUsers();
  }, [loadUsers]);

  const filteredUsers = useMemo(() => {
    const normalizedSearch = searchTerm
      .trim()
      .toLowerCase();

    return users.filter((user) => {
      const matchesSearch =
        normalizedSearch.length === 0 ||
        user.displayName
          .toLowerCase()
          .includes(normalizedSearch) ||
        user.firstName
          .toLowerCase()
          .includes(normalizedSearch) ||
        user.lastName
          .toLowerCase()
          .includes(normalizedSearch) ||
        user.email
          ?.toLowerCase()
          .includes(normalizedSearch);

      const matchesRole =
        roleFilter === "ALL" ||
        user.roles.includes(roleFilter);

      return matchesSearch && matchesRole;
    });
  }, [roleFilter, searchTerm, users]);

  function clearFilters() {
    setSearchTerm("");
    setRoleFilter("ALL");
    setErrorMessage("");
    setStatusMessage("");
  }

  function beginDelete(user: AdminUser) {
    setDeleteTarget(user);
    setErrorMessage("");
    setStatusMessage("");
  }

  async function handleDeleteUser() {
    if (!deleteTarget) {
      return;
    }

    const deletedUserId = deleteTarget.id;
    const deletedDisplayName = deleteTarget.displayName;

    setIsDeleting(true);
    setErrorMessage("");
    setStatusMessage("");

    try {
      await deleteAdminUser(deletedUserId);

      setUsers((currentUsers) =>
        currentUsers.filter(
          (user) => user.id !== deletedUserId,
        ),
      );

      setDeleteTarget(null);

      setStatusMessage(
        `${deletedDisplayName} was deleted successfully.`,
      );
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        navigate("/login", { replace: true });
        return;
      }

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

  return (
    <div className="admin-page">
      <header className="admin-header">
        <div className="admin-header-content">
          <Link
            to="/admin"
            aria-label="Knight School admin dashboard"
          >
            <img
              className="admin-logo"
              src={mainLogo}
              alt="Knight School Inc."
            />
          </Link>

          <nav
            className="admin-navigation"
            aria-label="Admin navigation"
          >
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

        <div className="admin-directory-title-row">
          <div>
            <p className="admin-eyebrow">User management</p>

            <h1 className="admin-title">
              Manage Existing Users
            </h1>

            <p className="admin-description">
              View, search, edit, or delete Knight School
              accounts.
            </p>
          </div>

          <Link
            className="admin-button admin-action-link"
            to="/admin/users/new"
          >
            Create user
          </Link>
        </div>

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

        <section
          className="admin-panel admin-directory-panel"
          aria-labelledby="user-directory-heading"
        >
          <div className="admin-directory-heading">
            <div>
              <p className="admin-card-category">
                Account directory
              </p>

              <h2 id="user-directory-heading">All users</h2>
            </div>

            <span className="admin-directory-count">
              {filteredUsers.length} of {users.length} shown
            </span>
          </div>

          <div className="admin-directory-filters">
            <div className="admin-form-group">
              <label htmlFor="userDirectorySearch">
                Search accounts
              </label>

              <input
                id="userDirectorySearch"
                name="search"
                type="search"
                autoComplete="off"
                placeholder="Search by name or email"
                value={searchTerm}
                onChange={(event) =>
                  setSearchTerm(event.target.value)
                }
              />
            </div>

            <div className="admin-form-group">
              <label htmlFor="userRoleFilter">
                Account role
              </label>

              <select
                id="userRoleFilter"
                value={roleFilter}
                onChange={(event) =>
                  setRoleFilter(event.target.value)
                }
              >
                <option value="ALL">All roles</option>
                <option value="REGULAR_USER">
                  Participants
                </option>
                <option value="INSTRUCTOR">
                  Instructors
                </option>
                <option value="ADMINISTRATOR">
                  Administrators
                </option>
              </select>
            </div>

            <button
              className="admin-secondary-button admin-clear-filters"
              type="button"
              onClick={clearFilters}
              disabled={
                !searchTerm && roleFilter === "ALL"
              }
            >
              Clear filters
            </button>
          </div>

          {isLoading && (
            <div
              className="admin-directory-state"
              role="status"
            >
              <span
                className="admin-empty-icon"
                aria-hidden="true"
              >
                KS
              </span>

              <h3>Loading user accounts…</h3>

              <p>
                Please wait while the account directory is
                retrieved.
              </p>
            </div>
          )}

          {!isLoading && users.length === 0 && (
            <div className="admin-directory-state">
              <span
                className="admin-empty-icon"
                aria-hidden="true"
              >
                0
              </span>

              <h3>No user accounts available</h3>

              <p>
                New accounts will appear here after they are
                created.
              </p>

              <Link
                className="admin-button admin-action-link"
                to="/admin/users/new"
              >
                Create the first user
              </Link>
            </div>
          )}

          {!isLoading &&
            users.length > 0 &&
            filteredUsers.length === 0 && (
              <div className="admin-directory-state">
                <span
                  className="admin-empty-icon"
                  aria-hidden="true"
                >
                  ?
                </span>

                <h3>No matching accounts</h3>

                <p>
                  Try changing the search text or selected role.
                </p>

                <button
                  className="admin-secondary-button"
                  type="button"
                  onClick={clearFilters}
                >
                  Clear filters
                </button>
              </div>
            )}

          {!isLoading && filteredUsers.length > 0 && (
            <div className="admin-users-directory-grid">
              {filteredUsers.map((user) => (
                <article
                  className="admin-directory-user-card"
                  key={user.id}
                >
                  <header className="admin-directory-user-header">
                    <div
                      className="admin-user-avatar"
                      aria-hidden="true"
                    >
                      {getInitials(user)}
                    </div>

                    <div className="admin-user-identity">
                      <p className="admin-user-label">
                        Knight School account
                      </p>

                      <h3>{user.displayName}</h3>

                      <p>
                        {user.email ?? "No email provided"}
                      </p>
                    </div>

                    <span className="admin-account-status">
                      {formatAccountStatus(user.status)}
                    </span>
                  </header>

                  <div className="admin-directory-user-details">
                    <div>
                      <span>Legal name</span>

                      <strong>
                        {user.firstName} {user.lastName}
                      </strong>
                    </div>

                    <div>
                      <span>Assigned roles</span>

                      <div className="admin-role-badges">
                        {user.roles.length > 0 ? (
                          user.roles.map((role) => (
                            <span
                              className="admin-role-badge"
                              key={role}
                            >
                              {formatRole(role)}
                            </span>
                          ))
                        ) : (
                          <span className="admin-no-role">
                            No roles assigned
                          </span>
                        )}
                      </div>
                    </div>
                  </div>

                  <footer className="admin-directory-user-actions">
                    <Link
                      className="admin-button admin-action-link"
                      to={`/admin/users/${user.id}/edit`}
                    >
                      View / edit
                    </Link>

                    <button
                      className="admin-danger-button"
                      type="button"
                      onClick={() => beginDelete(user)}
                      disabled={isDeleting}
                    >
                      Delete
                    </button>
                  </footer>
                </article>
              ))}
            </div>
          )}
        </section>

        {deleteTarget && (
          <section
            className="admin-delete-confirmation admin-directory-delete"
            role="alertdialog"
            aria-modal="true"
            aria-labelledby="delete-confirmation-heading"
            aria-describedby="delete-confirmation-description"
          >
            <div>
              <p className="admin-card-category">
                Permanent action
              </p>

              <h2 id="delete-confirmation-heading">
                Delete this account?
              </h2>

              <p id="delete-confirmation-description">
                This will permanently delete{" "}
                <strong>{deleteTarget.displayName}</strong> and
                all associated account data. This action cannot
                be undone.
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
                onClick={() => setDeleteTarget(null)}
                disabled={isDeleting}
              >
                Cancel
              </button>
            </div>
          </section>
        )}
      </main>
    </div>
  );
}

export default AdminUsersPage;