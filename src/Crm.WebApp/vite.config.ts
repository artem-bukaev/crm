import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost:5080',
      '/swagger': 'http://localhost:5080',
      '/hangfire': 'http://localhost:5080',
      '/health': 'http://localhost:5080',
    },
  },
  build: {
    chunkSizeWarningLimit: 1400,
  },
});
