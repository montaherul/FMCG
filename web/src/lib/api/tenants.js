import { api } from './client'

const base = '/Tenants'

export async function listTenants(page, pageSize) {
  const { data } = await api.get(base, { params: { page, pageSize } })
  if (!data.data) {
    throw new Error(data.error?.message ?? 'Failed to load tenants')
  }
  return data.data
}

export async function provisionTenant(request) {
  const { data } = await api.post(base, request)
  if (!data.data) {
    throw new Error(data.error?.message ?? 'Failed to create tenant')
  }
  return data.data
}

export async function updateTenantStatus(id, status) {
  const { data } = await api.patch(`${base}/${id}/status`, { status })
  if (!data.data) {
    throw new Error(data.error?.message ?? 'Failed to update tenant status')
  }
  return data.data
}