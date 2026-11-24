import { useState, type FormEvent } from "react";
import { Link, useNavigate } from "react-router";

import { createEvent, type EventRegistrationType } from "../api/eventsApi";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";

export default function AdminCreateEventPage() {
  const navigate = useNavigate();
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (isSubmitting) return;

    const form = new FormData(event.currentTarget);
    const eventName = String(form.get("eventName") ?? "").trim();
    const eventLocation = String(form.get("eventLocation") ?? "").trim();
    const description = String(form.get("description") ?? "").trim();
    const eventStart = new Date(String(form.get("eventStart") ?? ""));
    const eventEnd = new Date(String(form.get("eventEnd") ?? ""));
    const registrationType = form.get("registrationType");

    setError("");

    if (!eventName || !eventLocation) {
      setError("Enter an event name and location.");
      return;
    }

    if (
      !Number.isFinite(eventStart.getTime()) ||
      !Number.isFinite(eventEnd.getTime()) ||
      eventEnd <= eventStart
    ) {
      setError("Enter a valid start and end time. The end must be after the start.");
      return;
    }

    if (registrationType !== "PRIVATE" && registrationType !== "PUBLIC") {
      setError("Select a registration type.");
      return;
    }

    const selectedType: EventRegistrationType = registrationType;

    setIsSubmitting(true);
    try {
      await createEvent({
        eventName,
        eventLocation,
        description: description || null,
        eventStart: eventStart.toISOString(),
        eventEnd: eventEnd.toISOString(),
        registrationType: selectedType,
      });

      navigate("/admin/events", { replace: true });
    } catch (error) {
      setError(error instanceof Error ? error.message : "Could not create the event.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="admin-main">
      <p className="admin-eyebrow">Event management</p>
      <h1 className="admin-title">Create Event</h1>

      <section className="admin-panel admin-form-panel" aria-label="Event details">
        <form className="admin-form" onSubmit={handleSubmit}>
          <div className="admin-form-group">
            <label htmlFor="eventName">Event name</label>
            <input id="eventName" name="eventName" maxLength={400} required />
          </div>

          <div className="admin-form-group">
            <label htmlFor="eventLocation">Location</label>
            <input id="eventLocation" name="eventLocation" required />
          </div>

          <div className="admin-form-group">
            <label htmlFor="eventDescription">Description (optional)</label>
            <textarea id="eventDescription" name="description" rows={4} />
          </div>

          <div className="admin-form-grid">
            <div className="admin-form-group">
              <label htmlFor="eventStart">Starts at (local time)</label>
              <input id="eventStart" name="eventStart" type="datetime-local" required />
            </div>

            <div className="admin-form-group">
              <label htmlFor="eventEnd">Ends at (local time)</label>
              <input id="eventEnd" name="eventEnd" type="datetime-local" required />
            </div>
          </div>

          <div className="admin-form-group">
            <label htmlFor="registrationType">Registration type</label>
            <select id="registrationType" name="registrationType" defaultValue="PRIVATE">
              <option value="PRIVATE">Private</option>
              <option value="PUBLIC">Public</option>
            </select>
          </div>

          {error && <p className="admin-form-error" role="alert">{error}</p>}

          <div className="admin-form-actions">
            <button
              className="site-button site-button-primary"
              type="submit"
              disabled={isSubmitting}
            >
              {isSubmitting ? "Creating event…" : "Create Event"}
            </button>
            <Link className="site-button site-button-secondary" to="/admin/events">
              Cancel
            </Link>
          </div>
        </form>
      </section>
    </main>
  );
}