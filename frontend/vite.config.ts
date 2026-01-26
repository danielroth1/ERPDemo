import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  // We intentionally proxy API calls in dev to avoid browser CORS.
  // The frontend should call `/api/...` and Vite will forward to the gateway.
  const env = loadEnv(mode, process.cwd(), '');
  const gatewayTarget = (env.VITE_API_GATEWAY_URL || 'http://localhost:8080').replace(/\/+$/, '');

  return {
    plugins: [react()],
    server: {
      proxy: {
        '/api': {
          target: gatewayTarget,
          changeOrigin: true,
          secure: false,
        },
        // SignalR/WebSocket hub (optional, but safe to include)
        '/dashboardHub': {
          target: gatewayTarget,
          ws: true,
          changeOrigin: true,
          secure: false,
        },
      },
    },
  };
});
