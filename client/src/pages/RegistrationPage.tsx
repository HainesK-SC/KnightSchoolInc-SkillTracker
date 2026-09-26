import { useState, type FormEvent } from "react";
import { Link } from "react-router";

import mainLogo from "../assets/branding/main-logo.svg";
import "./LoginPage.css";

function RegistrationPage() {
  const [statusMessage, setStatusMessage] = useState("");
  const [errorMessage, setErrorMessage] = useState("");

  function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const form = new FormData(event.currentTarget);
    const password = form.get("password");
    const confirmPassword = form.get("confirmPassword");

    if (password !== confirmPassword) {
      setStatusMessage("");
      setErrorMessage("The passwords do not match.");
      return;
    }

    setErrorMessage("");
    setStatusMessage(
      "The registration form works. Account creation will be connected to the backend later."
    );
  }

  return (
    <main className="auth-page">
      <section className="auth-card" aria-labelledby="registration-title">
        <div className="auth-brand-panel">
          <img
            className="auth-logo"
            src={mainLogo}
            alt="Knight School Inc."
          />

          <p className="auth-brand-message">
            Create your account to begin tracking skills, achievements, and
            progress through Knight School.
          </p>
        </div>

        <div className="auth-form-panel">
          <p className="auth-eyebrow">Participant portal</p>

          <h1 className="auth-title" id="registration-title">
            Create account
          </h1>

          <p className="auth-description">
            Enter your information to create your Knight School account.
          </p>

          <form className="auth-form" onSubmit={handleSubmit}>
            <div className="form-group">
              <label className="form-label" htmlFor="firstName">
                First name
              </label>

              <input
                className="form-input"
                id="firstName"
                name="firstName"
                type="text"
                autoComplete="given-name"
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="lastName">
                Last name
              </label>

              <input
                className="form-input"
                id="lastName"
                name="lastName"
                type="text"
                autoComplete="family-name"
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="registrationEmail">
                Email address
              </label>

              <input
                className="form-input"
                id="registrationEmail"
                name="email"
                type="email"
                autoComplete="email"
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="registrationPassword">
                Password
              </label>

              <input
                className="form-input"
                id="registrationPassword"
                name="password"
                type="password"
                autoComplete="new-password"
                minLength={8}
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="confirmPassword">
                Confirm password
              </label>

              <input
                className="form-input"
                id="confirmPassword"
                name="confirmPassword"
                type="password"
                autoComplete="new-password"
                minLength={8}
                required
              />
            </div>

            <button className="auth-submit" type="submit">
              Create account
            </button>

            {errorMessage && (
              <p className="auth-status" role="alert">
                {errorMessage}
              </p>
            )}

            {statusMessage && (
              <p className="auth-status" role="status">
                {statusMessage}
              </p>
            )}
          </form>

          <p className="auth-register">
            Already have an account? <Link to="/login">Sign in</Link>
          </p>
        </div>
      </section>
    </main>
  );
}

export default RegistrationPage;