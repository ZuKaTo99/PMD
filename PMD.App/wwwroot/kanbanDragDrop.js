window.pmdKanbanDragDrop = (() => {
    const instances = new WeakMap();
    let activeInstance = null;

    function initialize(root, dotNetReference, options) {
        if (!root || !dotNetReference) {
            return;
        }

        dispose(root);

        const resolvedOptions = {
            columnSelector: options?.columnSelector ?? "[data-kanban-status]",
            listSelector: options?.listSelector ?? ".kanban-task-list",
            itemSelector: options?.itemSelector ?? "[data-kanban-task-id]",
            handleSelector: options?.handleSelector ?? ".kanban-task-drag-handle",
            draggingClass: options?.draggingClass ?? "is-dragging",
            targetColumnClass: options?.targetColumnClass ?? "is-drop-target"
        };

        const instance = {
            root,
            dotNetReference,
            options: resolvedOptions,
            sourceItem: null,
            targetColumn: null,
            placeholder: null,
            pointerId: null,
            ghost: null,
            scrollContainer: null,
            previousBodyUserSelect: "",
            previousBodyCursor: "",
            onPointerDown: null,
            onPointerMove: null,
            onPointerUp: null,
            onPointerCancel: null
        };

        instance.onPointerDown = event => beginPointerDrag(instance, event);
        root.addEventListener("pointerdown", instance.onPointerDown);
        instances.set(root, instance);
    }

    function beginPointerDrag(instance, event) {
        if (activeInstance || event.isPrimary === false) {
            return;
        }

        if (event.pointerType === "mouse" && event.button !== 0) {
            return;
        }

        const eventTarget = event.target instanceof Element
            ? event.target
            : null;

        const handle = eventTarget?.closest(instance.options.handleSelector);
        if (!handle ||
            !instance.root.contains(handle) ||
            handle.matches(":disabled") ||
            handle.getAttribute("aria-disabled") === "true") {
            return;
        }

        const sourceItem = handle.closest(instance.options.itemSelector);
        const sourceColumn = sourceItem?.closest(instance.options.columnSelector);
        const sourceList = sourceColumn?.querySelector(instance.options.listSelector);

        if (!sourceItem || !sourceColumn || !sourceList) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();

        instance.sourceItem = sourceItem;
        instance.targetColumn = sourceColumn;
        instance.pointerId = event.pointerId;
        instance.scrollContainer = findScrollContainer(instance.root);
        instance.placeholder = createPlaceholder(sourceItem);
        activeInstance = instance;

        sourceItem.insertAdjacentElement("afterend", instance.placeholder);
        sourceItem.classList.add(instance.options.draggingClass);
        sourceColumn.classList.add(instance.options.targetColumnClass);

        instance.previousBodyUserSelect = document.body.style.userSelect;
        instance.previousBodyCursor = document.body.style.cursor;
        document.body.style.userSelect = "none";
        document.body.style.cursor = "grabbing";
        document.body.classList.add("pmd-kanban-drag-active");

        instance.ghost = createGhost(sourceItem);
        updateGhost(instance.ghost, event.clientX, event.clientY);

        try {
            handle.setPointerCapture?.(event.pointerId);
        } catch {
            // Pointer capture is optional. Window listeners remain the fallback.
        }

        instance.onPointerMove = moveEvent => movePointerDrag(instance, moveEvent);
        instance.onPointerUp = upEvent => finishPointerDrag(instance, upEvent, true);
        instance.onPointerCancel = cancelEvent => finishPointerDrag(instance, cancelEvent, false);

        window.addEventListener("pointermove", instance.onPointerMove, { passive: false });
        window.addEventListener("pointerup", instance.onPointerUp, { passive: false });
        window.addEventListener("pointercancel", instance.onPointerCancel, { passive: false });
    }

    function movePointerDrag(instance, event) {
        if (activeInstance !== instance || event.pointerId !== instance.pointerId) {
            return;
        }

        event.preventDefault();
        updateGhost(instance.ghost, event.clientX, event.clientY);
        updateDropPosition(instance, event.clientX, event.clientY);
        scrollNearEdge(instance, event.clientY);
    }

    function updateDropPosition(instance, clientX, clientY) {
        const element = document.elementFromPoint(clientX, clientY);
        const targetColumn = element instanceof Element
            ? element.closest(instance.options.columnSelector)
            : null;

        if (!targetColumn || !instance.root.contains(targetColumn)) {
            return;
        }

        const targetList = targetColumn.querySelector(instance.options.listSelector);
        if (!targetList || !instance.placeholder) {
            return;
        }

        if (instance.targetColumn !== targetColumn) {
            instance.targetColumn?.classList.remove(
                instance.options.targetColumnClass);

            instance.targetColumn = targetColumn;
            targetColumn.classList.add(instance.options.targetColumnClass);
        }

        const cards = Array.from(
            targetList.querySelectorAll(instance.options.itemSelector))
            .filter(card => card !== instance.sourceItem);

        const cardBeforePointer = cards.find(card => {
            const rect = card.getBoundingClientRect();
            return clientY < rect.top + (rect.height / 2);
        });

        if (cardBeforePointer) {
            targetList.insertBefore(instance.placeholder, cardBeforePointer);
        } else {
            targetList.appendChild(instance.placeholder);
        }
    }

    async function finishPointerDrag(instance, event, shouldMove) {
        if (activeInstance !== instance || event.pointerId !== instance.pointerId) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();

        const taskId = instance.sourceItem?.dataset.kanbanTaskId;
        const statusValue = instance.targetColumn?.dataset.kanbanStatus;
        const targetIndex = getPlaceholderIndex(instance);

        cleanupActiveDrag(instance);

        if (!shouldMove || !taskId || statusValue === undefined || targetIndex < 0) {
            return;
        }

        const targetStatus = Number.parseInt(statusValue, 10);
        if (!Number.isInteger(targetStatus)) {
            return;
        }

        try {
            await instance.dotNetReference.invokeMethodAsync(
                "MoveTaskFromJavaScript",
                taskId,
                targetStatus,
                targetIndex);
        } catch (error) {
            console.error("PMD Kanban drag-and-drop could not move the task.", error);
        }
    }

    function getPlaceholderIndex(instance) {
        const placeholder = instance.placeholder;
        const parent = placeholder?.parentElement;

        if (!placeholder || !parent) {
            return -1;
        }

        let index = 0;

        for (const child of parent.children) {
            if (child === placeholder) {
                return index;
            }

            if (child === instance.sourceItem) {
                continue;
            }

            if (child.matches?.(instance.options.itemSelector)) {
                index++;
            }
        }

        return -1;
    }

    function cleanupActiveDrag(instance) {
        instance.sourceItem?.classList.remove(instance.options.draggingClass);
        instance.targetColumn?.classList.remove(instance.options.targetColumnClass);
        instance.placeholder?.remove();
        instance.ghost?.remove();

        if (instance.onPointerMove) {
            window.removeEventListener("pointermove", instance.onPointerMove);
        }

        if (instance.onPointerUp) {
            window.removeEventListener("pointerup", instance.onPointerUp);
        }

        if (instance.onPointerCancel) {
            window.removeEventListener("pointercancel", instance.onPointerCancel);
        }

        document.body.style.userSelect = instance.previousBodyUserSelect;
        document.body.style.cursor = instance.previousBodyCursor;
        document.body.classList.remove("pmd-kanban-drag-active");

        instance.sourceItem = null;
        instance.targetColumn = null;
        instance.placeholder = null;
        instance.pointerId = null;
        instance.ghost = null;
        instance.scrollContainer = null;
        instance.onPointerMove = null;
        instance.onPointerUp = null;
        instance.onPointerCancel = null;

        if (activeInstance === instance) {
            activeInstance = null;
        }
    }

    function createPlaceholder(sourceItem) {
        const placeholder = document.createElement("div");
        const sourceHeight = sourceItem.getBoundingClientRect().height;

        placeholder.className = "kanban-drop-placeholder";
        placeholder.setAttribute("aria-hidden", "true");
        placeholder.style.minHeight = `${Math.max(72, sourceHeight)}px`;

        return placeholder;
    }

    function createGhost(sourceItem) {
        const ghost = document.createElement("div");
        const title = sourceItem.dataset.kanbanTaskTitle ?? "Aufgabe";

        ghost.className = "pmd-kanban-drag-ghost";
        ghost.setAttribute("aria-hidden", "true");
        ghost.innerHTML = `<strong>${escapeHtml(title)}</strong><span>Status und Reihenfolge werden gespeichert</span>`;
        document.body.appendChild(ghost);

        return ghost;
    }

    function updateGhost(ghost, clientX, clientY) {
        if (!ghost) {
            return;
        }

        const offset = 15;
        const maxLeft = Math.max(8, window.innerWidth - ghost.offsetWidth - 8);
        const maxTop = Math.max(8, window.innerHeight - ghost.offsetHeight - 8);

        ghost.style.left = `${Math.min(clientX + offset, maxLeft)}px`;
        ghost.style.top = `${Math.min(clientY + offset, maxTop)}px`;
    }

    function findScrollContainer(element) {
        let current = element.parentElement;

        while (current) {
            const style = window.getComputedStyle(current);
            const overflowY = style.overflowY;

            if ((overflowY === "auto" || overflowY === "scroll") &&
                current.scrollHeight > current.clientHeight) {
                return current;
            }

            current = current.parentElement;
        }

        return document.scrollingElement ?? document.documentElement;
    }

    function scrollNearEdge(instance, clientY) {
        const container = instance.scrollContainer;
        if (!container) {
            return;
        }

        const threshold = 72;
        const speed = 18;
        const isDocument = container === document.scrollingElement ||
            container === document.documentElement ||
            container === document.body;

        const top = isDocument ? 0 : container.getBoundingClientRect().top;
        const bottom = isDocument
            ? window.innerHeight
            : container.getBoundingClientRect().bottom;

        if (clientY < top + threshold) {
            container.scrollTop -= speed;
        } else if (clientY > bottom - threshold) {
            container.scrollTop += speed;
        }
    }

    function dispose(root) {
        const instance = root ? instances.get(root) : null;
        if (!instance) {
            return;
        }

        if (activeInstance === instance) {
            cleanupActiveDrag(instance);
        }

        if (instance.onPointerDown) {
            root.removeEventListener("pointerdown", instance.onPointerDown);
        }

        instances.delete(root);
    }

    function escapeHtml(value) {
        return String(value)
            .replaceAll("&", "&amp;")
            .replaceAll("<", "&lt;")
            .replaceAll(">", "&gt;")
            .replaceAll('"', "&quot;")
            .replaceAll("'", "&#039;");
    }

    return {
        initialize,
        dispose
    };
})();
