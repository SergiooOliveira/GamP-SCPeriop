const themeKey = "gamp-dashboard-theme";

// Remembered choice first, then the operating system preference
export function initTheme() {
    let theme = null;
    try { theme = localStorage.getItem(themeKey); } catch { }
    if (theme !== "light" && theme !== "dark") {
        theme = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    }
    document.documentElement.setAttribute("data-bs-theme", theme);
    return theme;
}

export function setTheme(theme) {
    document.documentElement.setAttribute("data-bs-theme", theme);
    try { localStorage.setItem(themeKey, theme); } catch { }
}
