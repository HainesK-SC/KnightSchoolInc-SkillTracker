import { useEffect, useState, type FormEvent } from "react";

import {
  enrollExistingRegistrant,
  enrollNewRegistrant,
  getEventRoster,
  lookupEventRegistrant,
  type RegistrantLookupDto,
  type RosterEntryDto,
} from "../api/eventParticipantsApi";
import ParticipantEventSkillsEditor from "./ParticipantEventSkillsEditor";

function errorText(error: unknown, fallback: string) {
  return error instanceof Error ? error.message : fallback;
}

function participantName(person: RosterEntryDto) {
  return person.displayName || `${person.firstName} ${person.lastName}`;
}

export default function EventParticipantsSection({
  eventId,
  showEnrollment = true,
}: {
  eventId: number;
  showEnrollment?: boolean;
}) {
  const [roster, setRoster] = useState<RosterEntryDto[] | null>(null);
  const [selectedParticipant, setSelectedParticipant] =
    useState<RosterEntryDto | null>(null);
  const [rosterError, setRosterError] = useState("");
  const [rosterVersion, setRosterVersion] = useState(0);
  const [email, setEmail] = useState("");
  const [match, setMatch] = useState<RegistrantLookupDto | null>(null);
  const [pending, setPending] = useState<
    "lookup" | "enroll" | "create" | null
  >(null);
  const [actionError, setActionError] = useState("");
  const [status, setStatus] = useState("");

  useEffect(() => {
    let ignore = false;

    async function loadRoster() {
      try {
        const result = await getEventRoster(eventId);

        if (!Array.isArray(result)) {
          throw new Error("The server returned an unexpected roster format.");
        }

        if (!ignore) {
          setRoster(result);
          setRosterError("");
        }
      } catch (error) {
        if (!ignore) {
          setRosterError(errorText(error, "Could not load the roster."));
        }
      }
    }

    void loadRoster();

    return () => {
      ignore = true;
    };
  }, [eventId, rosterVersion]);

  function refreshRoster() {
    setRoster(null);
    setRosterError("");
    setRosterVersion((current) => current + 1);
  }

  async function handleLookup(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    const searchEmail = email.trim();
    if (!showEnrollment || pending || !searchEmail) return;

    setPending("lookup");
    setMatch(null);
    setActionError("");
    setStatus("");

    try {
      setMatch(await lookupEventRegistrant(eventId, searchEmail));
    } catch (error) {
      setActionError(errorText(error, "Could not look up this account."));
    } finally {
      setPending(null);
    }
  }

  async function handleEnroll() {
    if (
      !showEnrollment ||
      pending ||
      !match?.found ||
      !match.userId ||
      match.alreadyEnrolled
    ) {
      return;
    }

    setPending("enroll");
    setActionError("");
    setStatus("");

    try {
      await enrollExistingRegistrant(eventId, match.userId);
      setMatch(null);
      setEmail("");
      setStatus("Participant enrolled.");
      refreshRoster();
    } catch (error) {
      setActionError(errorText(error, "Could not enroll this participant."));
    } finally {
      setPending(null);
    }
  }

  async function handleCreateRegistrant(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    if (!showEnrollment || pending) return;

    const formElement = event.currentTarget;
    const form = new FormData(formElement);
    const firstName = String(form.get("firstName") ?? "").trim();
    const lastName = String(form.get("lastName") ?? "").trim();
    const newEmail = String(form.get("email") ?? "").trim();

    setActionError("");
    setStatus("");

    if (!firstName || !lastName) {
      setActionError("Enter the participant’s first and last name.");
      return;
    }

    setPending("create");

    try {
      await enrollNewRegistrant(eventId, {
        firstName,
        lastName,
        email: newEmail || null,
      });

      formElement.reset();
      setMatch(null);
      setEmail("");
      setStatus("New participant created and enrolled.");
      refreshRoster();
    } catch (error) {
      setActionError(
        errorText(error, "Could not create and enroll the participant."),
      );
    } finally {
      setPending(null);
    }
  }

  const matchName =
    match?.displayName ||
    [match?.firstName, match?.lastName].filter(Boolean).join(" ") ||
    email;

  return (
    <section className="admin-panel" aria-labelledby="event-participants-heading">
      <h2 id="event-participants-heading">Participants</h2>

      {rosterError ? (
        <p className="admin-form-error" role="alert">{rosterError}</p>
      ) : roster === null ? (
        <p role="status">Loading participants…</p>
      ) : roster.length === 0 ? (
        <p>No participants are enrolled in this event yet.</p>
      ) : (
        <ul>
          {roster.map((person) => (
            <li key={person.userId}>
              <strong>{participantName(person)}</strong>
              {" — "}
              {person.email || "No email provided"}
              {" — "}
              {person.accountStatus}
              {" "}
              <button
                className="site-button site-button-secondary"
                type="button"
                disabled={selectedParticipant !== null}
                aria-label={`Manage skills for ${participantName(person)}`}
                onClick={() => setSelectedParticipant(person)}
              >
                Manage skills
              </button>
            </li>
          ))}
        </ul>
      )}

      {selectedParticipant && (
        <ParticipantEventSkillsEditor
          key={`${eventId}:${selectedParticipant.userId}`}
          eventId={eventId}
          userId={selectedParticipant.userId}
          participantName={participantName(selectedParticipant)}
          onClose={() => setSelectedParticipant(null)}
        />
      )}

      {showEnrollment && (
        <>
          <h3>Enroll an existing participant</h3>

          <form className="admin-form" onSubmit={handleLookup}>
            <div className="admin-form-group">
              <label htmlFor="registrant-email">Participant email</label>
              <input
                id="registrant-email"
                type="email"
                value={email}
                maxLength={256}
                disabled={pending !== null}
                required
                onChange={(event) => {
                  setEmail(event.target.value);
                  setMatch(null);
                  setActionError("");
                  setStatus("");
                }}
              />
            </div>

            <div className="admin-form-actions">
              <button
                className="site-button site-button-primary"
                type="submit"
                disabled={pending !== null}
              >
                {pending === "lookup" ? "Searching…" : "Find account"}
              </button>
            </div>
          </form>

          {match && (
            <div>
              <p role="status">
                {!match.found
                  ? "No existing account found for this email."
                  : `${matchName} — ${
                      match.alreadyEnrolled ? "Already enrolled" : "Account found"
                    }`}
              </p>

              {match.found && match.userId && !match.alreadyEnrolled && (
                <button
                  className="site-button site-button-primary"
                  type="button"
                  disabled={pending !== null}
                  onClick={() => void handleEnroll()}
                >
                  {pending === "enroll" ? "Enrolling…" : "Confirm enrollment"}
                </button>
              )}
            </div>
          )}

          <h3>Create and enroll a new participant</h3>
          <p>
            If the participant already has a Knight School account, use the email
            lookup above.
          </p>

          <form className="admin-form" onSubmit={handleCreateRegistrant}>
            <div className="admin-form-group">
              <label htmlFor="new-registrant-first-name">First name</label>
              <input
                id="new-registrant-first-name"
                name="firstName"
                maxLength={50}
                disabled={pending !== null}
                required
              />
            </div>

            <div className="admin-form-group">
              <label htmlFor="new-registrant-last-name">Last name</label>
              <input
                id="new-registrant-last-name"
                name="lastName"
                maxLength={50}
                disabled={pending !== null}
                required
              />
            </div>

            <div className="admin-form-group">
              <label htmlFor="new-registrant-email">Email (optional)</label>
              <input
                id="new-registrant-email"
                name="email"
                type="email"
                maxLength={256}
                disabled={pending !== null}
              />
            </div>

            <div className="admin-form-actions">
              <button
                className="site-button site-button-primary"
                type="submit"
                disabled={pending !== null}
              >
                {pending === "create" ? "Enrolling…" : "Create and enroll"}
              </button>
            </div>
          </form>

          {actionError && (
            <p className="admin-form-error" role="alert">{actionError}</p>
          )}
          {status && (
            <p className="admin-form-status" role="status">{status}</p>
          )}
        </>
      )}
    </section>
  );
}