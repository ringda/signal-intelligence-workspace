window.frontstageTracking = {
    watchSections: function (options) {
        if (!options?.token || !options?.endpoint || !("IntersectionObserver" in window)) {
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
    }
};
