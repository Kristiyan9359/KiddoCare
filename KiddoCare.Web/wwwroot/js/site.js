document.addEventListener("DOMContentLoaded", () => {
    const themeToggle = document.getElementById("themeToggle");
    const themeToggleLabel = themeToggle?.querySelector(".theme-toggle-label");

    const setTheme = (theme) => {
        document.documentElement.dataset.theme = theme;
        document.documentElement.dataset.bsTheme = theme;
        localStorage.setItem("kiddocare-theme", theme);

        if (themeToggle instanceof HTMLButtonElement) {
            themeToggle.setAttribute("aria-pressed", (theme === "dark").toString());
            themeToggle.title = theme === "dark"
                ? themeToggle.dataset.switchToLightTitle || "Switch to light theme"
                : themeToggle.dataset.switchToDarkTitle || "Switch to dark theme";
        }

        if (themeToggleLabel instanceof HTMLElement) {
            themeToggleLabel.textContent = theme === "dark"
                ? themeToggle.dataset.darkLabel || "Dark"
                : themeToggle.dataset.lightLabel || "Light";
        }
    };

    if (themeToggle instanceof HTMLButtonElement) {
        const currentTheme = document.documentElement.dataset.theme === "dark" ? "dark" : "light";

        setTheme(currentTheme);

        themeToggle.addEventListener("click", () => {
            const nextTheme = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
            setTheme(nextTheme);
        });
    }

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

    document.querySelectorAll("[data-flash-alert]").forEach((alert) => {
        const dismissButton = alert.querySelector("[data-flash-dismiss]");
        const closeAlert = () => {
            alert.classList.add("is-hiding");
            window.setTimeout(() => alert.remove(), 180);
        };

        dismissButton?.addEventListener("click", closeAlert);
        window.setTimeout(closeAlert, 5200);
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
        messageElement.textContent = submitter.dataset.confirmMessage ||
            modalElement.dataset.defaultConfirmMessage ||
            "Are you sure you want to continue?";
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
