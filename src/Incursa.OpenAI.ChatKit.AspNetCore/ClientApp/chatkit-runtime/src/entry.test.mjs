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
      clientToolHandlers: "chatkit.clientTools",
      theme: {
        colorScheme: "dark"
      },
      header: {
        title: {
          text: "Assistant"
        }
      }
    },
    globalScope
  );

  assert.equal(options.api.url, "/api/chatkit");
  assert.equal(options.api.domainKey, "domain-key");
  assert.equal(options.locale, "en");
  assert.equal(options.theme.colorScheme, "dark");
  assert.equal(options.header.title.text, "Assistant");
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
