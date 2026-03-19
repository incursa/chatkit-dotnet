function normalizeLookupPath(lookupPath) {
  if (typeof lookupPath !== "string" || lookupPath.trim().length === 0) {
    throw new Error("ChatKit widget action handler lookup path is required.");
  }

  const segments = lookupPath
    .trim()
    .split(".")
    .map((segment) => segment.trim())
    .filter((segment) => segment.length > 0);

  if (segments.length === 0) {
    throw new Error("ChatKit widget action handler lookup path is required.");
  }

  if (segments[0] === "window" || segments[0] === "globalThis" || segments[0] === "self") {
    return segments.slice(1);
  }

  return segments;
}

export function resolveWidgetActionHandler(globalScope, lookupPath) {
  const segments = normalizeLookupPath(lookupPath);
  let current = globalScope;

  for (const segment of segments) {
    if (current == null || typeof current !== "object" || !(segment in current)) {
      throw new Error(
        `ChatKit widget action handler '${lookupPath}' was not found on the page.`
      );
    }

    current = current[segment];
  }

  if (typeof current !== "function") {
    throw new Error(
      `ChatKit widget action handler '${lookupPath}' must resolve to a function.`
    );
  }

  return current;
}

export function composeWidgetActionHandlers(clientHandler, forwardHandler) {
  // Hosted mode can run client-side handling, server forwarding, or both.
  // The client handler runs first so it can veto forwarding by throwing.
  if (typeof clientHandler !== "function" && typeof forwardHandler !== "function") {
    return undefined;
  }

  return async (action, widgetItem) => {
    if (typeof clientHandler === "function") {
      await clientHandler(action, widgetItem);
    }

    if (typeof forwardHandler === "function") {
      await forwardHandler(action, widgetItem);
    }
  };
}

export function createOnWidgetAction(globalScope, lookupPath) {
  // Keep the resolved handler dynamic so late page scripts can still register
  // or replace the callback after the Razor host is rendered.
  return async (action, widgetItem) => {
    const handler = resolveWidgetActionHandler(globalScope, lookupPath);
    await handler(action, widgetItem);
  };
}
