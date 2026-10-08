import { api } from './client'
import type { ApiEnvelope, TokenResponse, UserProfile } from './types'

export const authApi = {
  async login(email: string, password: string): Promise<TokenResponse> {
    const response = await api.post<ApiEnvelope<TokenResponse>>('/auth/login', { email, password })
    return response.data.data as TokenResponse
  },

  async refresh(refreshToken: string): Promise<TokenResponse> {
    const response = await api.post<ApiEnvelope<TokenResponse>>('/auth/refresh', { refreshToken })
    return response.data.data as TokenResponse
  },

  async logout(refreshToken: string): Promise<void> {
    await api.post('/auth/logout', { refreshToken })
  },

  async profile(): Promise<UserProfile> {
    const response = await api.get<ApiEnvelope<UserProfile>>('/auth/profile')
    return response.data.data as UserProfile
  },
}
