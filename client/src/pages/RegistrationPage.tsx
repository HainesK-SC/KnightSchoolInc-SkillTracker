import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router";

import { ApiError } from "../api/apiClient";
import { registerUser } from "../api/authApi";
import mainLogo from "../assets/branding/main-logo.svg";
import "./LoginPage.css";

function RegistrationPage() {
  const navigate = useNavigate();

  const [errorMessage, setErrorMessage] = useState("");
  const [isSubmitting, setIsSubmitting] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();

    const form = new FormData(event.currentTarget);

    const firstName = String(form.get("firstName") ?? "").trim();
    const lastName = String(form.get("lastName") ?? "").trim();
    const email = String(form.get("email") ?? "").trim();
    const password = String(form.get("password") ?? "");
    const confirmPassword = String(form.get("confirmPassword") ?? "");

    setErrorMessage("");

    if (password !== confirmPassword) {
      setErrorMessage("The passwords do not match.");
      return;
    }

    const hasValidPassword =
      password.length >= 8 &&
      /[a-z]/.test(password) &&
      /[A-Z]/.test(password) &&
      /\d/.test(password) &&
      /[^A-Za-z0-9]/.test(password);

    if (!hasValidPassword) {
      setErrorMessage(
        "Use at least 8 characters with an uppercase letter, lowercase letter, number, and symbol.",
      );
      return;
    }

    setIsSubmitting(true);

    try {
      await registerUser({
        firstName,
        lastName,
        email,
        password,
      });

      navigate("/profile", {
        replace: true,
      });
    } catch (error) {
      if (error instanceof ApiError) {
        setErrorMessage(error.message);
      } else {
        setErrorMessage(
          "Something unexpected happened. Please try again.",
        );
      }
    } finally {
      setIsSubmitting(false);
    }
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
                maxLength={50}
                disabled={isSubmitting}
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
                maxLength={50}
                disabled={isSubmitting}
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
                maxLength={256}
                disabled={isSubmitting}
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
                disabled={isSubmitting}
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
                disabled={isSubmitting}
                required
              />
            </div>

            <button
              className="auth-submit"
              type="submit"
              disabled={isSubmitting}
            >
              {isSubmitting ? "Creating account..." : "Create account"}
            </button>

            {errorMessage && (
              <p className="auth-status" role="alert">
                {errorMessage}
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