import { useEffect, useState, type FormEvent } from "react";
import { Link } from "react-router";

import { searchEvents, type EventDto } from "../api/eventsApi";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";

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

  return (
    <main className="admin-main">
      <p className="admin-eyebrow">Event management</p>
      <h1 className="admin-title">Events</h1>

      <div className="admin-form-actions">
        <Link
          className="site-button site-button-primary"
          to="/admin/events/new"
        >
          Create Event
        </Link>
      </div>

      <section className="admin-panel" aria-label="Event list">
        <form className="admin-search-form" onSubmit={handleSearch}>
          <div className="admin-form-group">
            <label htmlFor="eventSearch">Search events</label>
            <input
              id="eventSearch"
              type="search"
              value={search}
              maxLength={100}
              onChange={(event) => setSearch(event.target.value)}
            />
          </div>

          <button className="site-button site-button-primary" type="submit">
            Search
          </button>
        </form>

        {isLoading && <p role="status">Loading events…</p>}
        {error && <p role="alert">{error}</p>}

        {!isLoading && !error && (
          events.length === 0 ? (
            <p>
              {query.search ? "No events match your search." : "No events found."}
            </p>
          ) : (
            <ul>
              {events.map((event) => (
  <li key={event.eventId}>
    <Link to={`/admin/events/${event.eventId}`}>
      <strong>{event.eventName}</strong>
    </Link>
    {" — "}
    {event.eventLocation}
    {event.isCancelled && " (Cancelled)"}
  </li>
))}
            </ul>
          )
        )}
      </section>
    </main>
  );
}