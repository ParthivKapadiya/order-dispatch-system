document.addEventListener("DOMContentLoaded", () => {
    const sidebar = document.getElementById("appSidebar");
    const toggle = document.getElementById("sidebarToggle");
    const backdrop = document.getElementById("sidebarBackdrop");

    if (sidebar && toggle && backdrop) {
        const close = () => {
            sidebar.classList.remove("open");
            backdrop.classList.remove("show");
        };

        toggle.addEventListener("click", () => {
            sidebar.classList.toggle("open");
            backdrop.classList.toggle("show");
        });

        backdrop.addEventListener("click", close);
    }

    document.querySelectorAll("form[data-confirm]").forEach(form => {
        form.addEventListener("submit", event => {
            const message = form.getAttribute("data-confirm");
            if (message && !window.confirm(message)) {
                event.preventDefault();
            }
        });
    });

    const badge = document.getElementById("notificationUnreadBadge");
    if (!badge) {
        return;
    }

    const applyCount = (count) => {
        const value = Number.isFinite(count) ? count : 0;
        badge.dataset.unread = String(value);
        badge.textContent = String(value);
        badge.style.display = value > 0 ? "" : "none";
    };

    const refreshUnreadCount = async () => {
        try {
            const response = await fetch("/Notifications/UnreadCount", {
                headers: { "Accept": "application/json" },
                credentials: "same-origin"
            });
            if (!response.ok) {
                return;
            }
            const payload = await response.json();
            applyCount(payload.count);
        } catch {
            // Keep the last rendered count if the lightweight poll fails.
        }
    };

    window.setInterval(refreshUnreadCount, 45000);
});
