import { useRef, useState } from "react";

import "./DeleteProfileSection.css";

function DeleteProfileSection() {
  const dialogRef = useRef<HTMLDialogElement>(null);

  const [confirmation, setConfirmation] = useState("");
  const [statusMessage, setStatusMessage] = useState("");

  function openDialog() {
    setConfirmation("");
    setStatusMessage("");
    dialogRef.current?.showModal();
  }

  function closeDialog() {
    dialogRef.current?.close();
  }

  function handleDelete() {
    setStatusMessage(
      "No account was deleted because the profile-deletion endpoint is not connected yet.",
    );
  }

  return (
    <section
      className="delete-profile-section"
      aria-labelledby="delete-profile-heading"
    >
      <div>
        <h2 id="delete-profile-heading">Delete profile</h2>

        <p>
          Permanently remove your Knight School account and its associated
          information.
        </p>
      </div>

      <button
        className="delete-profile-open"
        type="button"
        onClick={openDialog}
      >
        Delete my profile
      </button>

      <dialog
        className="delete-profile-dialog"
        ref={dialogRef}
        aria-labelledby="delete-dialog-title"
      >
        <div className="delete-dialog-content">
          <p className="delete-dialog-eyebrow">
            Permanent account deletion
          </p>

          <h2 id="delete-dialog-title">Delete your profile?</h2>

          <p>
            This action will permanently remove your account and associated
            information. This cannot be undone.
          </p>

          <div className="delete-confirmation-field">
            <label htmlFor="deleteConfirmation">
              Type <strong>DELETE</strong> to confirm
            </label>

            <input
              id="deleteConfirmation"
              type="text"
              autoComplete="off"
              value={confirmation}
              onChange={(event) => {
                setConfirmation(event.target.value);
                setStatusMessage("");
              }}
            />
          </div>

          {statusMessage && (
            <p className="delete-profile-status" role="status">
              {statusMessage}
            </p>
          )}

          <div className="delete-dialog-actions">
            <button
              className="delete-dialog-cancel"
              type="button"
              onClick={closeDialog}
            >
              Cancel
            </button>

            <button
              className="delete-dialog-confirm"
              type="button"
              disabled={confirmation !== "DELETE"}
              onClick={handleDelete}
            >
              Permanently delete profile
            </button>
          </div>
        </div>
      </dialog>
    </section>
  );
}

export default DeleteProfileSection;