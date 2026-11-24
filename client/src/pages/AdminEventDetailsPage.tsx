import { useEffect, useState } from "react";
import { Link, useParams } from "react-router";

import { getEvent, type EventDto } from "../api/eventsApi";
import EventParticipantsSection from "../components/EventParticipantsSection";
import EventSkillsSection from "../components/EventSkillsSection";
import EventStaffingSection from "../components/EventStaffingSection";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";
import "./AdminEventsPage.css";
import "./AdminEventDetailsPage.css";

const dateFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: "medium",
  timeStyle: "short",
});

function formatDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? "Date unavailable"
    : dateFormatter.format(date);
}

export default function AdminEventDetailsPage() {
  const { eventId } = useParams();

  return <EventDetails key={eventId} eventId={Number(eventId)} />;
}

function EventDetails({ eventId }: { eventId: number }) {
  const [event, setEvent] = useState<EventDto | null>(null);
  const [error, setError] = useState("");
  const [attempt, setAttempt] = useState(0);
  const validId = Number.isSafeInteger(eventId) && eventId > 0;

  useEffect(() => {
    if (!validId) return;

    let ignore = false;

    getEvent(eventId)
      .then((result) => {
        if (!ignore) setEvent(result);
      })
      .catch((err: unknown) => {
        if (!ignore) {
          setError(err instanceof Error ? err.message : "Could not load the event.");
        }
      });

    return () => {
      ignore = true;
    };
  }, [eventId, validId, attempt]);

  const registrationLabel =
    event?.registrationType === "PRIVATE"
      ? "Private"
      : event?.registrationType === "PUBLIC"
        ? "Public"
        : "Unavailable";

  return (
    <main className="admin-main admin-events admin-event-details">
      <Link className="admin-back-link" to="/admin/events">
        ← Back to events
      </Link>

      <header className="admin-events-hero">
        <div>
          <p className="admin-events-eyebrow">Event management</p>
          <h1>{event?.eventName ?? "Event Details"}</h1>
          {event && (
            <p className="admin-events-intro">{event.eventLocation}</p>
          )}
        </div>

        {event && (
          <div className="admin-event-card-badges">
            {registrationLabel !== "Unavailable" && (
              <span className="admin-event-badge">
                {registrationLabel} registration
              </span>
            )}
            {event.isCancelled && (
              <span className="admin-event-badge admin-event-badge-cancelled">
                Cancelled
              </span>
            )}
          </div>
        )}
      </header>

      {!validId && (
        <p className="admin-form-error" role="alert">
          This event link is invalid.
        </p>
      )}

      {validId && !event && !error && (
        <p className="admin-panel" role="status">Loading event…</p>
      )}

      {error && (
        <div className="admin-panel">
          <p className="admin-form-error" role="alert">{error}</p>
          <button
            className="site-button site-button-secondary"
            type="button"
            onClick={() => {
              setError("");
              setAttempt((current) => current + 1);
            }}
          >
            Try again
          </button>
        </div>
      )}

      {event && (
        <>
          <nav className="admin-event-jump-links" aria-label="Jump to event section">
            <a href="#event-overview">Overview</a>
            <a href="#event-staffing">Staffing</a>
            <a href="#event-skills">Skills</a>
            <a href="#event-participants">Participants</a>
          </nav>

          <div className="admin-event-sections">
            <section
              id="event-overview"
              className="admin-panel admin-event-overview"
              aria-labelledby="event-information"
            >
              <h2 id="event-information">Event information</h2>

              <dl className="admin-event-facts">
                <div>
                  <dt>Starts</dt>
                  <dd>{formatDate(event.eventStart)}</dd>
                </div>
                <div>
                  <dt>Ends</dt>
                  <dd>{formatDate(event.eventEnd)}</dd>
                </div>
                <div>
                  <dt>Location</dt>
                  <dd>{event.eventLocation}</dd>
                </div>
                <div>
                  <dt>Registration</dt>
                  <dd>{registrationLabel}</dd>
                </div>
              </dl>

              <div className="admin-event-description">
                <h3>About this event</h3>
                <p>{event.description?.trim() || "No description provided."}</p>
              </div>
            </section>

            <div id="event-staffing">
              <EventStaffingSection eventId={eventId} />
            </div>

            <div id="event-skills">
              <EventSkillsSection eventId={eventId} />
            </div>

            <div id="event-participants">
              <EventParticipantsSection eventId={eventId} />
            </div>
          </div>
        </>
      )}
    </main>
  );
}