import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

// El web llama a /api y Vite lo reenvía a la API por HTTPS (RNF-02).
// API_URL permite apuntar a otra instancia sin tocar el código.
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      '/api': { target: process.env.API_URL ?? 'https://localhost:7080', changeOrigin: true, secure: false },
    },
  },
})
