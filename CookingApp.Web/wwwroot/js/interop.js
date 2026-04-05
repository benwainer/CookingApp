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
