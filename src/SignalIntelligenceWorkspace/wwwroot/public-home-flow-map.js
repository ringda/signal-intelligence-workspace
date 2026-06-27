const canvasLabels = {
    en: {
        signals: "Messy\nsignals",
        rules: "Decision\nrules",
        evidence: "Evidence\ntranslation",
        review: "Human\nreview",
        writeback: "Write-back\nloop"
    }
};

const nodes = [
    { id: "signals", label: canvasLabels.en.signals, x: 0.18, y: 0.28, color: "#e0533c" },
    { id: "rules", label: canvasLabels.en.rules, x: 0.50, y: 0.20, color: "#d97706" },
    { id: "evidence", label: canvasLabels.en.evidence, x: 0.78, y: 0.34, color: "#4f46e5" },
    { id: "review", label: canvasLabels.en.review, x: 0.63, y: 0.62, color: "#10b981" },
    { id: "writeback", label: canvasLabels.en.writeback, x: 0.30, y: 0.67, color: "#c8432e" }
];

const edges = [
    ["signals", "rules"],
    ["rules", "evidence"],
    ["evidence", "review"],
    ["review", "writeback"],
    ["writeback", "rules"],
    ["signals", "writeback"]
];

const activeMaps = new WeakMap();

function nodeById(id) {
    return nodes.find((node) => node.id === id);
}

function drawRoundedRect(ctx, x, y, w, h, r) {
    ctx.beginPath();
    ctx.moveTo(x + r, y);
    ctx.arcTo(x + w, y, x + w, y + h, r);
    ctx.arcTo(x + w, y + h, x, y + h, r);
    ctx.arcTo(x, y + h, x, y, r);
    ctx.arcTo(x, y, x + w, y, r);
    ctx.closePath();
}

function startFlowMap(canvas) {
    if (!canvas || activeMaps.has(canvas)) {
        return;
    }

    const ctx = canvas.getContext("2d");
    if (!ctx) {
        return;
    }

    const reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    let animationFrame = 0;

    function resize() {
        const rect = canvas.getBoundingClientRect();
        const ratio = window.devicePixelRatio || 1;
        canvas.width = Math.max(1, Math.floor(rect.width * ratio));
        canvas.height = Math.max(1, Math.floor(rect.height * ratio));
        ctx.setTransform(ratio, 0, 0, ratio, 0, 0);
    }

    function draw(timestamp) {
        const rect = canvas.getBoundingClientRect();
        const width = rect.width;
        const height = rect.height;

        if (width <= 0 || height <= 0) {
            return;
        }

        ctx.clearRect(0, 0, width, height);

        ctx.fillStyle = "#fcfaf7";
        ctx.fillRect(0, 0, width, height);

        ctx.strokeStyle = "rgba(31, 29, 26, 0.07)";
        ctx.lineWidth = 1;
        for (let x = 34; x < width; x += 56) {
            ctx.beginPath();
            ctx.moveTo(x, 0);
            ctx.lineTo(x, height);
            ctx.stroke();
        }
        for (let y = 34; y < height; y += 56) {
            ctx.beginPath();
            ctx.moveTo(0, y);
            ctx.lineTo(width, y);
            ctx.stroke();
        }

        const nodeW = Math.min(150, width * 0.24);
        const nodeH = Math.min(76, height * 0.14);

        edges.forEach(([fromId, toId], index) => {
            const from = nodeById(fromId);
            const to = nodeById(toId);
            const x1 = from.x * width;
            const y1 = from.y * height;
            const x2 = to.x * width;
            const y2 = to.y * height;
            const isFeedback = fromId === "writeback" && toId === "rules";
            const cx = (x1 + x2) / 2;
            const cy = (y1 + y2) / 2 - height * 0.055;

            ctx.strokeStyle = isFeedback ? "rgba(224, 83, 60, 0.55)" : "rgba(31, 29, 26, 0.24)";
            ctx.lineWidth = 2;
            ctx.setLineDash(isFeedback ? [6, 5] : []);
            ctx.beginPath();
            ctx.moveTo(x1, y1);
            ctx.quadraticCurveTo(cx, cy, x2, y2);
            ctx.stroke();
            ctx.setLineDash([]);

            const dirx = x2 - cx;
            const diry = y2 - cy;
            const angle = Math.atan2(diry, dirx);
            const headLen = Math.max(7, Math.min(11, width * 0.018));
            const back = nodeH * 0.42;
            const hx = x2 - Math.cos(angle) * back;
            const hy = y2 - Math.sin(angle) * back;

            ctx.fillStyle = isFeedback ? "rgba(224, 83, 60, 0.7)" : "rgba(31, 29, 26, 0.34)";
            ctx.beginPath();
            ctx.moveTo(hx, hy);
            ctx.lineTo(hx - Math.cos(angle - 0.42) * headLen, hy - Math.sin(angle - 0.42) * headLen);
            ctx.lineTo(hx - Math.cos(angle + 0.42) * headLen, hy - Math.sin(angle + 0.42) * headLen);
            ctx.closePath();
            ctx.fill();

            if (!reduceMotion) {
                const phase = ((timestamp / 1800) + index * 0.16) % 1;
                const ax = (1 - phase) * (1 - phase) * x1 + 2 * (1 - phase) * phase * cx + phase * phase * x2;
                const ay = (1 - phase) * (1 - phase) * y1 + 2 * (1 - phase) * phase * cy + phase * phase * y2;
                ctx.fillStyle = isFeedback ? "#e0533c" : (index % 2 === 0 ? "#d97706" : "#4f46e5");
                ctx.beginPath();
                ctx.arc(ax, ay, 4.5, 0, Math.PI * 2);
                ctx.fill();
            }
        });

        nodes.forEach((node) => {
            const x = node.x * width - nodeW / 2;
            const y = node.y * height - nodeH / 2;

            ctx.shadowColor = "rgba(31, 29, 26, 0.14)";
            ctx.shadowBlur = 18;
            ctx.shadowOffsetY = 8;
            ctx.fillStyle = "#ffffff";
            drawRoundedRect(ctx, x, y, nodeW, nodeH, 8);
            ctx.fill();
            ctx.shadowColor = "transparent";
            ctx.strokeStyle = "rgba(31, 29, 26, 0.16)";
            ctx.stroke();

            const cxNode = node.x * width;
            ctx.fillStyle = node.color;
            ctx.beginPath();
            ctx.arc(cxNode, y + 13, 5, 0, Math.PI * 2);
            ctx.fill();

            const fontSize = Math.max(11, Math.min(15, width * 0.022));
            ctx.fillStyle = "#12110f";
            ctx.font = `700 ${fontSize}px Inter, system-ui, sans-serif`;
            ctx.textAlign = "center";
            ctx.textBaseline = "middle";

            const lines = node.label.split("\n");
            const lineGap = fontSize * 1.18;
            const baseY = y + nodeH / 2 + 6;
            lines.forEach((line, lineIndex) => {
                ctx.fillText(line, cxNode, baseY + (lineIndex - (lines.length - 1) / 2) * lineGap);
            });
        });

        if (!reduceMotion) {
            animationFrame = requestAnimationFrame(draw);
        }
    }

    function handleResize() {
        resize();
        if (reduceMotion) {
            draw(0);
        }
    }

    activeMaps.set(canvas, {
        stop: () => {
            cancelAnimationFrame(animationFrame);
            window.removeEventListener("resize", handleResize);
            activeMaps.delete(canvas);
        }
    });

    resize();
    window.addEventListener("resize", handleResize);
    animationFrame = requestAnimationFrame(draw);
}

function initFlowMaps() {
    const canvases = document.querySelectorAll("#flowCanvas");
    canvases.forEach(startFlowMap);
    return canvases.length;
}

function scheduleFlowMapInit(attempt = 0) {
    const count = initFlowMaps();
    if (count > 0 || attempt >= 20) {
        return;
    }

    window.setTimeout(() => scheduleFlowMapInit(attempt + 1), 100);
}

scheduleFlowMapInit();
document.addEventListener("DOMContentLoaded", () => scheduleFlowMapInit());
document.addEventListener("enhancedload", () => scheduleFlowMapInit());

const observer = new MutationObserver(() => scheduleFlowMapInit());
observer.observe(document.body, { childList: true, subtree: true });
