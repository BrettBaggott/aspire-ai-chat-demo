import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import type { UserConfig } from 'vite';

const target = process.env.CHATAPI_HTTPS || process.env.CHATAPI_HTTP;

export default defineConfig({
  plugins: [react()],
  server: {
    host: true,
    port: 5173,
    strictPort: true,
    open: true,
    proxy: {
      '/api': {
        target,
        changeOrigin: true,
      },
      '/api/chat/stream': {
        target,
        ws: true,
        changeOrigin: true,
      }
    }
  }
} as UserConfig);
