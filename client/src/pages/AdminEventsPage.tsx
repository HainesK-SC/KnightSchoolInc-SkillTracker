import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router";

import { searchEvents, type EventDto } from "../api/eventsApi";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";
import "./AdminEventsPage.css";

const eventDateFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: "medium",
  timeStyle: "short",
});

function formatEventDate(value: string) {
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? "Date unavailable"
    : eventDateFormatter.format(date);
}

export default function AdminEventsPage() {
  const [search, setSearch] = useState("");
  const [query, setQuery] = useState({ search: "" });
  const [events, setEvents] = useState<EventDto[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let ignore = false;

    async function loadEvents() {
      setIsLoading(true);
      setError("");

      try {
        const result = await searchEvents(query);

        if (!Array.isArray(result)) {
          throw new Error("The server returned an unexpected event list format.");
        }

        if (!ignore) setEvents(result);
      } catch (error) {
        if (!ignore) {
          setError(
            error instanceof Error ? error.message : "Could not load events.",
          );
        }
      } finally {
        if (!ignore) setIsLoading(false);
      }
    }

    void loadEvents();

    return () => {
      ignore = true;
    };
  }, [query]);

  function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setQuery({ search: search.trim() });
  }

  function clearSearch() {
    setSearch("");
    setQuery({ search: "" });
  }

  return (
    <main className="admin-main admin-events">
      <Link className="admin-back-link" to="/admin">
        ← Back to dashboard
      </Link>

      <header className="admin-events-hero">
        <div>
          <p className="admin-events-eyebrow">Event management</p>
          <h1>Events</h1>
          <p className="admin-events-intro">
            Create workshops and open an event to manage its skills,
            instructors, and participants.
          </p>
        </div>

        <Link
          className="site-button site-button-primary admin-events-create"
          to="/admin/events/new"
        >
          Create Event
        </Link>
      </header>

      <section className="admin-panel admin-events-search" aria-label="Event search">
        <form className="admin-events-search-form" onSubmit={handleSearch}>
          <div className="admin-form-group">
            <label htmlFor="eventSearch">Search events</label>
            <input
              id="eventSearch"
              type="search"
              value={search}
              maxLength={100}
              placeholder="Enter an event name"
              onChange={(event) => setSearch(event.target.value)}
            />
          </div>

          <div className="admin-events-search-actions">
            <button className="site-button site-button-primary" type="submit">
              Search
            </button>

            {query.search && (
              <button
                className="site-button site-button-secondary"
                type="button"
                onClick={clearSearch}
              >
                Clear
              </button>
            )}
          </div>
        </form>
      </section>

      <section className="admin-events-results" aria-labelledby="event-list-heading">
        <div className="admin-events-list-heading">
          <h2 id="event-list-heading">
            {query.search ? "Search results" : "Event list"}
          </h2>

          {!isLoading && !error && (
            <span role="status">
              {events.length} {events.length === 1 ? "event" : "events"}
            </span>
          )}
        </div>

        {isLoading ? (
          <p className="admin-panel" role="status">Loading events…</p>
        ) : error ? (
          <div className="admin-panel">
            <p className="admin-form-error" role="alert">{error}</p>
            <button
              className="site-button site-button-secondary"
              type="button"
              onClick={() => setQuery({ ...query })}
            >
              Try again
            </button>
          </div>
        ) : events.length === 0 ? (
          <div className="admin-panel admin-events-empty">
            <h3>{query.search ? "No matching events" : "No events yet"}</h3>
            <p>
              {query.search
                ? `No events matched “${query.search}”. Try a different name or clear the search.`
                : "Create an event to start assigning skills, instructors, and participants."}
            </p>

            {query.search ? (
              <button
                className="site-button site-button-secondary"
                type="button"
                onClick={clearSearch}
              >
                Clear search
              </button>
            ) : (
              <Link className="site-button site-button-primary" to="/admin/events/new">
                Create your first event
              </Link>
            )}
          </div>
        ) : (
          <ul className="admin-events-grid" role="list">
            {events.map((event) => (
              <li className="admin-event-card" key={event.eventId}>
                <div className="admin-event-card-badges">
                  <span className="admin-event-badge">
                    {event.registrationType === "PRIVATE"
                      ? "Private registration"
                      : event.registrationType === "PUBLIC"
                        ? "Public registration"
                        : "Registration type unavailable"}
                  </span>

                  {event.isCancelled && (
                    <span className="admin-event-badge admin-event-badge-cancelled">
                      Cancelled
                    </span>
                  )}
                </div>

                <h3>{event.eventName}</h3>

                <dl className="admin-event-card-details">
                  <div>
                    <dt>Location</dt>
                    <dd>{event.eventLocation}</dd>
                  </div>
                  <div>
                    <dt>Starts</dt>
                    <dd>{formatEventDate(event.eventStart)}</dd>
                  </div>
                  <div>
                    <dt>Ends</dt>
                    <dd>{formatEventDate(event.eventEnd)}</dd>
                  </div>
                </dl>

                <Link
                  className="site-button site-button-secondary admin-event-card-link"
                  to={`/admin/events/${event.eventId}`}
                  aria-label={`View event: ${event.eventName}`}
                >
                  View Event →
                </Link>
              </li>
            ))}
          </ul>
        )}
      </section>
    </main>
  );
}