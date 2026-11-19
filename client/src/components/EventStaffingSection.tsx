import { useEffect, useState, type FormEvent } from "react";

import { getAdminUsers, type AdminUser } from "../api/adminUsersApi";
import {
  assignEventStaff,
  getEventStaff,
  unassignEventStaff,
  type EventStaffDto,
} from "../api/eventStaffApi";

export default function EventStaffingSection({
  eventId,
}: {
  eventId: number;
}) {
  const [staff, setStaff] = useState<EventStaffDto[] | null>(null);
  const [instructors, setInstructors] = useState<AdminUser[]>([]);
  const [selectedId, setSelectedId] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");
  const [status, setStatus] = useState("");

  useEffect(() => {
    let ignore = false;

    async function loadStaffing() {
      try {
        const [members, users] = await Promise.all([
          getEventStaff(eventId),
          getAdminUsers(),
        ]);

        if (ignore) return;
        setStaff(members);
        setInstructors(
          users.filter((user) => user.roles.includes("INSTRUCTOR")),
        );
      } catch (err) {
        if (!ignore) {
          setError(err instanceof Error ? err.message : "Could not load staffing.");
        }
      }
    }

    void loadStaffing();

    return () => {
      ignore = true;
    };
  }, [eventId]);

  const availableInstructors = instructors.filter(
    (user) => !staff?.some((member) => member.userId === user.id),
  );

  async function saveChange(change: () => Promise<void>, message: string) {
    if (isSaving) return;

    setIsSaving(true);
    setError("");
    setStatus("");

    try {
      await change();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Could not update staffing.");
      setIsSaving(false);
      return;
    }

    setStatus(message);
    setSelectedId("");

    try {
      setStaff(await getEventStaff(eventId));
    } catch {
      setStaff(null);
      setError("The change was saved, but the list could not refresh. Reload the page.");
    } finally {
      setIsSaving(false);
    }
  }

  function handleAssign(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!availableInstructors.some((user) => user.id === selectedId)) return;

    void saveChange(
      () => assignEventStaff(eventId, selectedId),
      "Instructor assigned.",
    );
  }

  function handleRemove(member: EventStaffDto) {
    const name = `${member.firstName} ${member.lastName}`;
    if (!window.confirm(`Remove ${name} from this event?`)) return;

    void saveChange(
      () => unassignEventStaff(eventId, member.userId),
      "Instructor removed from this event.",
    );
  }

  return (
    <section className="admin-panel" aria-labelledby="event-staffing-heading">
      <h2 id="event-staffing-heading">Event Staffing</h2>

      {error && <p className="admin-form-error" role="alert">{error}</p>}
      {status && <p className="admin-form-status" role="status">{status}</p>}
      {staff === null && !error && <p role="status">Loading staffing…</p>}

      {staff !== null && (
        <>
          {staff.length === 0 ? (
            <p>No instructors are assigned to this event yet.</p>
          ) : (
            <ul>
              {staff.map((member) => (
                <li key={member.userId}>
                  {member.firstName} {member.lastName}{" "}
                  <button
                    className="site-button site-button-danger"
                    type="button"
                    disabled={isSaving}
                    aria-label={`Remove ${member.firstName} ${member.lastName}`}
                    onClick={() => handleRemove(member)}
                  >
                    Remove
                  </button>
                </li>
              ))}
            </ul>
          )}

          <form className="admin-form" onSubmit={handleAssign}>
            <div className="admin-form-group">
              <label htmlFor="event-instructor">Instructor to assign</label>
              <select
                id="event-instructor"
                value={selectedId}
                onChange={(event) => setSelectedId(event.target.value)}
                disabled={isSaving || availableInstructors.length === 0}
                required
              >
                <option value="">Select an instructor</option>
                {availableInstructors.map((user) => (
                  <option key={user.id} value={user.id}>
                    {user.firstName} {user.lastName}
                    {user.email ? ` (${user.email})` : ""}
                  </option>
                ))}
              </select>
            </div>

            {availableInstructors.length === 0 && (
              <p>No unassigned instructor accounts are available.</p>
            )}

            <div className="admin-form-actions">
              <button
                className="site-button site-button-primary"
                type="submit"
                disabled={isSaving || !selectedId}
              >
                Assign instructor
              </button>
            </div>
          </form>
        </>
      )}
    </section>
  );
}