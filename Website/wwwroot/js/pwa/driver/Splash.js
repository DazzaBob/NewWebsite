(function () {
    "use strict";

    var msg = document.getElementById("driver-boot-message");
    var errorEl = document.getElementById("driver-boot-error");
    var exitBtn = document.getElementById("driver-boot-exit");

    function showError(text) {
        if (msg) {
            msg.textContent = "Unable to start the driver app.";
        }
        if (errorEl) {
            errorEl.textContent = text || "Location is required to use this app.";
            errorEl.style.display = "block";
        }
        if (exitBtn) {
            exitBtn.style.display = "inline-block";
        }
    }

    function hardExit() {
        window.location.href = "/Account/Logout?returnUrl=/";
    }

    if (exitBtn) {
        exitBtn.addEventListener("click", hardExit);
    }

    function proceedToApp(lat, lng) {
        // If you want to, later you can send lat/lng to the server here first.
        window.location.href = "/pwa/driver";
    }

    function startBoot() {
        if (!navigator.geolocation) {
            showError("This device does not support location services, which are required to use the driver app.");
            return;
        }

        if (msg) {
            msg.textContent = "Checking your location…";
        }

        navigator.geolocation.getCurrentPosition(
            function (pos) {
                var lat = pos.coords.latitude;
                var lng = pos.coords.longitude;

                // Persist for the driver map
                try {
                    if (window.localStorage) {
                        localStorage.setItem("driverBootLat", String(lat));
                        localStorage.setItem("driverBootLng", String(lng));
                    }
                } catch (e) {
                    // ignore storage failures
                }

                proceedToApp(lat, lng);
            },
            function (err) {
                var reason;

                if (err && typeof err.code === "number") {
                    if (err.code === err.PERMISSION_DENIED) {
                        reason = "Location access was denied. Please enable location for this site in your browser settings.";
                    } else if (err.code === err.POSITION_UNAVAILABLE) {
                        reason = "Location information is unavailable on this device.";
                    } else if (err.code === err.TIMEOUT) {
                        reason = "Timed out while trying to get your location.";
                    }
                }

                showError(reason || "Location is required to use this app.");
            },
            {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 0
            }
        );
    }

    startBoot();
})();