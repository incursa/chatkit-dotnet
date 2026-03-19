// @ts-check
/// <reference types="@openai/chatkit" />

import { createOnClientTool } from "./clientToolHandlers.js";
import { buildEntitiesOption } from "./entityHandlers.js";
import {
  composeWidgetActionHandlers,
  createOnWidgetAction
} from "./widgetActionHandlers.js";

/**
 * @typedef {import("@openai/chatkit").ChatKitOptions} ChatKitOptions
 * @typedef {import("@openai/chatkit").OpenAIChatKit} OpenAIChatKit
 */

/**
 * @typedef {{
 *   colorScheme?: "light" | "dark";
 *   radius?: "pill" | "round" | "soft" | "sharp";
 *   density?: "compact" | "normal" | "spacious";
 * }} ChatKitThemeConfig
 */

/**
 * @typedef {{
 *   enabled?: boolean;
 *   title?: {
 *     enabled?: boolean;
 *     text?: string;
 *   };
 * }} ChatKitHeaderConfig
 */

/**
 * @typedef {{
 *   enabled?: boolean;
 *   showDelete?: boolean;
 *   showRename?: boolean;
 * }} ChatKitHistoryConfig
 */

/**
 * @typedef {{
 *   label: string;
 *   prompt: string;
 *   icon?: string;
 * }} ChatKitStartPromptConfig
 */

/**
 * @typedef {{
 *   greeting?: string;
 *   prompts?: ChatKitStartPromptConfig[];
 * }} ChatKitStartScreenConfig
 */

/**
 * @typedef {{
 *   placeholder?: string;
 * }} ChatKitComposerConfig
 */

/**
 * @typedef {{
 *   text?: string;
 *   highContrast?: boolean;
 * }} ChatKitDisclaimerConfig
 */

/**
 * @typedef {{
 *   showComposerMenu?: boolean;
 * }} ChatKitEntitiesConfig
 */

/**
 * @typedef {{
 *   feedback?: boolean;
 *   retry?: boolean;
 * }} ChatKitThreadItemActionsConfig
 */

/**
 * @typedef {{
 *   forwardToEndpoint?: boolean;
 * }} ChatKitWidgetActionsConfig
 */

/**
 * @typedef {{
 *   apiUrl?: string;
 *   domainKey?: string;
 *   sessionEndpoint?: string;
 *   actionEndpoint?: string;
 *   height?: string;
 *   locale?: string;
 *   frameTitle?: string;
 *   initialThread?: string;
 *   clientToolHandlers?: string;
 *   entityHandlers?: string;
 *   widgetActionHandler?: string;
 *   theme?: ChatKitThemeConfig;
 *   header?: ChatKitHeaderConfig;
 *   history?: ChatKitHistoryConfig;
 *   startScreen?: ChatKitStartScreenConfig;
 *   composer?: ChatKitComposerConfig;
 *   disclaimer?: ChatKitDisclaimerConfig;
 *   entities?: ChatKitEntitiesConfig;
 *   threadItemActions?: ChatKitThreadItemActionsConfig;
 *   widgetActions?: ChatKitWidgetActionsConfig;
 * }} ChatKitHostConfig
 */

/**
 * @typedef {{
 *   client_secret?: string;
 *   clientSecret?: string;
 * }} ChatKitSessionResponse
 */

/**
 * @typedef {{
 *   dataset: Record<string, string | undefined>;
 *   textContent: string;
 *   replaceChildren: (...nodes: unknown[]) => void;
 * }} ChatKitHostElement
 */

// This module is the handwritten bridge between Razor-rendered config
// (`data-incursa-chatkit-config`) and the upstream <openai-chatkit> element.
// Vite bundles this file into wwwroot/chatkit/chatkit.js, but this source file
// is the version meant to be read and maintained.

async function getClientSecret(sessionEndpoint, currentClientSecret) {
  const response = await fetch(sessionEndpoint, {
    method: "POST",
    credentials: "same-origin",
    headers: {
      Accept: "application/json"
    }
  });

  if (!response.ok) {
    const message = await readErrorMessage(response);
    throw new Error(message);
  }

  const payload = /** @type {ChatKitSessionResponse} */ (await response.json());
  const clientSecret = payload.client_secret ?? payload.clientSecret;
  if (!clientSecret) {
    throw new Error("ChatKit session endpoint did not return client_secret.");
  }

  return currentClientSecret && clientSecret.length === 0 ? currentClientSecret : clientSecret;
}

async function forwardAction(actionEndpoint, action, itemId) {
  const response = await fetch(actionEndpoint, {
    method: "POST",
    credentials: "same-origin",
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json"
    },
    body: JSON.stringify({
      action,
      itemId
    })
  });

  if (!response.ok) {
    const message = await readErrorMessage(response);
    throw new Error(message);
  }
}

async function readErrorMessage(response) {
  try {
    const contentType = response.headers.get("content-type");
    if (contentType?.includes("application/json")) {
      const payload = /** @type {{ title?: string; detail?: string }} */ (
        await response.json()
      );
      if (payload.detail) {
        return payload.detail;
      }

      if (payload.title) {
        return payload.title;
      }
    }

    const text = await response.text();
    if (text.trim().length > 0) {
      return text;
    }
  } catch {
    // Fall through to the generic message.
  }

  return `ChatKit request failed (${response.status}).`;
}

async function sameOriginFetch(input, init) {
  return fetch(input, {
    ...init,
    credentials: "same-origin"
  });
}

/**
 * @param {ChatKitHostConfig} config
 * @returns {ChatKitOptions["api"]}
 */
function buildCustomApiOptions(config) {
  const domainKey = config.domainKey?.trim();
  if (!domainKey) {
    throw new Error("ChatKit direct API mode requires a domainKey.");
  }

  return {
    url: config.apiUrl,
    fetch: sameOriginFetch,
    domainKey
  };
}

/**
 * @param {ChatKitHostConfig} config
 * @param {typeof window} [globalScope]
 * @returns {ChatKitOptions}
 */
export function buildOptions(config, globalScope = window) {
  /** @type {ChatKitOptions} */
  const options = config.apiUrl
    ? {
        api: buildCustomApiOptions(config)
      }
    : {
        api: {
          getClientSecret: async (currentClientSecret) =>
            getClientSecret(
              config.sessionEndpoint ?? "/api/chatkit/session",
              currentClientSecret
            )
        }
      };

  // Everything below is straightforward option projection: take the serialized
  // server config, add browser callback adapters where needed, and produce the
  // exact ChatKitOptions object expected by setOptions(...).
  if (config.locale) {
    options.locale = config.locale;
  }

  if (config.frameTitle) {
    options.frameTitle = config.frameTitle;
  }

  if (config.initialThread) {
    options.initialThread = config.initialThread;
  }

  if (config.clientToolHandlers) {
    options.onClientTool = createOnClientTool(globalScope, config.clientToolHandlers);
  }

  if (config.theme) {
    options.theme = config.theme;
  }

  if (config.header) {
    options.header = config.header;
  }

  if (config.history) {
    options.history = config.history;
  }

  if (config.startScreen) {
    options.startScreen = config.startScreen;
  }

  if (config.composer) {
    options.composer = config.composer;
  }

  if (config.disclaimer) {
    options.disclaimer = config.disclaimer;
  }

  const entities = buildEntitiesOption(
    globalScope,
    config.entityHandlers,
    config.entities
  );
  if (entities) {
    options.entities = entities;
  }

  if (config.threadItemActions) {
    options.threadItemActions = config.threadItemActions;
  }

  const shouldForwardActions =
    !config.apiUrl &&
    (config.widgetActions?.forwardToEndpoint ?? true) &&
    !!config.actionEndpoint;

  const clientWidgetActionHandler = config.widgetActionHandler
    ? createOnWidgetAction(globalScope, config.widgetActionHandler)
    : undefined;
  const forwardedWidgetActionHandler =
    shouldForwardActions && config.actionEndpoint
      ? async (action, widgetItem) =>
          forwardAction(config.actionEndpoint, action, widgetItem.id)
      : undefined;
  const onWidgetAction = composeWidgetActionHandlers(
    clientWidgetActionHandler,
    forwardedWidgetActionHandler
  );

  if (onWidgetAction) {
    options.widgets = {
      onAction: onWidgetAction
    };
  }

  return options;
}

function applyHostHeight(root, height) {
  if (!height || height.trim().length === 0) {
    return;
  }

  root.style.height = height;
  root.style.minHeight = height;
}

function applyOptions(chatkit, options, registry) {
  // The CDN script is responsible for defining the custom element. If it has
  // already loaded, configure immediately; otherwise wait for the definition.
  if (registry.get("openai-chatkit")) {
    chatkit.setOptions(options);
    return;
  }

  void registry.whenDefined("openai-chatkit").then(() => {
    chatkit.setOptions(options);
  });
}

/**
 * @param {ChatKitHostElement} host
 * @param {typeof window} [globalScope]
 * @param {Document} [documentScope]
 * @param {CustomElementRegistry} [registry]
 */
export function renderHost(
  host,
  globalScope = window,
  documentScope = document,
  registry = customElements
) {
  // Each Razor host is just a div with serialized JSON config. We replace its
  // contents with a local wrapper div and one <openai-chatkit> element.
  const rawConfig = host.dataset.incursaChatkitConfig;
  if (!rawConfig) {
    throw new Error("Missing ChatKit host config.");
  }

  const config = /** @type {ChatKitHostConfig} */ (JSON.parse(rawConfig));
  const root = documentScope.createElement("div");
  root.className = "incursa-chatkit-root";
  applyHostHeight(root, config.height);

  const chatkit = /** @type {OpenAIChatKit} */ (
    documentScope.createElement("openai-chatkit")
  );
  chatkit.className = "incursa-chatkit-element";
  root.append(chatkit);

  host.replaceChildren(root);
  applyOptions(chatkit, buildOptions(config, globalScope), registry);
}

/**
 * @param {Document} [documentScope]
 * @param {typeof window} [globalScope]
 * @param {CustomElementRegistry} [registry]
 */
export function mountAll(
  documentScope = document,
  globalScope = window,
  registry = customElements
) {
  // Support multiple hosts on a page, though most apps only render one.
  const hosts = documentScope.querySelectorAll("[data-incursa-chatkit-host]");

  for (const host of hosts) {
    if (host.dataset.incursaChatkitMounted === "true") {
      continue;
    }

    try {
      renderHost(host, globalScope, documentScope, registry);
      host.dataset.incursaChatkitMounted = "true";
    } catch (error) {
      const message = error instanceof Error ? error.message : "Unknown ChatKit error.";
      host.textContent = `ChatKit error: ${message}`;
    }
  }
}
