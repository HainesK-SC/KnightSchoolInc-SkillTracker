import { useEffect, useState } from "react";
import { Link, useParams } from "react-router";

import { getEvent, type EventDto } from "../api/eventsApi";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";
import EventStaffingSection from "../components/EventStaffingSection";
import EventSkillsSection from "../components/EventSkillsSection";
import EventParticipantsSection from "../components/EventParticipantsSection";

export default function AdminEventDetailsPage() {
  const { eventId } = useParams();

  // Reset loading and error state when navigating to a different event.
  return <EventDetails key={eventId} eventId={Number(eventId)} />;
}

function EventDetails({ eventId }: { eventId: number }) {
  const [event, setEvent] = useState<EventDto | null>(null);
  const [error, setError] = useState("");

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
  }, [eventId, validId]);

  return (
    <main className="admin-main">
      <Link className="admin-back-link" to="/admin/events">
        ← Back to events
      </Link>

      <p className="admin-eyebrow">Event management</p>
      <h1 className="admin-title">{event?.eventName ?? "Event Details"}</h1>

      {!validId && <p role="alert">This event link is invalid.</p>}
      {error && <p className="admin-form-error" role="alert">{error}</p>}
      {validId && !error && !event && <p role="status">Loading event…</p>}

      {event && (
        <section className="admin-panel" aria-labelledby="event-information">
          <h2 id="event-information">Event information</h2>

          <dl>
            <dt>Location</dt>
            <dd>{event.eventLocation}</dd>

            <dt>Starts</dt>
            <dd>{new Date(event.eventStart).toLocaleString()}</dd>

            <dt>Ends</dt>
            <dd>{new Date(event.eventEnd).toLocaleString()}</dd>

            <dt>Registration type</dt>
            <dd>{event.registrationType === "PRIVATE" ? "Private" : "Public"}</dd>

            <dt>Status</dt>
            <dd>{event.isCancelled ? "Cancelled" : "Not cancelled"}</dd>
          </dl>

          <p>{event.description || "No description provided."}</p>
        </section>
      )}

      {event && <EventStaffingSection eventId={eventId} />}
        {event && <EventSkillsSection eventId={eventId} />}
        {event && <EventParticipantsSection eventId={eventId} />}
    </main>
  );
}