import { api } from './client'

export const rbacApi = {
  async listRoles() {
    const { data } = await api.get('/Roles')
    return data.data ?? []
  },

  async createRole(payload) {
    const { data } = await api.post('/Roles', payload)
    return data.data
  },

  async updateRolePermissions(roleId, permissionCodes) {
    const { data } = await api.put(`/Roles/${roleId}/permissions`, { permissionCodes })
    return data.data
  },

  async getRolePermissions(roleId) {
    const { data } = await api.get(`/Roles/${roleId}/permissions`)
    return data.data ?? []
  },

  async listPermissions() {
    const { data } = await api.get('/permissions')
    return data.data ?? []
  },

  async listUsers() {
    const { data } = await api.get('/Users')
    return data.data ?? []
  },

  async createUser(payload) {
    const { data } = await api.post('/Users', payload)
    return data.data
  },

  async assignRole(userId, roleId) {
    const { data } = await api.post(`/Users/${userId}/roles`, { roleId })
    return data.data
  },
}