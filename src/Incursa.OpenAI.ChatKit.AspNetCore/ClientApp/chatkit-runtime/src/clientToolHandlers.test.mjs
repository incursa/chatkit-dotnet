import test from "node:test";
import assert from "node:assert/strict";

import {
  createOnClientTool,
  resolveClientToolHandlers
} from "./clientToolHandlers.js";

test("resolveClientToolHandlers resolves dotted paths with window prefixes", () => {
  const globalScope = {
    chatkit: {
      clientTools: {
        get_selected_canvas_nodes() {
          return {};
        }
      }
    }
  };

  assert.equal(
    resolveClientToolHandlers(globalScope, "window.chatkit.clientTools"),
    globalScope.chatkit.clientTools
  );
});

test("createOnClientTool dispatches the named handler with the upstream tool-call shape", async () => {
  const calls = [];
  const globalScope = {
    chatkit: {
      clientTools: {
        async get_selected_canvas_nodes(toolCall) {
          calls.push(toolCall);
          return {
            nodes: [{ id: "node-1", kind: "shape" }]
          };
        }
      }
    }
  };

  const onClientTool = createOnClientTool(globalScope, "chatkit.clientTools");
  const result = await onClientTool({
    name: "get_selected_canvas_nodes",
    params: { project: "demo" }
  });

  assert.deepEqual(calls, [
    {
      name: "get_selected_canvas_nodes",
      params: { project: "demo" }
    }
  ]);
  assert.deepEqual(result, {
    nodes: [{ id: "node-1", kind: "shape" }]
  });
});

test("createOnClientTool throws when the registry is missing", async () => {
  const onClientTool = createOnClientTool({}, "chatkit.clientTools");

  await assert.rejects(
    () => onClientTool({ name: "missing_tool", params: {} }),
    /ChatKit client tool handlers 'chatkit\.clientTools' were not found on the page\./
  );
});

test("createOnClientTool throws when the named handler is missing", async () => {
  const onClientTool = createOnClientTool(
    {
      chatkit: {
        clientTools: {}
      }
    },
    "chatkit.clientTools"
  );

  await assert.rejects(
    () => onClientTool({ name: "missing_tool", params: {} }),
    /ChatKit client tool 'missing_tool' is not registered in 'chatkit\.clientTools'\./
  );
});
