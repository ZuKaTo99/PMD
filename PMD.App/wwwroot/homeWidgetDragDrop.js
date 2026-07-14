window.pmdHomeWidgetDragDrop = (() => {
    const instances = new WeakMap();
    let activeInstance = null;

    function initialize(root, dotNetReference, options) {
        if (!root || !dotNetReference) {
            return;
        }

        dispose(root);

        const resolvedOptions = {
            itemSelector: options?.itemSelector ?? "[data-home-widget-id]",
            handleSelector: options?.handleSelector ?? "[data-home-widget-drag-handle]",
            draggingClass: options?.draggingClass ?? "is-dragging",
            dropTargetClass: options?.dropTargetClass ?? "is-drop-target"
        };

        const instance = {
            root,
            dotNetReference,
            options: resolvedOptions,
            sourceItem: null,
            targetItem: null,
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
        if (!handle || !instance.root.contains(handle)) {
            return;
        }

        const sourceItem = handle.closest(instance.options.itemSelector);
        if (!sourceItem || !instance.root.contains(sourceItem)) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();

        instance.sourceItem = sourceItem;
        instance.targetItem = sourceItem;
        instance.pointerId = event.pointerId;
        instance.scrollContainer = findScrollContainer(instance.root);
        activeInstance = instance;

        sourceItem.classList.add(instance.options.draggingClass);
        instance.root.classList.add("pmd-home-widget-drag-container-active");

        instance.previousBodyUserSelect = document.body.style.userSelect;
        instance.previousBodyCursor = document.body.style.cursor;
        document.body.style.userSelect = "none";
        document.body.style.cursor = "grabbing";
        document.body.classList.add("pmd-home-widget-drag-active");

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
        updateDropTarget(instance, event.clientX, event.clientY);
        scrollNearEdge(instance, event.clientY);
    }

    function updateDropTarget(instance, clientX, clientY) {
        const element = document.elementFromPoint(clientX, clientY);
        const targetItem = element instanceof Element
            ? element.closest(instance.options.itemSelector)
            : null;

        if (!targetItem || !instance.root.contains(targetItem)) {
            return;
        }

        if (instance.targetItem === targetItem) {
            return;
        }

        if (instance.targetItem && instance.targetItem !== instance.sourceItem) {
            instance.targetItem.classList.remove(instance.options.dropTargetClass);
        }

        instance.targetItem = targetItem;

        if (targetItem !== instance.sourceItem) {
            targetItem.classList.add(instance.options.dropTargetClass);
        }
    }

    async function finishPointerDrag(instance, event, shouldMove) {
        if (activeInstance !== instance || event.pointerId !== instance.pointerId) {
            return;
        }

        event.preventDefault();
        event.stopPropagation();

        const sourceId = instance.sourceItem?.dataset.homeWidgetId;
        const targetId = instance.targetItem?.dataset.homeWidgetId;

        cleanupActiveDrag(instance);

        if (!shouldMove || !sourceId || !targetId || sourceId === targetId) {
            return;
        }

        try {
            await instance.dotNetReference.invokeMethodAsync(
                "MoveWidgetFromJavaScript",
                sourceId,
                targetId);
        } catch (error) {
            console.error("PMD widget drag-and-drop could not update the order.", error);
        }
    }

    function cleanupActiveDrag(instance) {
        if (instance.sourceItem) {
            instance.sourceItem.classList.remove(instance.options.draggingClass);
        }

        if (instance.targetItem) {
            instance.targetItem.classList.remove(instance.options.dropTargetClass);
        }

        instance.root.classList.remove("pmd-home-widget-drag-container-active");
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
        document.body.classList.remove("pmd-home-widget-drag-active");

        instance.sourceItem = null;
        instance.targetItem = null;
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

    function createGhost(sourceItem) {
        const ghost = document.createElement("div");
        const title = sourceItem.dataset.homeWidgetTitle ?? "Widget";

        ghost.className = "pmd-home-widget-drag-ghost";
        ghost.setAttribute("aria-hidden", "true");
        ghost.innerHTML = `<span class="pmd-home-widget-drag-ghost-grip">⋮⋮</span><span>${escapeHtml(title)}</span>`;
        document.body.appendChild(ghost);

        return ghost;
    }

    function updateGhost(ghost, clientX, clientY) {
        if (!ghost) {
            return;
        }

        const offset = 14;
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
