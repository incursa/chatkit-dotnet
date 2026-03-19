function normalizeLookupPath(lookupPath) {
  if (typeof lookupPath !== "string" || lookupPath.trim().length === 0) {
    throw new Error("ChatKit client tool handlers lookup path is required.");
  }

  const segments = lookupPath
    .trim()
    .split(".")
    .map((segment) => segment.trim())
    .filter((segment) => segment.length > 0);

  if (segments.length === 0) {
    throw new Error("ChatKit client tool handlers lookup path is required.");
  }

  if (segments[0] === "window" || segments[0] === "globalThis" || segments[0] === "self") {
    return segments.slice(1);
  }

  return segments;
}

export function resolveClientToolHandlers(globalScope, lookupPath) {
  // The Razor wrapper cannot serialize real JS functions, so server-side
  // configuration passes a dotted lookup path such as "window.chatkit.clientTools".
  const segments = normalizeLookupPath(lookupPath);
  let current = globalScope;

  for (const segment of segments) {
    if (current == null || typeof current !== "object" || !(segment in current)) {
      throw new Error(
        `ChatKit client tool handlers '${lookupPath}' were not found on the page.`
      );
    }

    current = current[segment];
  }

  if (current == null || typeof current !== "object" || Array.isArray(current)) {
    throw new Error(
      `ChatKit client tool handlers '${lookupPath}' must resolve to an object map.`
    );
  }

  return current;
}

export function createOnClientTool(globalScope, lookupPath) {
  // ChatKit expects a single onClientTool callback. This adapter resolves the
  // registry object at call time and dispatches to a named handler inside it.
  return async (toolCall) => {
    const handlers = resolveClientToolHandlers(globalScope, lookupPath);
    const handler = handlers[toolCall.name];

    if (typeof handler !== "function") {
      throw new Error(
        `ChatKit client tool '${toolCall.name}' is not registered in '${lookupPath}'.`
      );
    }

    return await handler(toolCall);
  };
}
