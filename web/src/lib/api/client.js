import axios from 'axios'

const baseURL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5085/api/v1'

export const api = axios.create({ baseURL })

let accessToken = null
let refreshToken = null

let handlers = {}

export function initializeAuth(next) {
  handlers = next
}

export function setTokens(access, refresh) {
  accessToken = access
  refreshToken = refresh
}

export function clearTokens() {
  accessToken = null
  refreshToken = null
}

api.interceptors.request.use((config) => {
  if (accessToken) {
    config.headers.Authorization = `Bearer ${accessToken}`
  }
  return config
})

let refreshing = null

async function refreshTokens() {
  if (!refreshToken) {
    return false
  }

  try {
    const response = await axios.post(`${baseURL}/auth/refresh`, { refreshToken })
    const data = response.data?.data
    if (!data) {
      return false
    }

    accessToken = data.accessToken
    refreshToken = data.refreshToken
    handlers.onTokens?.(data.accessToken, data.refreshToken)
    return true
  } catch {
    return false
  }
}

api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const original = error.config

    if (
      error.response?.status === 401 &&
      original &&
      !original._retry &&
      !original.url?.includes('/auth/')
    ) {
      original._retry = true
      refreshing ??= refreshTokens().finally(() => {
        refreshing = null
      })

      const refreshed = await refreshing
      if (refreshed && accessToken) {
        original.headers.Authorization = `Bearer ${accessToken}`
        return api(original)
      }

      handlers.onExpired?.()
    }

    return Promise.reject(error)
  },
)