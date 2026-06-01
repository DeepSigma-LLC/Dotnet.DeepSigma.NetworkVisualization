import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { resolve } from 'node:path';

export default defineConfig({
  plugins: [react()],
  resolve: {
    preserveSymlinks: true,
    alias: {
      'deepsigma-network-react': resolve(__dirname, '../../src/js/deepsigma-network-react/src/index.ts'),
      'deepsigma-network-core': resolve(__dirname, '../../src/js/deepsigma-network-core/src/index.ts'),
    },
  },
  optimizeDeps: {
    exclude: ['deepsigma-network-react', 'deepsigma-network-core'],
  },
  build: {
    outDir: resolve(__dirname, '../DeepSigma.NetworkVisualization.Demo.Web/wwwroot'),
    emptyOutDir: true,
  },
  server: {
    port: 5173,
    proxy: {
      '/api': 'http://localhost:5180',
    },
  },
});
