// Paste and drag-over-card file capture.
// dotnetRef must implement ReceiveFile(name, type, base64data).

window.attachmentInterop = {
    clickById(id) {
        document.getElementById(id)?.click();
    },

    registerPaste(dotnetRef) {
        document.addEventListener('paste', (e) => {
            const items = e.clipboardData?.items ?? [];
            for (const item of items) {
                if (item.kind !== 'file') continue;
                const file = item.getAsFile();
                if (!file) continue;
                readAndSend(dotnetRef, file);
            }
        });
    },

    registerDropZone(element, dotnetRef) {
        element.addEventListener('dragover', (e) => {
            e.preventDefault();
            e.stopPropagation();
        });
        element.addEventListener('drop', (e) => {
            e.preventDefault();
            e.stopPropagation();
            for (const file of e.dataTransfer?.files ?? []) {
                readAndSend(dotnetRef, file);
            }
        });
    }
};

function readAndSend(dotnetRef, file) {
    const reader = new FileReader();
    reader.onload = () => {
        const base64 = reader.result.split(',')[1];
        dotnetRef.invokeMethodAsync('ReceiveFilePaste', file.name, file.type, base64)
            .catch(err => console.warn('interop error:', err));
    };
    reader.readAsDataURL(file);
}

// Scroll a given element into view smoothly
window.scrollIntoView = (el) => el?.scrollIntoView({ behavior: 'smooth', block: 'nearest' });

window.downloadFileWithAuth = async function(url, token, filename) {
    try {
        const response = await fetch(url, {
            headers: { 'Authorization': 'Bearer ' + token }
        });
        if (!response.ok) { console.error('Download failed:', response.status); return; }
        const blob = await response.blob();
        const blobUrl = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = blobUrl;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        URL.revokeObjectURL(blobUrl);
    } catch (e) { console.error('Download error:', e); }
};
