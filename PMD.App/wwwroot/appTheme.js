(() => {
    const storageKey = "pmd-app-theme";

    function normalizeTheme(theme) {
        return theme === "dark" ? "dark" : "light";
    }

    function applyTheme(theme) {
        const normalizedTheme = normalizeTheme(theme);
        const root = document.documentElement;

        root.setAttribute("data-bs-theme", normalizedTheme);
        root.style.colorScheme = normalizedTheme;

        if (document.body) {
            document.body.setAttribute("data-bs-theme", normalizedTheme);
        }

        try {
            window.localStorage.setItem(storageKey, normalizedTheme);
        } catch {
            // Local storage is only a visual startup aid.
        }
    }

    let startupTheme = "light";

    try {
        startupTheme = normalizeTheme(
            window.localStorage.getItem(storageKey));
    } catch {
        startupTheme = "light";
    }

    applyTheme(startupTheme);

    window.pmdTheme = {
        apply: applyTheme
    };
})();
