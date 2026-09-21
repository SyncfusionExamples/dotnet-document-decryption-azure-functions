(function () {
    "use strict";

    // The server-side function is registered with Route = "DecryptDocument".
    const DECRYPT_URL = `${window.location.origin}/api/DecryptDocument`;
    const MAX_BYTES = 25 * 1024 * 1024; // 25 MB

    const ALLOWED_EXTENSIONS = [".pdf", ".xlsx", ".xls"];

    const form = document.getElementById("decrypt-form");
    const fileInput = document.getElementById("document-file");
    const uploadBtn = document.getElementById("upload-btn");
    const uploadStatus = document.getElementById("upload-status");
    const uploadStatusText = document.getElementById("upload-status-text");
    const dropzone = document.getElementById("dropzone");
    const passwordInput = document.getElementById("password");
    const toggleEye = document.getElementById("toggle-eye");
    const submitBtn = document.getElementById("submit-btn");
    const btnLabel = submitBtn.querySelector(".btn-label");
    const spinner = submitBtn.querySelector(".spinner");
    const statusEl = document.getElementById("status");

    let currentFile = null;

    // ---------- Upload button ----------
    uploadBtn.addEventListener("click", (e) => {
        e.preventDefault();
        fileInput.click();
    });

    fileInput.addEventListener("change", () => {
        const file = fileInput.files?.[0];
        if (file) setFile(file);
    });

    // ---------- Drag & drop ----------
    ["dragenter", "dragover"].forEach((evt) =>
        dropzone.addEventListener(evt, (e) => {
            e.preventDefault();
            dropzone.classList.add("is-dragover");
        })
    );
    ["dragleave", "drop"].forEach((evt) =>
        dropzone.addEventListener(evt, (e) => {
            e.preventDefault();
            dropzone.classList.remove("is-dragover");
        })
    );
    dropzone.addEventListener("drop", (e) => {
        const file = e.dataTransfer?.files?.[0];
        if (file) setFile(file);
    });

    function setFile(file) {
        const ext = getExtension(file.name);
        if (!ALLOWED_EXTENSIONS.includes(ext)) {
            showStatus("error", "Only PDF and Excel (.xlsx, .xls) files are supported.");
            clearFile();
            return;
        }
        if (file.size > MAX_BYTES) {
            showStatus("error", "File is too large. Maximum size is 25 MB.");
            clearFile();
            return;
        }
        currentFile = file;
        uploadStatusText.textContent = `${file.name}  ·  ${formatBytes(file.size)}`;
        uploadStatus.classList.add("has-file");
        hideStatus();
    }

    function clearFile() {
        currentFile = null;
        fileInput.value = "";
        uploadStatusText.textContent = "No file selected";
        uploadStatus.classList.remove("has-file");
    }

    // ---------- Password show/hide ----------
    toggleEye.addEventListener("click", (e) => {
        e.preventDefault();
        const showing = passwordInput.type === "text";
        passwordInput.type = showing ? "password" : "text";
        toggleEye.setAttribute(
            "aria-label",
            showing ? "Show password" : "Hide password"
        );
    });

    // ---------- Form submit ----------
    form.addEventListener("submit", async (e) => {
        e.preventDefault();
        await submitDecrypt();
    });

    async function submitDecrypt() {
        if (!currentFile) {
            showStatus("error", "Please choose a PDF or Excel file to decrypt.");
            return;
        }

        const password = passwordInput.value;
        const ext = getExtension(currentFile.name);
        const isExcel = ext === ".xlsx" || ext === ".xls";

        setLoading(true);
        showStatus(
            "info",
            `Decrypting your ${isExcel ? "Excel" : "PDF"} file, please wait...`
        );

        try {
            const fd = new FormData();
            fd.append("file", currentFile, currentFile.name);
            if (password) fd.append("password", password);

            const response = await fetch(DECRYPT_URL, {
                method: "POST",
                body: fd
            });

            if (!response.ok) {
                let msg = `Decryption failed (HTTP ${response.status}).`;
                try {
                    const data = await response.json();
                    if (data?.error) msg = data.error;
                } catch (_) { /* ignore */ }
                showStatus("error", msg);
                return;
            }

            const blob = await response.blob();
            if (blob.size === 0) {
                showStatus("error", "The server returned an empty file.");
                return;
            }

            const downloadName =
                parseFileName(response.headers.get("Content-Disposition")) ||
                defaultOutputName(currentFile.name);

            triggerDownload(blob, downloadName);
            showStatus(
                "success",
                `Success! Your decrypted file (${formatBytes(blob.size)}) has been downloaded as "${downloadName}".`
            );
        } catch (err) {
            showStatus("error", `Network error: ${err.message || err}`);
        } finally {
            setLoading(false);
        }
    }

    // ---------- Helpers ----------
    function setLoading(loading) {
        submitBtn.disabled = loading;
        btnLabel.textContent = loading ? "Decrypting..." : "Decrypt Document";
        spinner.hidden = !loading;
    }

    function showStatus(kind, message) {
        statusEl.className = `status ${kind}`;
        statusEl.textContent = message;
        statusEl.hidden = false;
    }

    function hideStatus() {
        statusEl.hidden = true;
        statusEl.textContent = "";
    }

    function getExtension(name) {
        const idx = name.lastIndexOf(".");
        return idx >= 0 ? name.substring(idx).toLowerCase() : "";
    }

    function defaultOutputName(name) {
        const ext = getExtension(name);
        const outExt = ext === ".xls" ? ".xlsx" : ext;
        return `${baseName(name)}-Decrypted${outExt}`;
    }

    function formatBytes(bytes) {
        if (bytes < 1024) return `${bytes} B`;
        if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
        return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
    }

    function parseFileName(header) {
        if (!header) return null;
        const utf8Match = header.match(/filename\*=UTF-8''([^;]+)/i);
        if (utf8Match) return decodeURIComponent(utf8Match[1]);
        const quoted = header.match(/filename="?([^";]+)"?/i);
        return quoted ? quoted[1].trim() : null;
    }

    function baseName(name) {
        const idx = name.lastIndexOf(".");
        return idx > 0 ? name.substring(0, idx) : name;
    }

    function triggerDownload(blob, fileName) {
        const url = URL.createObjectURL(blob);
        const a = document.createElement("a");
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        a.remove();
        setTimeout(() => URL.revokeObjectURL(url), 1500);
    }
})();
