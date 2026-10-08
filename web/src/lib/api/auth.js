import { api } from './client'

export const authApi = {
  async login(email, password) {
    const response = await api.post('/auth/login', { email, password })
    return response.data.data
  },

  async refresh(refreshToken) {
    const response = await api.post('/auth/refresh', { refreshToken })
    return response.data.data
  },

  async logout(refreshToken) {
    await api.post('/auth/logout', { refreshToken })
  },

  async profile() {
    const response = await api.get('/auth/profile')
    return response.data.data
  },
}