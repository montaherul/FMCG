export interface ApiError {
  code: string
  message: string
  details?: unknown
}

export interface ApiEnvelope<T> {
  success: boolean
  data?: T
  error?: ApiError
}

export interface PagedResult<T> {
  items: T[]
  page: number
  pageSize: number
  total: number
  totalPages: number
}

export interface UserProfile {
  id: string
  tenantId: string | null
  email: string
  fullName: string
  isPlatformScope: boolean
  scopeNodeId: string | null
  roles: string[]
  permissions: string[]
}

export interface TokenResponse {
  accessToken: string
  accessTokenExpiresAt: string
  refreshToken: string
  refreshTokenExpiresAt: string
  user: UserProfile
}

export function apiErrorMessage(error: unknown): string {
  const fallback = 'Something went wrong. Please try again.'
  if (typeof error === 'object' && error !== null && 'response' in error) {
    const response = (error as { response?: { data?: ApiEnvelope<unknown> } }).response
    return response?.data?.error?.message ?? fallback
  }
  return fallback
}
