import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const API = 'http://localhost:5224'
const CHAT = 'http://localhost:5225'

const bypass = (req) =>
  req.headers.accept?.includes('text/html') ? '/index.html' : undefined

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5174,
    proxy: {
      '/auth':               { target: API, changeOrigin: true, bypass },
      '/clubs':              { target: API, changeOrigin: true, bypass },
      '/halls':              { target: API, changeOrigin: true, bypass },
      '/staff':              { target: API, changeOrigin: true, bypass },
      '/clients':            { target: API, changeOrigin: true, bypass },
      '/schedule':           { target: API, changeOrigin: true, bypass },
      '/bookings':           { target: API, changeOrigin: true, bypass },
      '/subscription-types': { target: API, changeOrigin: true, bypass },
      '/subscriptions':      { target: API, changeOrigin: true, bypass },
      '/class-types':        { target: API, changeOrigin: true, bypass },
      '/visits':             { target: API, changeOrigin: true, bypass },
      '/reporting':          { target: API, changeOrigin: true, bypass },
      '/chat':               { target: CHAT, changeOrigin: true, bypass },
      '/hubs':               { target: CHAT, changeOrigin: true, ws: true },
    },
  },
})
