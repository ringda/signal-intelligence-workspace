window.frontstageTracking = {
    watchSections: function (options) {
        if (!options?.token) {
            return;
        }

        this.watchClicks(options);

        if (!options?.endpoint || !("IntersectionObserver" in window)) {
            return;
        }

        if (window.__frontstageSectionObserver) {
            window.__frontstageSectionObserver.disconnect();
        }

        const viewedSections = new Set();
        const sections = Array.from(document.querySelectorAll("[data-frontstage-section]"));
        const submit = function (sectionKey) {
            if (!sectionKey || viewedSections.has(sectionKey)) {
                return;
            }

            viewedSections.add(sectionKey);
            const payload = JSON.stringify({
                token: options.token,
                sectionKey,
                language: options.language || "en"
            });

            if (navigator.sendBeacon) {
                const blob = new Blob([payload], { type: "application/json" });
                navigator.sendBeacon(options.endpoint, blob);
                return;
            }

            fetch(options.endpoint, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: payload,
                keepalive: true
            }).catch(() => {});
        };

        const observer = new IntersectionObserver((entries) => {
            for (const entry of entries) {
                if (!entry.isIntersecting || entry.intersectionRatio < 0.45) {
                    continue;
                }

                const sectionKey = entry.target.getAttribute("data-frontstage-section");
                submit(sectionKey);
                observer.unobserve(entry.target);
            }
        }, {
            threshold: [0.45]
        });

        for (const section of sections) {
            observer.observe(section);
        }

        window.__frontstageSectionObserver = observer;
    },

    watchClicks: function (options) {
        if (!options?.token || !options?.clickEndpoint) {
            return;
        }

        if (window.__frontstageClickHandler) {
            document.removeEventListener("click", window.__frontstageClickHandler, true);
        }

        const trackedClicks = new Set();
        const submit = function (eventKey, target) {
            if (!eventKey) {
                return;
            }

            const clickKey = `${eventKey}:${target || ""}`;
            if (trackedClicks.has(clickKey)) {
                return;
            }

            trackedClicks.add(clickKey);
            const payload = JSON.stringify({
                token: options.token,
                eventKey,
                target: target || null,
                language: options.language || "en"
            });

            if (navigator.sendBeacon) {
                const blob = new Blob([payload], { type: "application/json" });
                navigator.sendBeacon(options.clickEndpoint, blob);
                return;
            }

            fetch(options.clickEndpoint, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: payload,
                keepalive: true
            }).catch(() => {});
        };

        const handler = function (event) {
            const element = event.target?.closest?.("[data-frontstage-click]");
            if (!element) {
                return;
            }

            submit(
                element.getAttribute("data-frontstage-click"),
                element.getAttribute("href") || element.getAttribute("aria-label") || element.textContent?.trim());
        };

        document.addEventListener("click", handler, true);
        window.__frontstageClickHandler = handler;
    }
};
