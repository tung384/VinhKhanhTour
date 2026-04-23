import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': {
        //target: 'https://192.168.68.103:7164',
        target: 'https://192.168.247.209:7164',
        //target: 'https://192.168.32.1:7164',
        changeOrigin: true,
        secure: false,
      },
      '/uploads': {
        //target: 'https://192.168.68.103:7164',
        target: 'https://192.168.247.209:7164',
        //target: 'https://192.168.32.1:7164',
		changeOrigin: true,
        secure: false,
      }
    }
  }
})
