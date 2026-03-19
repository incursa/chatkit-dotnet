import test from "node:test";
import assert from "node:assert/strict";

import {
  buildEntitiesOption,
  resolveEntityHandlers,
  validateEntityPreviewResult,
  validateEntitySearchResults
} from "./entityHandlers.js";

test("resolveEntityHandlers resolves dotted paths with window prefixes", () => {
  const globalScope = {
    chatkit: {
      entityHandlers: {
        onTagSearch() {
          return [];
        }
      }
    }
  };

  assert.equal(
    resolveEntityHandlers(globalScope, "window.chatkit.entityHandlers"),
    globalScope.chatkit.entityHandlers
  );
});

test("buildEntitiesOption wires entity callbacks and preserves serializable config", async () => {
  const calls = [];
  const globalScope = {
    chatkit: {
      entityHandlers: {
        async onTagSearch(query) {
          calls.push(["search", query]);
          return [
            {
              id: "article-1",
              title: "Article",
              group: "Docs",
              icon: "document",
              interactive: true,
              data: {
                type: "article"
              }
            }
          ];
        },
        onClick(entity) {
          calls.push(["click", entity]);
        },
        async onRequestPreview(entity) {
          calls.push(["preview", entity]);
          return {
            preview: {
              type: "root",
              children: []
            }
          };
        }
      }
    }
  };

  const entities = buildEntitiesOption(globalScope, "chatkit.entityHandlers", {
    showComposerMenu: true
  });

  assert.equal(entities.showComposerMenu, true);
  assert.ok(typeof entities.onTagSearch === "function");
  assert.ok(typeof entities.onClick === "function");
  assert.ok(typeof entities.onRequestPreview === "function");

  const searchResults = await entities.onTagSearch("art");
  entities.onClick({
    id: "article-1",
    title: "Article",
    data: {
      type: "article"
    }
  });
  const previewResult = await entities.onRequestPreview({
    id: "article-1",
    title: "Article",
    interactive: true
  });

  assert.deepEqual(searchResults, [
    {
      id: "article-1",
      title: "Article",
      group: "Docs",
      icon: "document",
      interactive: true,
      data: {
        type: "article"
      }
    }
  ]);
  assert.deepEqual(previewResult, {
    preview: {
      type: "root",
      children: []
    }
  });
  assert.deepEqual(calls, [
    ["search", "art"],
    [
      "click",
      {
        id: "article-1",
        title: "Article",
        data: {
          type: "article"
        }
      }
    ],
    [
      "preview",
      {
        id: "article-1",
        title: "Article",
        interactive: true
      }
    ]
  ]);
});

test("validateEntitySearchResults rejects invalid entity payloads", () => {
  assert.throws(
    () =>
      validateEntitySearchResults([
        {
          id: "doc-1",
          title: "Doc",
          data: {
            version: 2
          }
        }
      ]),
    /ChatKit entity result at index 0 field 'data\.version' must be a string\./
  );
});

test("validateEntityPreviewResult rejects invalid preview payloads", () => {
  assert.throws(
    () =>
      validateEntityPreviewResult({
        preview: "invalid"
      }),
    /ChatKit entity preview handler 'preview' must be an object or null\./
  );
});

test("buildEntitiesOption throws when configured handlers are not found", () => {
  assert.throws(
    () => buildEntitiesOption({}, "chatkit.entityHandlers"),
    /ChatKit entity handlers 'chatkit\.entityHandlers' were not found on the page\./
  );
});
