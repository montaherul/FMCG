import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Loader2, Plus, UserRound } from 'lucide-react'
import { useState } from 'react'
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

const USER_STATUS = {
  0: { label: 'Active', badge: 'bg-emerald-500/15 text-emerald-600' },
  1: { label: 'Inactive', badge: 'bg-muted text-muted-foreground' },
  2: { label: 'Suspended', badge: 'bg-rose-500/15 text-rose-600' },
  3: { label: 'Terminated', badge: 'bg-destructive/10 text-destructive' },
}

const schema = z.object({
  email: z.string().email('Enter a valid email'),
  fullName: z.string().min(2, 'Name is required'),
  password: z.string().min(8, 'Min 8 characters').or(z.literal('')),
})

export function UsersPage() {
  const hasPermission = useAuthStore((state) => state.hasPermission)
  const [selectedRoles, setSelectedRoles] = useState([])
  const [showForm, setShowForm] = useState(false)

  const canView = hasPermission('user.view')
  const canCreate = hasPermission('user.create')
  const canAssign = hasPermission('user.roles')

  const queryClient = useQueryClient()
  const invalidate = () => void queryClient.invalidateQueries({ queryKey: ['users'] })

  const { data: users, isLoading, isError, error } = useQuery({ queryKey: ['users'], queryFn: rbacApi.listUsers, enabled: canView })
  const { data: roles, isLoading: loadingRoles } = useQuery({ queryKey: ['roles'], queryFn: rbacApi.listRoles, enabled: canView })

  const form = useForm({ resolver: zodResolver(schema), defaultValues: { email: '', fullName: '', password: '' } })

  const createMutation = useMutation({
    mutationFn: (payload) => rbacApi.createUser(payload),
    onSuccess: (result) => {
      toast.success(
        result.temporaryPassword
          ? `User created. Interim password: ${result.temporaryPassword}`
          : 'User created.',
        { duration: 12000 },
      )
      form.reset()
      setSelectedRoles([])
      setShowForm(false)
      invalidate()
    },
    onError: (err) => toast.error(apiErrorMessage(err)),
  })

  const assignMutation = useMutation({
    mutationFn: ({ userId, roleId }) => rbacApi.assignRole(userId, roleId),
    onSuccess: () => {
      toast.success('Role assigned.')
      invalidate()
    },
    onError: (err) => toast.error(apiErrorMessage(err)),
  })

  function toggleRole(roleId) {
    setSelectedRoles((current) =>
      current.includes(roleId) ? current.filter((id) => id !== roleId) : [...current, roleId],
    )
  }

  if (!canView) {
    return <p className="text-muted-foreground">You do not have access to this module.</p>
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Users</h1>
          <p className="text-muted-foreground text-sm">Manage tenant users, roles and scope.</p>
        </div>
        {canCreate && (
          <Button onClick={() => setShowForm((value) => !value)}>
            <Plus className="size-4" /> {showForm ? 'Close form' : 'New user'}
          </Button>
        )}
      </div>

      {canCreate && showForm && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Create a user</CardTitle>
          </CardHeader>
          <CardContent>
            <form
              className="grid gap-4 sm:grid-cols-2"
              onSubmit={form.handleSubmit((values) =>
                createMutation.mutate({
                  email: values.email,
                  fullName: values.fullName,
                  password: values.password || null,
                  roleIds: selectedRoles,
                }),
              )}
              noValidate
            >
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="fullName">Full name</Label>
                <Input id="fullName" placeholder="Jane Doe" {...form.register('fullName')} />
                {form.formState.errors.fullName && (
                  <p className="text-destructive text-xs">{form.formState.errors.fullName.message}</p>
                )}
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="email">Email</Label>
                <Input id="email" type="email" placeholder="jane@acme.com" {...form.register('email')} />
                {form.formState.errors.email && <p className="text-destructive text-xs">{form.formState.errors.email.message}</p>}
              </div>
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="password">Password (optional)</Label>
                <Input id="password" type="password" placeholder="Leave empty to generate" {...form.register('password')} />
                {form.formState.errors.password && <p className="text-destructive text-xs">{form.formState.errors.password.message}</p>}
              </div>

              <div className="flex flex-col gap-1.5 sm:col-span-2">
                <Label>Roles</Label>
                {loadingRoles && !roles ? (
                  <span className="text-muted-foreground text-sm">Loading roles…</span>
                ) : (
                  <div className="flex flex-wrap gap-2">
                    {(roles ?? []).map((role) => (
                      <button
                        key={role.id}
                        type="button"
                        onClick={() => toggleRole(role.id)}
                        className={cn(
                          'rounded-md border px-3 py-1.5 text-sm transition-colors',
                          selectedRoles.includes(role.id)
                            ? 'border-primary/40 bg-primary/10 text-primary'
                            : 'hover:bg-accent',
                        )}
                      >
                        {role.name}
                      </button>
                    ))}
                  </div>
                )}
              </div>

              <div className="flex items-end sm:col-span-2">
                <Button type="submit" disabled={createMutation.isPending}>
                  {createMutation.isPending && <Loader2 className="size-4 animate-spin" />}
                  Create user
                </Button>
              </div>
            </form>
          </CardContent>
        </Card>
      )}

      <Card>
        <CardContent className="p-0">
          {isLoading && (
            <div className="flex items-center justify-center gap-2 p-10 text-sm text-muted-foreground">
              <Loader2 className="size-4 animate-spin" /> Loading…
            </div>
          )}

          {isError && <p className="p-10 text-sm text-destructive">{apiErrorMessage(error)}</p>}

          {users && (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-muted-foreground border-b text-left text-xs uppercase tracking-wide">
                    <th className="px-4 py-3">User</th>
                    <th className="px-4 py-3">Status</th>
                    <th className="px-4 py-3">Roles</th>
                    {canAssign && <th className="px-4 py-3">Assign role</th>}
                  </tr>
                </thead>
                <tbody>
                  {users.length === 0 && (
                    <tr>
                      <td colSpan={canAssign ? 4 : 3} className="text-muted-foreground px-4 py-8 text-center">
                        No users yet.
                      </td>
                    </tr>
                  )}
                  {users.map((user) => (
                    <tr key={user.id} className="border-b last:border-0">
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-3">
                          <span className="bg-primary/10 text-primary flex size-8 items-center justify-center rounded-md">
                            <UserRound className="size-4" />
                          </span>
                          <div>
                            <p className="font-medium">{user.fullName}</p>
                            <p className="text-muted-foreground text-xs">{user.email}</p>
                          </div>
                        </div>
                      </td>
                      <td className="px-4 py-3">
                        <span
                          className={cn(
                            'inline-flex rounded-full px-2 py-0.5 text-xs font-medium',
                            USER_STATUS[user.status].badge,
                          )}
                        >
                          {USER_STATUS[user.status].label}
                        </span>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex flex-wrap gap-1">
                          {user.roles.length === 0 && <span className="text-muted-foreground text-xs">—</span>}
                          {user.roles.map((role) => (
                            <span
                              key={role}
                              className="bg-muted text-muted-foreground rounded-md px-2 py-0.5 font-mono text-xs"
                            >
                              {role}
                            </span>
                          ))}
                        </div>
                      </td>
                      {canAssign && (
                        <td className="px-4 py-3">
                          <select
                            className="h-8 rounded-md border bg-background px-2 text-xs"
                            defaultValue=""
                            onChange={(event) => {
                              if (event.target.value) {
                                assignMutation.mutate({ userId: user.id, roleId: event.target.value })
                                event.target.value = ''
                              }
                            }}
                          >
                            <option value="" disabled>
                              Add role…
                            </option>
                            {(roles ?? []).map((role) => (
                              <option key={role.id} value={role.id}>
                                {role.name}
                              </option>
                            ))}
                          </select>
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}