import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

const API = 'http://localhost:5224'

const bypass = (req) =>
  req.headers.accept?.includes('text/html') ? '/index.html' : undefined

export default defineConfig({
  plugins: [react()],
  server: {
    port: 5175,
    proxy: {
      '/auth':      { target: API, changeOrigin: true, bypass },
      '/clubs':     { target: API, changeOrigin: true, bypass },
      '/turnstile': { target: API, changeOrigin: true, bypass },
    },
  },
})
