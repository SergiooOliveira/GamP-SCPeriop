// Opens a file that was downloaded with the user's login token (a plain link can't send the token)
export function openFile(bytes, contentType, fileName) {
    const url = URL.createObjectURL(new Blob([bytes], { type: contentType }));

    // PDFs open in a new tab; Word documents (or a blocked pop-up) are saved instead
    const opened = contentType === "application/pdf" ? window.open(url, "_blank") : null;
    if (!opened) {
        const link = document.createElement("a");
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        link.remove();
    }

    setTimeout(() => URL.revokeObjectURL(url), 60000);
}
