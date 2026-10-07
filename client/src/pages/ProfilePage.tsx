import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router";

import { ApiError } from "../api/apiClient";
import {
  getCurrentUser,
  logoutUser,
  type CurrentUser,
} from "../api/authApi";
import mainLogo from "../assets/branding/main-logo.svg";
import "./ProfilePage.css";

function formatRole(role: string) {
  return role
    .toLowerCase()
    .split("_")
    .map((word) => word.charAt(0).toUpperCase() + word.slice(1))
    .join(" ");
}

function ProfilePage() {
  const navigate = useNavigate();

  const [user, setUser] = useState<CurrentUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSigningOut, setIsSigningOut] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");

  const loadProfile = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage("");

    try {
      const currentUser = await getCurrentUser();
      setUser(currentUser);
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        navigate("/login", { replace: true });
        return;
      }

      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "Something unexpected happened while loading your profile.",
        );
      }
    } finally {
      setIsLoading(false);
    }
  }, [navigate]);

  useEffect(() => {
    void loadProfile();
  }, [loadProfile]);

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

  const initials = user
    ? `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase()
    : "KS";

  const roleDisplay =
    user && user.roles.length > 0
      ? user.roles.map(formatRole).join(", ")
      : "Role not assigned";

  return (
    <div className="profile-page">
      <header className="profile-header">
        <div className="profile-header-content">
          <Link to="/profile" aria-label="Knight School profile home">
            <img
              className="profile-logo"
              src={mainLogo}
              alt="Knight School Inc."
            />
          </Link>

          <nav className="profile-navigation" aria-label="Main navigation">
            <Link to="/profile" aria-current="page">
              My profile
            </Link>

            <Link to="/skill-tree">Skill tree</Link>

            <button
              className="profile-sign-out"
              type="button"
              onClick={handleSignOut}
              disabled={isSigningOut}
            >
              {isSigningOut ? "Signing out..." : "Sign out"}
            </button>
          </nav>
        </div>
      </header>

      <main className="profile-main">
        {isLoading && (
          <section className="profile-card profile-state" role="status">
            <h1>Loading your profile...</h1>
            <p>Please wait while we retrieve your account information.</p>
          </section>
        )}

        {!isLoading && !user && (
          <section className="profile-card profile-state" role="alert">
            <h1>Profile unavailable</h1>

            <p>
              {errorMessage ||
                "Your profile information could not be loaded."}
            </p>

            <button
              className="profile-action"
              type="button"
              onClick={() => void loadProfile()}
            >
              Try again
            </button>
          </section>
        )}

        {!isLoading && user && (
          <>
            {errorMessage && (
              <p className="profile-error" role="alert">
                {errorMessage}
              </p>
            )}

            <section
              className="profile-hero"
              aria-labelledby="profile-title"
            >
              <p className="profile-avatar" aria-hidden="true">
                {initials}
              </p>

              <div>
                <h1 id="profile-title">
                  {user.displayName ||
                    `${user.firstName} ${user.lastName}`}
                </h1>

                <p className="profile-role">{roleDisplay}</p>
              </div>
            </section>

            <div className="profile-grid">
              <section
                className="profile-card"
                aria-labelledby="account-heading"
              >
                <h2 id="account-heading">Account details</h2>

                <dl className="profile-details">
                  <div className="profile-detail">
                    <dt>First name</dt>
                    <dd>{user.firstName}</dd>
                  </div>

                  <div className="profile-detail">
                    <dt>Last name</dt>
                    <dd>{user.lastName}</dd>
                  </div>

                  <div className="profile-detail">
                    <dt>Email address</dt>
                    <dd>{user.email}</dd>
                  </div>

                  <div className="profile-detail">
                    <dt>Account role</dt>
                    <dd>{roleDisplay}</dd>
                  </div>
                </dl>
              </section>

              <section
                className="profile-card"
                aria-labelledby="training-heading"
              >
                <h2 id="training-heading">Training progress</h2>

                <p className="profile-empty-message">
                  Your training progress will appear here when it becomes
                  available.
                </p>

                <Link
                  className="profile-action profile-action-link"
                  to="/skill-tree"
                >
                  View skill tree
                </Link>
              </section>
            </div>
          </>
        )}
      </main>
    </div>
  );
}

export default ProfilePage;