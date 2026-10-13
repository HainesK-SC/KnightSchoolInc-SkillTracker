import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate } from "react-router";

import { ApiError } from "../api/apiClient";
import {
  getCurrentUser,
  logoutUser,
  type CurrentUser,
} from "../api/authApi";
import mainLogo from "../assets/branding/main-logo.svg";
import DeleteProfileSection from "../components/DeleteProfileSection";
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
            {user?.roles.includes("ADMINISTRATOR") && (
              <Link to="/admin">Admin dashboard</Link>
            )}

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
            <div className="profile-state-symbol" aria-hidden="true">
              ⚔
            </div>

            <h1>Retrieving your record…</h1>
            <p>Please wait while we open the Knight School archives.</p>
          </section>
        )}

        {!isLoading && !user && (
          <section className="profile-card profile-state" role="alert">
            <div className="profile-state-symbol" aria-hidden="true">
              !
            </div>

            <h1>Record unavailable</h1>

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
              <div className="profile-avatar-frame">
                <p className="profile-avatar" aria-hidden="true">
                  {initials}
                </p>
              </div>

              <div className="profile-hero-copy">
                <p className="profile-hero-eyebrow">
                  Knight School heraldic record
                </p>

                <h1 id="profile-title">
                  {user.displayName ||
                    `${user.firstName} ${user.lastName}`}
                </h1>

                <div
                  className="profile-role-badges"
                  aria-label={`Account roles: ${roleDisplay}`}
                >
                  {user.roles.length > 0 ? (
                    user.roles.map((role) => (
                      <span className="profile-role-badge" key={role}>
                        {formatRole(role)}
                      </span>
                    ))
                  ) : (
                    <span className="profile-role-badge">
                      Role not assigned
                    </span>
                  )}
                </div>
              </div>
            </section>

            <div className="profile-ornament" aria-hidden="true">
              <span />
              <strong>✦</strong>
              <span />
            </div>

            <div className="profile-grid">
              <section
                className="profile-card profile-record-card"
                aria-labelledby="account-heading"
              >
                <div className="profile-card-heading">
                  <span className="profile-card-icon" aria-hidden="true">
                    ⚜
                  </span>

                  <div>
                    <p className="profile-card-eyebrow">
                      Personal registry
                    </p>

                    <h2 id="account-heading">Knight’s Record</h2>
                  </div>
                </div>

                <dl className="profile-details">
                  <div className="profile-detail">
                    <dt>Given name</dt>
                    <dd>{user.firstName}</dd>
                  </div>

                  <div className="profile-detail">
                    <dt>Family name</dt>
                    <dd>{user.lastName}</dd>
                  </div>

                  <div className="profile-detail">
                    <dt>Email address</dt>
                    <dd>{user.email ?? "No email provided"}</dd>
                  </div>

                  <div className="profile-detail">
                    <dt>Standing within Knight School</dt>
                    <dd>{roleDisplay}</dd>
                  </div>
                </dl>
              </section>

              <section
                className="profile-card profile-training-card"
                aria-labelledby="training-heading"
              >
                <div className="profile-card-heading">
                  <span className="profile-card-icon" aria-hidden="true">
                    ⚔
                  </span>

                  <div>
                    <p className="profile-card-eyebrow">
                      Training record
                    </p>

                    <h2 id="training-heading">
                      Training Chronicle
                    </h2>
                  </div>
                </div>

                <div className="profile-scroll-empty">
                  <div className="profile-wax-seal" aria-hidden="true">
                    KS
                  </div>

                  <h3>Awaiting your first recorded achievement</h3>

                  <p>
                    Your training progress will appear in this chronicle
                    when skill information becomes available.
                  </p>
                </div>

                <Link
                  className="profile-action profile-action-link"
                  to="/skill-tree"
                >
                  Open skill map
                </Link>
              </section>
            </div>

            <DeleteProfileSection />
          </>
        )}
      </main>
    </div>
  );
}

export default ProfilePage;