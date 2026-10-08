import { create } from 'zustand'
import { authApi } from '@/lib/api/auth'
import { clearTokens, initializeAuth, setTokens } from '@/lib/api/client'
import type { UserProfile } from '@/lib/api/types'

const STORAGE_KEY = 'tobacco.auth'

interface StoredAuth {
  user: UserProfile
  refreshToken: string
}

interface AuthState {
  user: UserProfile | null
  status: 'unknown' | 'authenticated' | 'anonymous'
  login: (email: string, password: string) => Promise<void>
  logout: () => Promise<void>
  bootstrap: () => Promise<void>
  hasPermission: (code: string) => boolean
}

function readStored(): StoredAuth | null {
  const raw = localStorage.getItem(STORAGE_KEY)
  if (!raw) {
    return null
  }

  try {
    return JSON.parse(raw) as StoredAuth
  } catch {
    return null
  }
}

function writeStored(value: StoredAuth | null) {
  if (value) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(value))
  } else {
    localStorage.removeItem(STORAGE_KEY)
  }
}

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  status: 'unknown',

  async login(email, password) {
    const tokens = await authApi.login(email, password)
    setTokens(tokens.accessToken, tokens.refreshToken)
    writeStored({ user: tokens.user, refreshToken: tokens.refreshToken })
    set({ user: tokens.user, status: 'authenticated' })
  },

  async logout() {
    const stored = readStored()
    try {
      if (stored) {
        await authApi.logout(stored.refreshToken)
      }
    } catch {
      // Ignore logout failures — the local session is cleared regardless.
    }
    clearTokens()
    writeStored(null)
    set({ user: null, status: 'anonymous' })
  },

  async bootstrap() {
    const stored = readStored()
    if (!stored) {
      set({ status: 'anonymous' })
      return
    }

    setTokens(null, stored.refreshToken)
    try {
      const tokens = await authApi.refresh(stored.refreshToken)
      setTokens(tokens.accessToken, tokens.refreshToken)
      writeStored({ user: tokens.user, refreshToken: tokens.refreshToken })
      set({ user: tokens.user, status: 'authenticated' })
    } catch {
      clearTokens()
      writeStored(null)
      set({ user: null, status: 'anonymous' })
    }
  },

  hasPermission(code) {
    return get().user?.permissions.includes(code) ?? false
  },
}))

initializeAuth({
  onTokens: (access, refresh) => {
    setTokens(access, refresh)
    const user = useAuthStore.getState().user
    if (user) {
      writeStored({ user, refreshToken: refresh })
    }
  },
  onExpired: () => {
    clearTokens()
    writeStored(null)
    useAuthStore.setState({ user: null, status: 'anonymous' })
  },
})
