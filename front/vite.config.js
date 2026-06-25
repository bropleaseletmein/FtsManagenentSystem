import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const API_TARGET = 'http://localhost:5224'
const CHAT_TARGET = 'http://localhost:5225'

// Browser navigation sends Accept: text/html → serve SPA index.
// Fetch/XHR requests send Accept: application/json or */* → proxy to API.
const bypass = (req) =>
  req.headers.accept?.includes('text/html') ? '/index.html' : undefined

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/auth':               { target: API_TARGET, changeOrigin: true, bypass },
      '/clients':            { target: API_TARGET, changeOrigin: true, bypass },
      '/clubs':              { target: API_TARGET, changeOrigin: true, bypass },
      '/bookings':           { target: API_TARGET, changeOrigin: true, bypass },
      '/schedule':           { target: API_TARGET, changeOrigin: true, bypass },
      '/subscription-types': { target: API_TARGET, changeOrigin: true, bypass },
      '/subscriptions':      { target: API_TARGET, changeOrigin: true, bypass },
      '/class-types':        { target: API_TARGET, changeOrigin: true, bypass },
      '/visits':             { target: API_TARGET, changeOrigin: true, bypass },
      '/chat':               { target: CHAT_TARGET, changeOrigin: true, bypass },
      '/hubs':               { target: CHAT_TARGET, changeOrigin: true, ws: true },
    },
  },
})
