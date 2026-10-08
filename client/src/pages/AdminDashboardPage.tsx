import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router";

import { ApiError } from "../api/apiClient";
import { logoutUser } from "../api/authApi";
import { getAdminUsers } from "../api/adminUsersApi";
import mainLogo from "../assets/branding/main-logo.svg";
import "./AdminDashboardPage.css";

function AdminDashboardPage() {
  const navigate = useNavigate();

  const [isSigningOut, setIsSigningOut] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [summary, setSummary] = useState<{
  total: number;
  participants: number;
  instructors: number;
  administrators: number;
    } | null>(null);

  const [isLoadingSummary, setIsLoadingSummary] = useState(true);
  useEffect(() => {
  let isCurrent = true;

  async function loadDashboardSummary() {
    try {
      const users = await getAdminUsers();

      if (!isCurrent) {
        return;
      }

      setSummary({
        total: users.length,
        participants: users.filter((user) =>
          user.roles.includes("REGULAR_USER"),
        ).length,
        instructors: users.filter((user) =>
          user.roles.includes("INSTRUCTOR"),
        ).length,
        administrators: users.filter((user) =>
          user.roles.includes("ADMINISTRATOR"),
        ).length,
      });
    } catch (error) {
      if (!isCurrent) {
        return;
      }

      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "The dashboard summary could not be loaded.",
        );
      }
    } finally {
      if (isCurrent) {
        setIsLoadingSummary(false);
      }
    }
  }

  void loadDashboardSummary();

  return () => {
    isCurrent = false;
  };
}, []);
  async function handleSignOut() {
    setIsSigningOut(true);
    setErrorMessage("");

    try {
      await logoutUser();
      navigate("/login", { replace: true });
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        navigate("/login", { replace: true });
        return;
      }

      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "Something unexpected happened while signing out.",
        );
      }
    } finally {
      setIsSigningOut(false);
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
            <Link to="/admin" aria-current="page">
              Dashboard
            </Link>

            <Link to="/profile">My profile</Link>

            <button
              className="admin-sign-out"
              type="button"
              onClick={handleSignOut}
              disabled={isSigningOut}
            >
              {isSigningOut ? "Signing out..." : "Sign out"}
            </button>
          </nav>
        </div>
      </header>

      <main className="admin-main">
        <p className="admin-eyebrow">Administration portal</p>

        <h1 className="admin-title">Admin Dashboard</h1>

        <p className="admin-description">
          Manage Knight School accounts and review participant progression.
        </p>

        {errorMessage && (
          <p className="admin-error" role="alert">
            {errorMessage}
          </p>
        )}

        <section
          className="admin-feature-grid"
          aria-labelledby="management-heading"
        >
          <h2 className="admin-section-title" id="management-heading">
            Management tools
          </h2>

          <article className="admin-feature-card">
            <span className="admin-card-number" aria-hidden="true">
              01
            </span>

            <p className="admin-card-category">User management</p>
            <h3>Create a new user</h3>

            <p>
              Create a participant, instructor or administrator account.
            </p>

            <Link className="admin-card-link" to="/admin/users/new">
              Create user
            </Link>
          </article>

          <article className="admin-feature-card">
            <span className="admin-card-number" aria-hidden="true">
              02
            </span>

            <p className="admin-card-category">User management</p>
            <h3>Manage existing users</h3>

            <p>
              Search for an account, update account information or remove
              an account.
            </p>

            <Link className="admin-card-link" to="/admin/users">
              Manage users
            </Link>
          </article>

          <article className="admin-feature-card">
            <span className="admin-card-number" aria-hidden="true">
              03
            </span>

            <p className="admin-card-category">Training progress</p>
            <h3>Participant progress</h3>

            <p>
              Find a participant and review their recorded skill-tree
              progression.
            </p>

            <Link className="admin-card-link" to="/admin/progress">
              View progress
            </Link>
          </article>

          <article className="admin-feature-card">
            <span className="admin-card-number" aria-hidden="true">
              04
            </span>

            <p className="admin-card-category">Administrator account</p>
            <h3>My profile</h3>

            <p>
              View the account information associated with your current
              session.
            </p>

            <Link className="admin-card-link" to="/profile">
              View profile
            </Link>
          </article>
        </section>

       <section aria-labelledby="summary-heading">
  <p className="admin-card-category">Dashboard summary</p>

  <h2 className="admin-section-title" id="summary-heading">
    Account overview
  </h2>

  {isLoadingSummary && (
    <p className="admin-form-status" role="status">
      Loading account summary…
    </p>
  )}

  {!isLoadingSummary && summary && (
    <div className="admin-summary-grid">
      <article className="admin-summary-card">
        <span className="admin-summary-number">
          {summary.total}
        </span>

        <span className="admin-summary-label">
          Total accounts
        </span>
      </article>

      <article className="admin-summary-card">
        <span className="admin-summary-number">
          {summary.participants}
        </span>

        <span className="admin-summary-label">
          Participants
        </span>
      </article>

      <article className="admin-summary-card">
        <span className="admin-summary-number">
          {summary.instructors}
        </span>

        <span className="admin-summary-label">
          Instructors
        </span>
      </article>

      <article className="admin-summary-card">
        <span className="admin-summary-number">
          {summary.administrators}
        </span>

        <span className="admin-summary-label">
          Administrators
        </span>
      </article>
    </div>
  )}
</section>
      </main>
    </div>
  );
}

export default AdminDashboardPage;