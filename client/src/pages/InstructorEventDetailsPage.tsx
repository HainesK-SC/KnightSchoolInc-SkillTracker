import { Link, useParams } from "react-router";

import EventParticipantsSection from "../components/EventParticipantsSection";
import "./AdminDashboardPage.css";
import "./AdminManagementPage.css";

export default function InstructorEventDetailsPage() {
  const { eventId: routeEventId } = useParams();
  const eventId = Number(routeEventId);
  const validId = Number.isSafeInteger(eventId) && eventId > 0;

  return (
    <main className="admin-main">
      <Link className="admin-back-link" to="/instructor/events">
        ← Back to my events
      </Link>

      <p className="admin-eyebrow">Instructor portal</p>
      <h1 className="admin-title">Event Roster</h1>

      {validId ? (
        <EventParticipantsSection
          key={eventId}
          eventId={eventId}
          showEnrollment={false}
        />
      ) : (
        <p role="alert">This event link is invalid.</p>
      )}
    </main>
  );
}