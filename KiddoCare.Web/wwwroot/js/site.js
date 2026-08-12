document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll("[data-remove-photo-button]").forEach((button) => {
        button.addEventListener("click", () => {
            const form = button.closest("form");
            const preview = button.closest("[data-current-photo-preview]");
            const removePhotoInput = form?.querySelector("[data-remove-photo-input]");
            const removedNote = form?.querySelector("[data-photo-removed-note]");

            if (!preview || !(removePhotoInput instanceof HTMLInputElement)) {
                return;
            }

            removePhotoInput.value = "true";
            preview.classList.add("is-removed");

            if (removedNote instanceof HTMLElement) {
                removedNote.hidden = false;
            }
        });
    });

    const modalElement = document.getElementById("confirmActionModal");
    const messageElement = document.getElementById("confirmActionModalMessage");
    const confirmButton = document.getElementById("confirmActionModalButton");

    if (!modalElement || !messageElement || !confirmButton || !window.bootstrap) {
        return;
    }

    const confirmModal = new bootstrap.Modal(modalElement);
    let pendingForm = null;
    let pendingSubmitter = null;

    document.addEventListener("submit", (event) => {
        const submitter = event.submitter;

        if (!(submitter instanceof HTMLElement) ||
            !submitter.hasAttribute("data-confirm-message")) {
            return;
        }

        const form = event.target;

        if (!(form instanceof HTMLFormElement) ||
            form.dataset.confirmed === "true") {
            form.dataset.confirmed = "false";
            return;
        }

        event.preventDefault();

        pendingForm = form;
        pendingSubmitter = submitter;
        messageElement.textContent = submitter.dataset.confirmMessage || "Are you sure you want to continue?";
        confirmModal.show();
    });

    confirmButton.addEventListener("click", () => {
        if (!pendingForm) {
            return;
        }

        pendingForm.dataset.confirmed = "true";
        confirmModal.hide();

        if (pendingSubmitter instanceof HTMLElement && pendingForm.requestSubmit) {
            pendingForm.requestSubmit(pendingSubmitter);
        } else {
            pendingForm.submit();
        }

        pendingForm = null;
        pendingSubmitter = null;
    });

    modalElement.addEventListener("hidden.bs.modal", () => {
        pendingForm = null;
        pendingSubmitter = null;
    });
});
