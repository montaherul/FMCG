export const TenantStatus = Object.freeze({
  Active: 0,
  Suspended: 1,
  Trial: 2,
  Terminated: 3,
})

export function apiErrorMessage(error) {
  const fallback = 'Something went wrong. Please try again.'
  if (typeof error === 'object' && error !== null && 'response' in error) {
    const response = error.response
    return response?.data?.error?.message ?? fallback
  }
  return fallback
}