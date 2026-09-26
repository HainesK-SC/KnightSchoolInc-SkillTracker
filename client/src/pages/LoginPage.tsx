import { useState, type FormEvent } from "react";
import { Link } from "react-router";

import mainLogo from "../assets/branding/main-logo.svg";
import "./LoginPage.css";

function LoginPage() {
  const [showPassword, setShowPassword] = useState(false);
  const [statusMessage, setStatusMessage] = useState("");

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    setStatusMessage(
      "The login form works. Backend authentication will be connected later."
    );
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
                  required
                />

                <button
                  className="password-toggle"
                  type="button"
                  aria-label={showPassword ? "Hide password" : "Show password"}
                  aria-pressed={showPassword}
                  onClick={() => setShowPassword((current) => !current)}
                >
                  {showPassword ? "Hide" : "Show"}
                </button>
              </div>
            </div>

            <button className="auth-submit" type="submit">
              Sign in
            </button>

            {statusMessage && (
              <p className="auth-status" role="status">
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