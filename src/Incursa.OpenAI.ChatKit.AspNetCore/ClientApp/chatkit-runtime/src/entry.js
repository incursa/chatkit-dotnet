import "./runtime.css";
import { mountAll } from "./runtimeHost.js";

// This file is the only browser entrypoint Vite bundles. The heavier logic
// lives in runtimeHost.js so it can be unit-tested without importing CSS.
if (document.readyState === "loading") {
  document.addEventListener("DOMContentLoaded", () => mountAll(document, window), {
    once: true
  });
} else {
  mountAll(document, window);
}
