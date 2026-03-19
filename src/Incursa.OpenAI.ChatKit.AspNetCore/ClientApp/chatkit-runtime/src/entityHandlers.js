function normalizeLookupPath(lookupPath) {
  if (typeof lookupPath !== "string" || lookupPath.trim().length === 0) {
    throw new Error("ChatKit entity handlers lookup path is required.");
  }

  const segments = lookupPath
    .trim()
    .split(".")
    .map((segment) => segment.trim())
    .filter((segment) => segment.length > 0);

  if (segments.length === 0) {
    throw new Error("ChatKit entity handlers lookup path is required.");
  }

  if (segments[0] === "window" || segments[0] === "globalThis" || segments[0] === "self") {
    return segments.slice(1);
  }

  return segments;
}

function normalizeEntityData(data, context) {
  if (data == null) {
    return undefined;
  }

  if (typeof data !== "object" || Array.isArray(data)) {
    throw new Error(`${context} field 'data' must be an object map of string values.`);
  }

  const normalized = {};

  for (const [key, value] of Object.entries(data)) {
    if (typeof value !== "string") {
      throw new Error(`${context} field 'data.${key}' must be a string.`);
    }

    normalized[key] = value;
  }

  return normalized;
}

export function validateEntity(entity, context = "ChatKit entity") {
  if (entity == null || typeof entity !== "object" || Array.isArray(entity)) {
    throw new Error(`${context} must be an object.`);
  }

  if (typeof entity.id !== "string" || entity.id.trim().length === 0) {
    throw new Error(`${context} must include a non-empty string 'id'.`);
  }

  if (typeof entity.title !== "string" || entity.title.trim().length === 0) {
    throw new Error(`${context} must include a non-empty string 'title'.`);
  }

  if (entity.icon != null && typeof entity.icon !== "string") {
    throw new Error(`${context} field 'icon' must be a string when provided.`);
  }

  if (entity.interactive != null && typeof entity.interactive !== "boolean") {
    throw new Error(`${context} field 'interactive' must be a boolean when provided.`);
  }

  if (entity.group != null && typeof entity.group !== "string") {
    throw new Error(`${context} field 'group' must be a string when provided.`);
  }

  const normalized = {
    id: entity.id,
    title: entity.title
  };

  if (entity.icon != null) {
    normalized.icon = entity.icon;
  }

  if (entity.interactive != null) {
    normalized.interactive = entity.interactive;
  }

  if (entity.group != null) {
    normalized.group = entity.group;
  }

  const data = normalizeEntityData(entity.data, context);
  if (data != null) {
    normalized.data = data;
  }

  return normalized;
}

export function validateEntitySearchResults(results) {
  if (!Array.isArray(results)) {
    throw new Error("ChatKit entity onTagSearch must return an array of entity objects.");
  }

  return results.map((entity, index) =>
    validateEntity(entity, `ChatKit entity result at index ${index}`)
  );
}

export function validateEntityPreviewResult(result) {
  if (result == null || typeof result !== "object" || Array.isArray(result)) {
    throw new Error("ChatKit entity preview handler must return an object.");
  }

  if (!("preview" in result)) {
    throw new Error("ChatKit entity preview handler must return an object with a 'preview' property.");
  }

  if (result.preview !== null && (typeof result.preview !== "object" || Array.isArray(result.preview))) {
    throw new Error("ChatKit entity preview handler 'preview' must be an object or null.");
  }

  return {
    preview: result.preview
  };
}

function getOptionalHandler(handlers, lookupPath, handlerName) {
  if (!(handlerName in handlers) || handlers[handlerName] == null) {
    return undefined;
  }

  if (typeof handlers[handlerName] !== "function") {
    throw new Error(
      `ChatKit entity handler '${handlerName}' in '${lookupPath}' must be a function.`
    );
  }

  return handlers[handlerName];
}

export function resolveEntityHandlers(globalScope, lookupPath) {
  const segments = normalizeLookupPath(lookupPath);
  let current = globalScope;

  for (const segment of segments) {
    if (current == null || typeof current !== "object" || !(segment in current)) {
      throw new Error(`ChatKit entity handlers '${lookupPath}' were not found on the page.`);
    }

    current = current[segment];
  }

  if (current == null || typeof current !== "object" || Array.isArray(current)) {
    throw new Error(`ChatKit entity handlers '${lookupPath}' must resolve to an object map.`);
  }

  return current;
}

export function buildEntitiesOption(globalScope, lookupPath, config) {
  const hasConfig = config != null && typeof config === "object";
  if (!lookupPath) {
    return hasConfig ? { ...config } : undefined;
  }

  const handlers = resolveEntityHandlers(globalScope, lookupPath);
  const onTagSearch = getOptionalHandler(handlers, lookupPath, "onTagSearch");
  const onClick = getOptionalHandler(handlers, lookupPath, "onClick");
  const onRequestPreview = getOptionalHandler(handlers, lookupPath, "onRequestPreview");

  if (!hasConfig && !onTagSearch && !onClick && !onRequestPreview) {
    return undefined;
  }

  const option = hasConfig ? { ...config } : {};

  if (onTagSearch) {
    option.onTagSearch = async (query) => validateEntitySearchResults(await onTagSearch(query));
  }

  if (onClick) {
    option.onClick = (entity) => onClick(validateEntity(entity, "ChatKit entity click payload"));
  }

  if (onRequestPreview) {
    option.onRequestPreview = async (entity) =>
      validateEntityPreviewResult(
        await onRequestPreview(validateEntity(entity, "ChatKit entity preview payload"))
      );
  }

  return option;
}
