import { useEffect, useState } from "react";
import { Link } from "react-router";

import { searchEvents, type EventDto } from "../api/eventsApi";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";

export default function InstructorEventsPage() {
  const [events, setEvents] = useState<EventDto[] | null>(null);
  const [error, setError] = useState("");

  useEffect(() => {
    let ignore = false;

    async function loadEvents() {
      try {
        const result = await searchEvents();

        if (!Array.isArray(result)) {
          throw new Error("The server returned an unexpected event list.");
        }

        if (!ignore) setEvents(result);
      } catch (err) {
        if (!ignore) {
          setError(err instanceof Error ? err.message : "Could not load events.");
        }
      }
    }

    void loadEvents();
    return () => {
      ignore = true;
    };
  }, []);

  return (
    <main className="admin-main">
      <Link className="admin-back-link" to="/profile">
        ← Back to profile
      </Link>

      <p className="admin-eyebrow">Instructor portal</p>
      <h1 className="admin-title">My Events</h1>

      <section className="admin-panel" aria-label="Instructor events">
        {error ? (
          <p className="admin-form-error" role="alert">{error}</p>
        ) : events === null ? (
          <p role="status">Loading events…</p>
        ) : events.length === 0 ? (
          <p>No events found.</p>
        ) : (
          <ul>
            {events.map((event) => (
              <li key={event.eventId}>
                <Link to={`/instructor/events/${event.eventId}`}>
                  {event.eventName}
                </Link>
                {" — "}
                {event.eventLocation}
              </li>
            ))}
          </ul>
        )}
      </section>
    </main>
  );
}