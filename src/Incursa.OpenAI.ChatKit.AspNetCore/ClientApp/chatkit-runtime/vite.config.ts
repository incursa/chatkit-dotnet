import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  plugins: [react()],
  build: {
    outDir: path.resolve(__dirname, "../../wwwroot/chatkit"),
    emptyOutDir: true,
    rollupOptions: {
      input: path.resolve(__dirname, "src/entry.tsx"),
      output: {
        entryFileNames: "chatkit.js",
        chunkFileNames: "chunks/[name]-[hash].js",
        assetFileNames: (assetInfo) => {
          if (assetInfo.name?.endsWith(".css")) {
            return "chatkit.css";
          }

          return "assets/[name]-[hash][extname]";
        }
      }
    }
  }
});
