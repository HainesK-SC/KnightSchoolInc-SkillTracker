import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router";

import { ApiError } from "../api/apiClient";
import { loginUser } from "../api/authApi";
import mainLogo from "../assets/branding/main-logo.svg";
import "./LoginPage.css";

function LoginPage() {
  const navigate = useNavigate();

  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [showPassword, setShowPassword] = useState(false);
  const [statusMessage, setStatusMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setStatusMessage("");
    setIsSubmitting(true);

    try {
      const user = await loginUser({
        email: email.trim(),
        password,
      });

      const isAdministrator = user.roles.includes("ADMINISTRATOR");

      navigate(isAdministrator ? "/admin" : "/profile", {
        replace: true,
      });
    } catch (error) {
      if (error instanceof ApiError) {
        if (error.status === 401) {
          setStatusMessage("The email or password is incorrect.");
        } else {
          setStatusMessage(error.message);
        }
      } else {
        setStatusMessage(
          "Something unexpected happened. Please try again.",
        );
      }
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="auth-page">
      <section className="auth-card" aria-labelledby="login-title">
        <div className="auth-brand-panel">
          <img
            className="auth-logo"
            src={mainLogo}
            alt="Knight School Inc."
          />

          <p className="auth-brand-message">
            Track your training, celebrate your achievements, and continue your
            journey through the Knight School skill tree.
          </p>
        </div>

        <div className="auth-form-panel">
          <p className="auth-eyebrow">Participant portal</p>

          <h1 className="auth-title" id="login-title">
            Sign in
          </h1>

          <p className="auth-description">
            Enter your account information to continue.
          </p>

          <form className="auth-form" onSubmit={handleSubmit}>
            <div className="form-group">
              <label className="form-label" htmlFor="email">
                Email address
              </label>

              <input
                className="form-input"
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                disabled={isSubmitting}
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="password">
                Password
              </label>

              <div className="password-field">
                <input
                  className="form-input"
                  id="password"
                  name="password"
                  type={showPassword ? "text" : "password"}
                  autoComplete="current-password"
                  value={password}
                  onChange={(event) => setPassword(event.target.value)}
                  disabled={isSubmitting}
                  required
                />

                <button
                  className="password-toggle"
                  type="button"
                  aria-label={showPassword ? "Hide password" : "Show password"}
                  aria-pressed={showPassword}
                  onClick={() => setShowPassword((current) => !current)}
                  disabled={isSubmitting}
                >
                  {showPassword ? "Hide" : "Show"}
                </button>
              </div>
            </div>

            <button
              className="auth-submit"
              type="submit"
              disabled={isSubmitting}
            >
              {isSubmitting ? "Signing in..." : "Sign in"}
            </button>

            {statusMessage && (
              <p className="auth-status" role="alert">
                {statusMessage}
              </p>
            )}
          </form>

          <p className="auth-register">
            New to Knight School?{" "}
            <Link to="/register">Create an account</Link>
          </p>
        </div>
      </section>
    </main>
  );
}

export default LoginPage;