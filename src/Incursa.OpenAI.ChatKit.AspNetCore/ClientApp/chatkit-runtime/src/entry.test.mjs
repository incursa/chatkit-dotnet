import test from "node:test";
import assert from "node:assert/strict";

import { buildOptions, mountAll, renderHost } from "./runtimeHost.js";

class FakeElement {
  constructor(tagName) {
    this.tagName = tagName;
    this.className = "";
    this.style = {};
    this.dataset = {};
    this.textContent = "";
    this.children = [];
    this.optionsSet = [];
  }

  append(child) {
    this.children.push(child);
  }

  replaceChildren(...children) {
    this.children = [...children];
  }

  setOptions(options) {
    this.optionsSet.push(options);
  }
}

test("buildOptions preserves serializable config and wires callbacks without React", () => {
  const globalScope = {
    chatkit: {
      headerActions: {
        openHistory() {
          globalScope.headerActionCalls = (globalScope.headerActionCalls ?? 0) + 1;
        }
      },
      clientTools: {
        lookup() {
          return { ok: true };
        }
      }
    }
  };

  const options = buildOptions(
    {
      apiUrl: "/api/chatkit",
      domainKey: "domain-key",
      locale: "en",
      uploadStrategy: {
        type: "direct",
        uploadUrl: "/files"
      },
      clientToolHandlers: "chatkit.clientTools",
      theme: {
        colorScheme: "dark",
        typography: {
          baseSize: 16,
          fontFamily: "Inter",
          fontFamilyMono: "IBM Plex Mono",
          fontSources: [
            {
              family: "Inter",
              src: "/fonts/inter.woff2",
              display: "swap"
            }
          ]
        },
        color: {
          grayscale: {
            hue: 210,
            tint: 4,
            shade: -2
          },
          accent: {
            primary: "#8B5CF6",
            level: 2
          },
          surface: {
            background: "#111111",
            foreground: "#F5F5F5"
          }
        }
      },
      header: {
        enabled: true,
        leftAction: {
          icon: "history",
          onClickHandler: "chatkit.headerActions.openHistory"
        },
        title: {
          enabled: false,
          text: "Assistant"
        }
      },
      composer: {
        attachments: {
          enabled: true,
          maxSize: 1024,
          maxCount: 3,
          accept: {
            "application/pdf": [".pdf"]
          }
        },
        tools: [
          {
            id: "summarize",
            label: "Summarize",
            icon: "book-open"
          }
        ],
        models: [
          {
            id: "gpt-4.1",
            label: "Quality",
            description: "All rounder",
            default: true
          }
        ],
        dictation: {
          enabled: true
        }
      }
    },
    globalScope
  );

  assert.equal(options.api.url, "/api/chatkit");
  assert.equal(options.api.domainKey, "domain-key");
  assert.equal(options.api.uploadStrategy.type, "direct");
  assert.equal(options.api.uploadStrategy.uploadUrl, "/files");
  assert.equal(options.locale, "en");
  assert.equal(options.theme.colorScheme, "dark");
  assert.equal(options.theme.typography.baseSize, 16);
  assert.equal(options.theme.typography.fontFamily, "Inter");
  assert.equal(options.theme.typography.fontFamilyMono, "IBM Plex Mono");
  assert.equal(options.theme.typography.fontSources[0].family, "Inter");
  assert.equal(options.theme.color.grayscale.hue, 210);
  assert.equal(options.theme.color.accent.primary, "#8B5CF6");
  assert.equal(options.theme.color.surface.background, "#111111");
  assert.equal(options.header.enabled, true);
  assert.equal(options.header.leftAction.icon, "history");
  assert.equal(options.header.title.enabled, false);
  assert.equal(options.header.title.text, "Assistant");
  options.header.leftAction.onClick();
  assert.equal(globalScope.headerActionCalls, 1);
  assert.equal(options.composer.attachments.enabled, true);
  assert.equal(options.composer.attachments.maxSize, 1024);
  assert.equal(options.composer.attachments.maxCount, 3);
  assert.deepEqual(options.composer.attachments.accept, {
    "application/pdf": [".pdf"]
  });
  assert.equal(options.composer.tools[0].id, "summarize");
  assert.equal(options.composer.models[0].default, true);
  assert.equal(options.composer.dictation.enabled, true);
  assert.equal(typeof options.onClientTool, "function");
});

test("buildOptions rejects direct API mode without a domain key", () => {
  assert.throws(
    () =>
      buildOptions({
        apiUrl: "/api/chatkit"
      }, {}),
    /ChatKit direct API mode requires a domainKey\./
  );
});

test("buildOptions wires widgets.onAction for client handler in API mode", async () => {
  const calls = [];
  const globalScope = {
    chatkit: {
      async onWidgetAction(action, widgetItem) {
        calls.push([action.type, widgetItem.id]);
      }
    }
  };

  const options = buildOptions(
    {
      apiUrl: "/api/chatkit",
      domainKey: "domain-key",
      widgetActionHandler: "chatkit.onWidgetAction"
    },
    globalScope
  );

  assert.equal(typeof options.widgets?.onAction, "function");
  await options.widgets.onAction({ type: "save_profile" }, { id: "widget-1", widget: { type: "card" } });
  assert.deepEqual(calls, [["save_profile", "widget-1"]]);
});

test("buildOptions wires widgets.onAction for client-only handler in hosted mode", async () => {
  const calls = [];
  const globalScope = {
    chatkit: {
      async onWidgetAction(action, widgetItem) {
        calls.push([action.type, widgetItem.id]);
      }
    }
  };

  const options = buildOptions(
    {
      sessionEndpoint: "/api/chatkit/session",
      widgetActionHandler: "chatkit.onWidgetAction",
      widgetActions: { forwardToEndpoint: false }
    },
    globalScope
  );

  assert.equal(typeof options.widgets?.onAction, "function");
  await options.widgets.onAction({ type: "save_profile" }, { id: "widget-1", widget: { type: "card" } });
  assert.deepEqual(calls, [["save_profile", "widget-1"]]);
});

test("buildOptions composes widgets.onAction with client handler running before endpoint forwarding", async () => {
  const calls = [];
  const globalScope = {
    chatkit: {
      async onWidgetAction(action, widgetItem) {
        calls.push(["client", action.type, widgetItem.id]);
      }
    }
  };

  const originalFetch = globalThis.fetch;
  const forwardCalls = [];
  globalThis.fetch = async (url, init) => {
    forwardCalls.push(url);
    return { ok: true };
  };

  try {
    const options = buildOptions(
      {
        sessionEndpoint: "/api/chatkit/session",
        actionEndpoint: "/api/chatkit/action",
        widgetActionHandler: "chatkit.onWidgetAction",
        widgetActions: { forwardToEndpoint: true }
      },
      globalScope
    );

    assert.equal(typeof options.widgets?.onAction, "function");
    await options.widgets.onAction({ type: "save_profile" }, { id: "widget-1", widget: { type: "card" } });
    assert.deepEqual(calls, [["client", "save_profile", "widget-1"]]);
    assert.equal(forwardCalls.length, 1);
    assert.equal(forwardCalls[0], "/api/chatkit/action");
  } finally {
    globalThis.fetch = originalFetch;
  }
});

test("renderHost creates a direct openai-chatkit element and applies options immediately", () => {
  const host = new FakeElement("div");
  host.dataset.incursaChatkitConfig = JSON.stringify({
    apiUrl: "/api/chatkit",
    domainKey: "domain-key",
    height: "720px"
  });

  const created = [];
  const documentScope = {
    createElement(tagName) {
      const element = new FakeElement(tagName);
      created.push(element);
      return element;
    }
  };
  const registry = {
    get(name) {
      return name === "openai-chatkit" ? {} : undefined;
    },
    whenDefined() {
      return Promise.resolve();
    }
  };

  renderHost(host, {}, documentScope, registry);

  const [root, chatkit] = created;
  assert.equal(root.className, "incursa-chatkit-root");
  assert.equal(root.style.height, "720px");
  assert.equal(root.style.minHeight, "720px");
  assert.equal(chatkit.tagName, "openai-chatkit");
  assert.equal(chatkit.className, "incursa-chatkit-element");
  assert.equal(root.children[0], chatkit);
  assert.equal(host.children[0], root);
  assert.equal(chatkit.optionsSet.length, 1);
  assert.equal(chatkit.optionsSet[0].api.url, "/api/chatkit");
});

test("mountAll waits for the custom element definition before applying options", async () => {
  const host = new FakeElement("div");
  host.dataset.incursaChatkitConfig = JSON.stringify({
    sessionEndpoint: "/api/chatkit/session"
  });

  const documentScope = {
    querySelectorAll() {
      return [host];
    },
    createElement(tagName) {
      return new FakeElement(tagName);
    }
  };

  let resolveDefinition;
  const registry = {
    get() {
      return undefined;
    },
    whenDefined(name) {
      assert.equal(name, "openai-chatkit");
      return new Promise((resolve) => {
        resolveDefinition = resolve;
      });
    }
  };

  mountAll(documentScope, {}, registry);

  const root = host.children[0];
  const chatkit = root.children[0];
  assert.equal(host.dataset.incursaChatkitMounted, "true");
  assert.equal(chatkit.optionsSet.length, 0);

  resolveDefinition();
  await Promise.resolve();
  await Promise.resolve();

  assert.equal(chatkit.optionsSet.length, 1);
  assert.equal(typeof chatkit.optionsSet[0].api.getClientSecret, "function");
});
