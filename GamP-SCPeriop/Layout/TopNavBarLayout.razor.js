// Light/dark theme, remembered in a cookie for a year.
// index.html applies it before the page draws (no flash); this module keeps it working even if an old index.html is cached.
const cookie = /(?:^|;\s*)gamp-theme=(dark|light)/;

export function initTheme() {
    const match = document.cookie.match(cookie);
    const theme = match ? match[1] : "light";
    document.documentElement.setAttribute("data-bs-theme", theme);
    return theme;
}

export function setTheme(theme) {
    document.documentElement.setAttribute("data-bs-theme", theme);
    document.cookie = "gamp-theme=" + theme + "; Path=/; Max-Age=31536000; SameSite=Lax" +
        (location.protocol === "https:" ? "; Secure" : "");
}
