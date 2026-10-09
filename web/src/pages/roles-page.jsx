import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2, Plus, Save, ShieldCheck } from 'lucide-react'
import { useEffect, useMemo, useState } from 'react'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { rbacApi } from '@/lib/api/rbac'
import { apiErrorMessage } from '@/lib/api/types'
import { cn } from '@/lib/utils'
import { useAuthStore } from '@/store/auth'

const NEW_ROLE = '__new__'

const schema = z.object({
  code: z
    .string()
    .min(1, 'Code is required')
    .regex(/^[A-Za-z0-9_]+$/, 'Letters, digits and underscores only'),
  name: z.string().min(1, 'Name is required'),
  description: z.string().max(500, 'Max 500 characters'),
})

function PermissionMatrix({ permissions, selected, canManage, onToggle }) {
  const grouped = useMemo(() => {
    const map = new Map()
    for (const permission of permissions) {
      if (!map.has(permission.module)) map.set(permission.module, [])
      map.get(permission.module).push(permission)
    }
    return [...map.entries()].map(([module, items]) => ({ module, items }))
  }, [permissions])

  return (
    <div className="flex flex-col gap-3">
      {grouped.map(({ module, items }) => (
        <div key={module}>
          <p className="text-muted-foreground mb-1 text-xs font-semibold uppercase tracking-wide">{module}</p>
          <div className="grid gap-1 sm:grid-cols-2">
            {items.map((permission) => {
              const isChecked = selected.includes(permission.code)
              return (
                <label
                  key={permission.code}
                  className={cn(
                    'flex items-center gap-2 rounded-md border px-2.5 py-1.5 text-sm transition-colors',
                    canManage ? 'hover:bg-accent cursor-pointer' : 'cursor-default',
                    isChecked && 'border-primary/40 bg-primary/5',
                  )}
                >
                  <input
                    type="checkbox"
                    className="size-4 accent-primary"
                    checked={isChecked}
                    disabled={!canManage}
                    onChange={() => onToggle(permission.code)}
                  />
                  <span className="font-mono text-xs">{permission.code}</span>
                </label>
              )
            })}
          </div>
        </div>
      ))}
    </div>
  )
}

export function RolesPage() {
  const hasPermission = useAuthStore((state) => state.hasPermission)
  const [selectedRoleId, setSelectedRoleId] = useState(null)
  const [checked, setChecked] = useState([])

  const canView = hasPermission('role.view')
  const canManage = hasPermission('role.manage')

  const queryClient = useQueryClient()
  const invalidate = () => void queryClient.invalidateQueries({ queryKey: ['roles'] })

  const { data: roles, isLoading, isError, error } = useQuery({ queryKey: ['roles'], queryFn: rbacApi.listRoles, enabled: canView })
  const { data: permissions, isLoading: loadingPermissions } = useQuery({
    queryKey: ['permissions'],
    queryFn: rbacApi.listPermissions,
    enabled: canView,
  })

  const rolePermissions = useQuery({
    queryKey: ['rolePermissions', selectedRoleId],
    queryFn: () => rbacApi.getRolePermissions(selectedRoleId),
    enabled: canView && !!selectedRoleId && selectedRoleId !== NEW_ROLE,
  })

  useEffect(() => {
    if (selectedRoleId === NEW_ROLE) {
      setChecked([])
    } else if (rolePermissions.data) {
      setChecked([...rolePermissions.data])
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedRoleId, rolePermissions.data])

  const form = useForm({ resolver: zodResolver(schema), defaultValues: { code: '', name: '', description: '' } })

  const createMutation = useMutation({
    mutationFn: (payload) => rbacApi.createRole(payload),
    onSuccess: (role) => {
      toast.success(`Role "${role.name}" created.`)
      form.reset()
      invalidate()
      setSelectedRoleId(role.id)
    },
    onError: (err) => toast.error(apiErrorMessage(err)),
  })

  const updateMutation = useMutation({
    mutationFn: ({ roleId, permissionCodes }) => rbacApi.updateRolePermissions(roleId, permissionCodes),
    onSuccess: () => {
      toast.success('Permissions saved.')
      invalidate()
    },
    onError: (err) => toast.error(apiErrorMessage(err)),
  })

  function toggle(code) {
    setChecked((current) => (current.includes(code) ? current.filter((c) => c !== code) : [...current, code]))
  }

  function handleSave() {
    if (selectedRoleId === NEW_ROLE) {
      const values = form.getValues()
      createMutation.mutate({ ...values, permissionCodes: [...checked] })
    } else if (selectedRoleId) {
      updateMutation.mutate({ roleId: selectedRoleId, permissionCodes: [...checked] })
    }
  }

  if (!canView) {
    return <p className="text-muted-foreground">You do not have access to this module.</p>
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Roles &amp; Permissions</h1>
          <p className="text-muted-foreground text-sm">Define roles and grant permission bundles.</p>
        </div>
        {canManage && (
          <Button
            variant={selectedRoleId === NEW_ROLE ? 'secondary' : 'default'}
            onClick={() => setSelectedRoleId(NEW_ROLE)}
          >
            <Plus className="size-4" /> New role
          </Button>
        )}
      </div>

      {!(isLoading || isError) && roles && (
        <div className="grid gap-4 lg:grid-cols-[280px_1fr]">
          <Card className="h-fit">
            <CardContent className="p-3">
              {roles.length === 0 && <p className="text-muted-foreground p-4 text-sm">No roles yet.</p>}
              <div className="flex flex-col gap-1">
                {roles.map((role) => (
                  <button
                    key={role.id}
                    type="button"
                    onClick={() => setSelectedRoleId(role.id)}
                    className={cn(
                      'flex items-center justify-between rounded-md px-3 py-2 text-left text-sm transition-colors',
                      selectedRoleId === role.id ? 'bg-primary/10 text-primary' : 'hover:bg-accent',
                    )}
                  >
                    <span className="flex items-center gap-2">
                      <ShieldCheck className="size-4" />
                      <span className="font-medium">{role.name}</span>
                    </span>
                    <span className="text-muted-foreground text-xs">
                      {role.isSystem ? 'system' : role.permissionCount}
                    </span>
                  </button>
                ))}
              </div>
            </CardContent>
          </Card>

          <Card>
            <CardHeader className="flex-row items-center justify-between gap-2">
              <CardTitle className="text-base">
                {selectedRoleId === NEW_ROLE ? 'New role' : roles.find((r) => r.id === selectedRoleId)?.name ?? 'Role'}
              </CardTitle>
              {canManage && (
                <Button size="sm" onClick={handleSave} disabled={createMutation.isPending || updateMutation.isPending}>
                  {(createMutation.isPending || updateMutation.isPending) && <Loader2 className="size-4 animate-spin" />}
                  <Save className="size-4" /> Save
                </Button>
              )}
            </CardHeader>
            <CardContent>
              {selectedRoleId === NEW_ROLE && (
                <form className="mb-6 grid gap-4 sm:grid-cols-2" onSubmit={(e) => e.preventDefault()}>
                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor="code">Code</Label>
                    <Input id="code" placeholder="FIELD_MANAGER" {...form.register('code')} />
                    {form.formState.errors.code && <p className="text-destructive text-xs">{form.formState.errors.code.message}</p>}
                  </div>
                  <div className="flex flex-col gap-1.5">
                    <Label htmlFor="name">Name</Label>
                    <Input id="name" placeholder="Field Manager" {...form.register('name')} />
                    {form.formState.errors.name && <p className="text-destructive text-xs">{form.formState.errors.name.message}</p>}
                  </div>
                  <div className="flex flex-col gap-1.5 sm:col-span-2">
                    <Label htmlFor="description">Description</Label>
                    <Input id="description" placeholder="Optional description" {...form.register('description')} />
                  </div>
                </form>
              )}

              {loadingPermissions && !permissions && (
                <div className="flex items-center gap-2 text-sm text-muted-foreground">
                  <Loader2 className="size-4 animate-spin" /> Loading permissions…
                </div>
              )}

              {permissions && selectedRoleId && (
                <PermissionMatrix permissions={permissions} selected={checked} canManage={canManage} onToggle={toggle} />
              )}

              {!selectedRoleId && (
                <p className="text-muted-foreground text-sm">Select a role to view or edit its permissions.</p>
              )}
            </CardContent>
          </Card>
        </div>
      )}

      {isLoading && (
        <div className="text-muted-foreground flex items-center gap-2 text-sm">
          <Loader2 className="size-4 animate-spin" /> Loading roles…
        </div>
      )}
      {isError && <p className="text-destructive text-sm">{apiErrorMessage(error)}</p>}
    </div>
  )
}