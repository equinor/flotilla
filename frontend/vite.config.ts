import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react-swc'

// https://vitejs.dev/config/
export default defineConfig({
  base: './',
  plugins: [
    react()],
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
      'config': '/src/config',
      'pages': '/src/pages'
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
