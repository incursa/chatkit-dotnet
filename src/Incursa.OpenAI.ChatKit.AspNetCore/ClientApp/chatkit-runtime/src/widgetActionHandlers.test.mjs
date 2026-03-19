import test from "node:test";
import assert from "node:assert/strict";

import {
  composeWidgetActionHandlers,
  createOnWidgetAction,
  resolveWidgetActionHandler
} from "./widgetActionHandlers.js";

test("resolveWidgetActionHandler resolves dotted paths with window prefixes", () => {
  const globalScope = {
    chatkit: {
      onWidgetAction() {}
    }
  };

  assert.equal(
    resolveWidgetActionHandler(globalScope, "window.chatkit.onWidgetAction"),
    globalScope.chatkit.onWidgetAction
  );
});

test("createOnWidgetAction dispatches the upstream action and widget item payload", async () => {
  const calls = [];
  const globalScope = {
    chatkit: {
      async onWidgetAction(action, widgetItem) {
        calls.push([action, widgetItem]);
      }
    }
  };

  const onWidgetAction = createOnWidgetAction(globalScope, "chatkit.onWidgetAction");
  await onWidgetAction(
    {
      type: "save_profile",
      payload: {
        user_id: "user-1"
      }
    },
    {
      id: "widget-1",
      widget: {
        type: "card"
      }
    }
  );

  assert.deepEqual(calls, [
    [
      {
        type: "save_profile",
        payload: {
          user_id: "user-1"
        }
      },
      {
        id: "widget-1",
        widget: {
          type: "card"
        }
      }
    ]
  ]);
});

test("composeWidgetActionHandlers runs client handling before forwarding", async () => {
  const calls = [];
  const onWidgetAction = composeWidgetActionHandlers(
    async (action, widgetItem) => {
      calls.push(["client", action.type, widgetItem.id]);
    },
    async (action, widgetItem) => {
      calls.push(["forward", action.type, widgetItem.id]);
    }
  );

  await onWidgetAction(
    {
      type: "save_profile"
    },
    {
      id: "widget-1",
      widget: {
        type: "card"
      }
    }
  );

  assert.deepEqual(calls, [
    ["client", "save_profile", "widget-1"],
    ["forward", "save_profile", "widget-1"]
  ]);
});

test("composeWidgetActionHandlers skips forwarding when client handling fails", async () => {
  const calls = [];
  const onWidgetAction = composeWidgetActionHandlers(
    async () => {
      calls.push("client");
      throw new Error("client failed");
    },
    async () => {
      calls.push("forward");
    }
  );

  await assert.rejects(() =>
    onWidgetAction(
      {
        type: "save_profile"
      },
      {
        id: "widget-1",
        widget: {
          type: "card"
        }
      }
    )
  );

  assert.deepEqual(calls, ["client"]);
});

test("createOnWidgetAction throws when the handler is missing", async () => {
  const onWidgetAction = createOnWidgetAction({}, "chatkit.onWidgetAction");

  await assert.rejects(
    () =>
      onWidgetAction(
        {
          type: "save_profile"
        },
        {
          id: "widget-1",
          widget: {
            type: "card"
          }
        }
      ),
    /ChatKit widget action handler 'chatkit\.onWidgetAction' was not found on the page\./
  );
});
