'use strict';

(function () {
    const editor = document.querySelector('[data-role="timeline-json"]');
    const canvas = document.querySelector('[data-role="timeline-canvas"]');
    const statusBadge = document.querySelector('[data-role="timeline-status"]');
    const updatedLabel = document.querySelector('[data-role="timeline-updated"]');
    const errorPanel = document.querySelector('[data-role="timeline-errors"]');
    const viewModeButtons = document.querySelectorAll('[data-view-mode]');
    const formatButton = document.querySelector('[data-action="format-json"]');
    const resetButton = document.querySelector('[data-action="reset-json"]');

    if (!editor || !canvas) {
        return;
    }

    const originalValue = editor.value;
    let lastValidDocument = null;

    const formatDoc = (doc) => JSON.stringify(doc, null, 2);

    function updateStatus(state, message) {
        if (!statusBadge) {
            return;
        }

        if (state) {
            statusBadge.dataset.state = state;
        } else {
            statusBadge.removeAttribute('data-state');
        }

        statusBadge.textContent = message;
    }

    function showErrors(errors) {
        if (!errorPanel) {
            return;
        }

        if (!errors || errors.length === 0) {
            errorPanel.classList.add('d-none');
            errorPanel.textContent = '';
            return;
        }

        errorPanel.classList.remove('d-none');
        errorPanel.replaceChildren(
            ...errors.map((message) => {
                const div = document.createElement('div');
                div.textContent = message;
                return div;
            })
        );
    }

    function handleInput() {
        let documentJson;

        try {
            documentJson = JSON.parse(editor.value);
        } catch (error) {
            updateStatus('error', 'JSON parse error');
            showErrors([error.message]);
            if (lastValidDocument) {
                renderTimeline(lastValidDocument);
            }
            return;
        }

        const validationErrors = validateDocument(documentJson);
        if (validationErrors.length > 0) {
            updateStatus('error', 'Validation failed');
            showErrors(validationErrors);
            if (lastValidDocument) {
                renderTimeline(lastValidDocument);
            }
            return;
        }

        const normalized = normalizeDocument(documentJson);
        lastValidDocument = normalized;
        renderTimeline(normalized);
        updateStatus('ok', 'Live');
        showErrors([]);

        if (updatedLabel) {
            const now = new Date();
            updatedLabel.textContent = new Intl.DateTimeFormat(undefined, {
                dateStyle: 'medium',
                timeStyle: 'short'
            }).format(now);
        }

        syncActiveViewButton(normalized.view.mode);
    }

    function validateDocument(doc) {
        const errors = [];

        if (!doc || typeof doc !== 'object') {
            errors.push('Document must be a JSON object.');
            return errors;
        }

        if (!doc.view) {
            errors.push('view section is required.');
        }

        const range = doc?.view?.range;
        if (!range?.start || !range?.end) {
            errors.push('view.range.start and view.range.end are required.');
        } else {
            const start = new Date(range.start);
            const end = new Date(range.end);
            if (isNaN(start.getTime()) || isNaN(end.getTime())) {
                errors.push('view.range must be valid ISO date strings.');
            } else if (end <= start) {
                errors.push('view.range.end must be after view.range.start.');
            }
        }

        if (!doc.view?.mode || !['day', 'hour'].includes(doc.view.mode)) {
            errors.push('view.mode must be "day" or "hour".');
        }

        if (!Array.isArray(doc.lanes) || doc.lanes.length === 0) {
            errors.push('lanes must contain at least one lane.');
        } else {
            const ids = new Set();
            doc.lanes.forEach((lane, index) => {
                if (!lane?.id) {
                    errors.push(`lanes[${index}] is missing id.`);
                    return;
                }
                if (ids.has(lane.id)) {
                    errors.push(`Lane id "${lane.id}" is duplicated.`);
                }
                ids.add(lane.id);
                if (!lane.label) {
                    errors.push(`Lane "${lane.id}" is missing label.`);
                }
            });
        }

        if (!Array.isArray(doc.items)) {
            errors.push('items must be an array (can be empty).');
        } else {
            const laneIds = new Set((doc.lanes || []).map((lane) => lane.id));
            doc.items.forEach((item, index) => {
                if (!item?.laneId) {
                    errors.push(`items[${index}] is missing laneId.`);
                } else if (!laneIds.has(item.laneId)) {
                    errors.push(`items[${index}] references unknown lane "${item.laneId}".`);
                }

                const start = item?.start ? new Date(item.start) : null;
                const end = item?.end ? new Date(item.end) : null;

                if (!start && !end) {
                    errors.push(`items[${index}] needs a start or end.`);
                }

                if (start && isNaN(start.getTime())) {
                    errors.push(`items[${index}].start is not a valid date.`);
                }

                if (end && isNaN(end.getTime())) {
                    errors.push(`items[${index}].end is not a valid date.`);
                }

                if (start && end && end <= start) {
                    errors.push(`items[${index}].end must be after start.`);
                }
            });
        }

        return errors;
    }

    function normalizeDocument(doc) {
        const rangeStart = new Date(doc.view.range.start);
        const rangeEnd = new Date(doc.view.range.end);
        const rangeMs = Math.max(rangeEnd - rangeStart, 1);
        const lanes = Array.isArray(doc.lanes) ? doc.lanes : [];
        const buckets = new Map();
        lanes.forEach((lane) => buckets.set(lane.id, []));

        const normalizedItems = (Array.isArray(doc.items) ? doc.items : []).map((item) => {
            const start = item.start ? new Date(item.start) : rangeStart;
            let end = item.end ? new Date(item.end) : null;

            if ((!end || isNaN(end.getTime())) && typeof item.durationDays === 'number' && !isNaN(item.durationDays)) {
                const durationMs = item.durationDays * 24 * 60 * 60 * 1000;
                end = new Date(start.getTime() + durationMs);
            }

            if (!end || isNaN(end.getTime()) || end <= start) {
                end = new Date(start.getTime() + 60 * 60 * 1000);
            }

            const offset = clamp((start - rangeStart) / rangeMs, 0, 1);
            const width = clamp((end - start) / rangeMs, 0.01, 1);
            const statusSlug = ((item.status || 'default').toLowerCase().replace(/\s+/g, '-')) || 'default';

            return {
                ...item,
                startDate: start,
                endDate: end,
                offsetPct: offset * 100,
                widthPct: Math.min(width * 100, 100 - offset * 100),
                statusClass: `status-${statusSlug}`
            };
        });

        normalizedItems.forEach((item) => {
            if (buckets.has(item.laneId)) {
                buckets.get(item.laneId).push(item);
            }
        });

        buckets.forEach((items) => {
            items.sort((a, b) => (a.startDate - b.startDate));
        });

        return {
            view: doc.view,
            lanes,
            buckets,
            range: { start: rangeStart, end: rangeEnd, span: rangeMs }
        };
    }

    function clamp(value, min, max) {
        return Math.max(min, Math.min(max, value));
    }

    function renderTimeline(doc) {
        canvas.innerHTML = '';

        if (!doc.lanes.length) {
            const empty = document.createElement('p');
            empty.className = 'empty-state';
            empty.textContent = 'Add at least one lane to render the timeline.';
            canvas.appendChild(empty);
            return;
        }

        const axis = document.createElement('div');
        axis.className = 'timeline-axis';
        buildTicks(doc).forEach((label) => {
            const span = document.createElement('span');
            span.textContent = label;
            axis.appendChild(span);
        });
        canvas.appendChild(axis);

        const lanesContainer = document.createElement('div');
        lanesContainer.className = 'timeline-lanes';

        doc.lanes.forEach((lane) => {
            const laneRow = document.createElement('div');
            laneRow.className = 'timeline-lane';

            const laneLabel = document.createElement('div');
            laneLabel.className = 'lane-label';
            laneLabel.textContent = lane.label || lane.id;
            if (lane.color) {
                laneLabel.style.color = lane.color;
            }

            const track = document.createElement('div');
            track.className = 'lane-track';

            const items = doc.buckets.get(lane.id) || [];
            items.forEach((item) => {
                const itemElement = document.createElement('div');
                const classes = ['timeline-item', 'status-default'];
                if (item.statusClass !== 'status-default') {
                    classes.push(item.statusClass);
                }
                itemElement.className = classes.join(' ');
                itemElement.style.left = `${item.offsetPct}%`;
                itemElement.style.width = `${Math.max(item.widthPct, 2)}%`;
                itemElement.title = buildTooltip(item);

                const title = document.createElement('strong');
                title.textContent = item.label || item.id;

                const meta = document.createElement('span');
                meta.textContent = formatRange(item.startDate, item.endDate, doc.view.mode);

                itemElement.appendChild(title);
                itemElement.appendChild(meta);

                track.appendChild(itemElement);
            });

            laneRow.appendChild(laneLabel);
            laneRow.appendChild(track);
            lanesContainer.appendChild(laneRow);
        });

        canvas.appendChild(lanesContainer);
    }

    function buildTicks(doc) {
        const maxTicks = 12;
        const labels = [];
        const mode = doc.view.mode === 'hour' ? 'hour' : 'day';
        const start = doc.range.start;
        const end = doc.range.end;
        const baseStep = mode === 'hour' ? 60 * 60 * 1000 : 24 * 60 * 60 * 1000;
        let step = baseStep;
        let tickCount = Math.ceil((end - start) / step);
        while (tickCount > maxTicks) {
            step *= 2;
            tickCount = Math.ceil((end - start) / step);
        }

        const formatter = new Intl.DateTimeFormat(undefined, mode === 'hour'
            ? { hour: 'numeric', minute: '2-digit' }
            : { month: 'short', day: 'numeric' });

        for (let cursor = new Date(start); cursor <= end; cursor = new Date(cursor.getTime() + step)) {
            labels.push(formatter.format(cursor));
        }

        return labels;
    }

    function formatRange(start, end, mode) {
        const options = mode === 'hour'
            ? { hour: 'numeric', minute: '2-digit' }
            : { month: 'short', day: 'numeric' };

        const formatter = new Intl.DateTimeFormat(undefined, options);
        return `${formatter.format(start)} → ${formatter.format(end)}`;
    }

    function buildTooltip(item) {
        const start = item.startDate.toLocaleString();
        const end = item.endDate.toLocaleString();
        const status = item.status || 'default';
        const description = item.description ? `\n${item.description}` : '';
        return `${item.label || item.id}\n${start} → ${end}\nStatus: ${status}${description}`;
    }

    function syncActiveViewButton(mode) {
        viewModeButtons.forEach((button) => {
            if (button.dataset.viewMode === mode) {
                button.classList.add('is-active');
            } else {
                button.classList.remove('is-active');
            }
        });
    }

    editor.addEventListener('input', handleInput);

    if (formatButton) {
        formatButton.addEventListener('click', () => {
            try {
                const doc = JSON.parse(editor.value);
                editor.value = formatDoc(doc);
                handleInput();
            } catch (error) {
                showErrors([error.message]);
            }
        });
    }

    if (resetButton) {
        resetButton.addEventListener('click', () => {
            editor.value = originalValue;
            handleInput();
        });
    }

    viewModeButtons.forEach((button) => {
        button.addEventListener('click', () => {
            const desiredMode = button.dataset.viewMode;
            try {
                const doc = JSON.parse(editor.value || '{}');
                doc.view = doc.view || {};
                doc.view.mode = desiredMode;
                editor.value = formatDoc(doc);
                handleInput();
            } catch (error) {
                showErrors([`Unable to switch mode: ${error.message}`]);
            }
        });
    });

    // Auto render once on load.
    handleInput();
})();
