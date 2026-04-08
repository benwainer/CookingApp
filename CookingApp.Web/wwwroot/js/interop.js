window.getLocation = () => new Promise((resolve, reject) => {
    if (!navigator.geolocation) {
        reject('Geolocation not supported');
        return;
    }
    navigator.geolocation.getCurrentPosition(
        pos => resolve([pos.coords.latitude, pos.coords.longitude]),
        err => reject(err.message),
        { timeout: 8000 }
    );
});

window.copyToClipboard = (text) => {
    if (navigator.clipboard) {
        return navigator.clipboard.writeText(text);
    }
    // Fallback for older browsers
    const el = document.createElement('textarea');
    el.value = text;
    el.style.position = 'fixed';
    el.style.opacity = '0';
    document.body.appendChild(el);
    el.focus();
    el.select();
    document.execCommand('copy');
    document.body.removeChild(el);
    return Promise.resolve();
};

window.historyBack = (fallback) => {
    if (window.history.length > 1) {
        window.history.back();
    } else {
        window.location.href = fallback || '/browse';
    }
};
