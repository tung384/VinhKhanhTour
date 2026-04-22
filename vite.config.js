import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        target: 'https://192.168.68.108:7164',
        changeOrigin: true,
        secure: false,
      },
      '/uploads': {
        target: 'https://192.168.68.108:7164',
        changeOrigin: true,
        secure: false,
      }
    }
  }
})
