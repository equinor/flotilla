import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react-swc'
import history from 'connect-history-api-fallback'
import type { Plugin } from 'vite'

// https://vitejs.dev/config/
export default defineConfig({
  base: './',
  plugins: [
    react(),
    {
      name: 'spa-fallback',
      configureServer(server) {
        server.middlewares.use(
          history({
            disableDotRule: true,
            htmlAcceptHeaders: ['text/html', 'application/xhtml+xml'],
          })
        );
      },
    } as Plugin,
  ],
  test: {
    environment: 'jsdom',
  },
  resolve: { // To handle absolute imports
    alias: {
      'api': '/src/api',
      'components': '/src/components',
      'language': '/src/language',
      'mediaAssets': '/src/mediaAssets',
      'models': '/src/models',
      'utils': '/src/utils',
      'config': '/src/config'
    }
  },
  server: {
    open: true,
    port: 3001,
  },
  build: {
    target: 'esnext' // To support "Top-level-await"
  }
})
