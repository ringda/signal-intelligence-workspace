window.publicFeedback = {
    submit: async function (endpoint, payload) {
        const response = await fetch(endpoint, {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(payload)
        });

        let body = null;
        try {
            body = await response.json();
        } catch {
            body = null;
        }

        if (!response.ok) {
            throw new Error(body?.message || "The feedback could not be saved. Please try again.");
        }

        return body;
    }
};
