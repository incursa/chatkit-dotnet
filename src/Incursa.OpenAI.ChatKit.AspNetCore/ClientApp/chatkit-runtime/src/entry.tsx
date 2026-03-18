import { ChatKit, type UseChatKitOptions, useChatKit } from "@openai/chatkit-react";
import { createRoot } from "react-dom/client";

import "./runtime.css";

type ChatKitThemeConfig = {
  colorScheme?: "light" | "dark";
  radius?: "pill" | "round" | "soft" | "sharp";
  density?: "compact" | "normal" | "spacious";
};

type ChatKitHeaderConfig = {
  enabled?: boolean;
  title?: {
    text?: string;
  };
};

type ChatKitHistoryConfig = {
  enabled?: boolean;
  showDelete?: boolean;
  showRename?: boolean;
};

type ChatKitStartPromptConfig = {
  label: string;
  prompt: string;
  icon?: string;
};

type ChatKitStartScreenConfig = {
  greeting?: string;
  prompts?: ChatKitStartPromptConfig[];
};

type ChatKitComposerConfig = {
  placeholder?: string;
};

type ChatKitThreadItemActionsConfig = {
  feedback?: boolean;
  retry?: boolean;
};

type ChatKitWidgetActionsConfig = {
  forwardToEndpoint?: boolean;
};

type ChatKitHostConfig = {
  apiUrl?: string;
  domainKey?: string;
  sessionEndpoint?: string;
  actionEndpoint?: string;
  height?: string;
  locale?: string;
  frameTitle?: string;
  initialThread?: string;
  theme?: ChatKitThemeConfig;
  header?: ChatKitHeaderConfig;
  history?: ChatKitHistoryConfig;
  startScreen?: ChatKitStartScreenConfig;
  composer?: ChatKitComposerConfig;
  threadItemActions?: ChatKitThreadItemActionsConfig;
  widgetActions?: ChatKitWidgetActionsConfig;
};

type ChatKitSessionResponse = {
  client_secret?: string;
  clientSecret?: string;
};

async function getClientSecret(
  sessionEndpoint: string,
  currentClientSecret: string | null
): Promise<string> {
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

  const payload = (await response.json()) as ChatKitSessionResponse;
  const clientSecret = payload.client_secret ?? payload.clientSecret;
  if (!clientSecret) {
    throw new Error("ChatKit session endpoint did not return client_secret.");
  }

  return currentClientSecret && clientSecret.length === 0 ? currentClientSecret : clientSecret;
}

async function forwardAction(
  actionEndpoint: string,
  action: { type: string; payload?: Record<string, unknown> },
  itemId: string
): Promise<void> {
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

async function readErrorMessage(response: Response): Promise<string> {
  try {
    const contentType = response.headers.get("content-type");
    if (contentType?.includes("application/json")) {
      const payload = (await response.json()) as { title?: string; detail?: string };
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

async function sameOriginFetch(
  input: RequestInfo | URL,
  init?: RequestInit
): Promise<Response> {
  return fetch(input, {
    ...init,
    credentials: "same-origin"
  });
}

function requireDomainKey(domainKey?: string): string {
  if (!domainKey || domainKey.trim().length === 0) {
    throw new Error(
      "ChatKit custom API mode requires a domain key. Set `domain-key` on the tag helper or configure ChatKitAspNetCoreOptions.DomainKey."
    );
  }

  return domainKey;
}

function buildOptions(config: ChatKitHostConfig): UseChatKitOptions {
  const options: UseChatKitOptions = config.apiUrl
    ? {
        api: {
          url: config.apiUrl,
          domainKey: requireDomainKey(config.domainKey),
          fetch: sameOriginFetch
        }
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

  if (config.locale) {
    options.locale = config.locale;
  }

  if (config.frameTitle) {
    options.frameTitle = config.frameTitle;
  }

  if (config.initialThread) {
    options.initialThread = config.initialThread;
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

  if (config.threadItemActions) {
    options.threadItemActions = config.threadItemActions;
  }

  const shouldForwardActions =
    !config.apiUrl &&
    (config.widgetActions?.forwardToEndpoint ?? true) &&
    !!config.actionEndpoint;

  if (shouldForwardActions && config.actionEndpoint) {
    options.widgets = {
      onAction: async (action, widgetItem) =>
        forwardAction(config.actionEndpoint!, action, widgetItem.id)
    };
  }

  return options;
}

function ChatKitHost({ config }: { config: ChatKitHostConfig }) {
  const { control } = useChatKit(buildOptions(config));
  const style =
    config.height && config.height.trim().length > 0
      ? {
          height: config.height,
          minHeight: config.height
        }
      : undefined;

  return (
    <div className="incursa-chatkit-root" style={style}>
      <ChatKit control={control} className="incursa-chatkit-element" />
    </div>
  );
}

function renderHost(host: HTMLElement) {
  const rawConfig = host.dataset.incursaChatkitConfig;
  if (!rawConfig) {
    throw new Error("Missing ChatKit host config.");
  }

  const config = JSON.parse(rawConfig) as ChatKitHostConfig;
  createRoot(host).render(<ChatKitHost config={config} />);
}

function mountAll() {
  const hosts = document.querySelectorAll<HTMLElement>("[data-incursa-chatkit-host]");

  for (const host of hosts) {
    if (host.dataset.incursaChatkitMounted === "true") {
      continue;
    }

    try {
      renderHost(host);
      host.dataset.incursaChatkitMounted = "true";
    } catch (error) {
      const message = error instanceof Error ? error.message : "Unknown ChatKit error.";
      host.textContent = `ChatKit error: ${message}`;
    }
  }
}

if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", mountAll, { once: true });
} else {
  mountAll();
}
