import { zodResolver } from '@hookform/resolvers/zod'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Building2, ChevronLeft, ChevronRight, Loader2, Plus } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { toast } from 'sonner'
import { z } from 'zod'
import { listTenants, provisionTenant, updateTenantStatus } from '@/lib/api/tenants'
import { apiErrorMessage, TenantStatus } from '@/lib/api/types'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { cn } from '@/lib/utils'
import { useAuthStore } from '@/store/auth'

const PAGE_SIZE = 10

const PLAN_CODES = ['FREE', 'STARTER', 'BUSINESS', 'ENTERPRISE']

const STATUS_LABEL = {
  [TenantStatus.Active]: 'Active',
  [TenantStatus.Suspended]: 'Suspended',
  [TenantStatus.Trial]: 'Trial',
  [TenantStatus.Terminated]: 'Terminated',
}

const STATUS_BADGE = {
  [TenantStatus.Active]: 'bg-emerald-500/15 text-emerald-600',
  [TenantStatus.Suspended]: 'bg-rose-500/15 text-rose-600',
  [TenantStatus.Trial]: 'bg-amber-500/15 text-amber-600',
  [TenantStatus.Terminated]: 'bg-muted text-muted-foreground',
}

const schema = z.object({
  name: z.string().min(2, 'Name is required'),
  slug: z.string().min(2, 'Slug is required').regex(/^[a-z0-9-]+$/, 'Lowercase letters, numbers and dashes only'),
  adminEmail: z.string().email('Enter a valid email'),
  adminFullName: z.string().min(2, 'Admin name is required'),
  planCode: z.enum(['FREE', 'STARTER', 'BUSINESS', 'ENTERPRISE']),
})

export function TenantsPage() {
  const hasPermission = useAuthStore((state) => state.hasPermission)
  const [page, setPage] = useState(1)
  const [showForm, setShowForm] = useState(false)

  const canView = hasPermission('platform.tenant.view')
  const canCreate = hasPermission('platform.tenant.create')
  const canSuspend = hasPermission('platform.tenant.suspend')

  const queryClient = useQueryClient()
  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['tenants', page],
    queryFn: () => listTenants(page, PAGE_SIZE),
    enabled: canView,
  })

  const invalidate = () => {
    void queryClient.invalidateQueries({ queryKey: ['tenants'] })
  }

  const statusMutation = useMutation({
    mutationFn: ({ id, status }) => updateTenantStatus(id, status),
    onSuccess: () => invalidate(),
    onError: (err) => toast.error(apiErrorMessage(err)),
  })

  const form = useForm({
    resolver: zodResolver(schema),
    defaultValues: { name: '', slug: '', adminEmail: '', adminFullName: '', planCode: 'BUSINESS' },
  })

  const createMutation = useMutation({
    mutationFn: (values) => provisionTenant(values),
    onSuccess: (result) => {
      toast.success(
        `Tenant "${result.tenant.name}" created. Admin ${result.adminEmail} — interim password: ${result.temporaryPassword}`,
        { duration: 12000 },
      )
      form.reset()
      setShowForm(false)
      setPage(1)
      invalidate()
    },
    onError: (err) => toast.error(apiErrorMessage(err)),
  })

  function suggestSlug(name) {
    const slug = name
      .toLowerCase()
      .trim()
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '')
    form.setValue('slug', slug, { shouldValidate: true })
  }

  if (!canView) {
    return <p className="text-muted-foreground">You do not have access to this module.</p>
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold">Tenants</h1>
          <p className="text-muted-foreground text-sm">Platform level tenant administration.</p>
        </div>
        {canCreate && (
          <Button onClick={() => setShowForm((value) => !value)}>
            {showForm ? <ChevronLeft className="size-4" /> : <Plus className="size-4" />}
            {showForm ? 'Close form' : 'New tenant'}
          </Button>
        )}
      </div>

      {canCreate && showForm && (
        <Card>
          <CardHeader>
            <CardTitle className="text-base">Provision a new tenant</CardTitle>
          </CardHeader>
          <CardContent>
            <form
              className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3"
              onSubmit={form.handleSubmit((values) => createMutation.mutate(values))}
              noValidate
            >
              <div className="flex flex-col gap-1.5">
                <Label htmlFor="name">Company name</Label>
                <Input
                  id="name"
                  placeholder="Acme Distributors"
                  {...form.register('name', {
                    onBlur: () => {
                      if (!form.getValues('slug')) suggestSlug(form.getValues('name'))
                    },
                  })}
                />
                {form.formState.errors.name && (
                  <p className="text-destructive text-xs">{form.formState.errors.name.message}</p>
                )}
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="slug">Slug</Label>
                <Input id="slug" placeholder="acme-distributors" {...form.register('slug')} />
                {form.formState.errors.slug && (
                  <p className="text-destructive text-xs">{form.formState.errors.slug.message}</p>
                )}
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="plan">Plan</Label>
                <select id="plan" className="h-10 rounded-md border bg-background px-3" {...form.register('planCode')}>
                  {PLAN_CODES.map((plan) => (
                    <option key={plan} value={plan}>
                      {plan}
                    </option>
                  ))}
                </select>
              </div>

              <div className="flex flex-col gap-1.5">
                <Label htmlFor="adminFullName">Admin name</Label>
                <Input id="adminFullName" placeholder="Jane Manager" {...form.register('adminFullName')} />
                {form.formState.errors.adminFullName && (
                  <p className="text-destructive text-xs">{form.formState.errors.adminFullName.message}</p>
                )}
              </div>

              <div className="flex flex-col gap-1.5 sm:col-span-2 lg:col-span-2">
                <Label htmlFor="adminEmail">Admin email</Label>
                <Input id="adminEmail" type="email" placeholder="manager@acme.com" {...form.register('adminEmail')} />
                {form.formState.errors.adminEmail && (
                  <p className="text-destructive text-xs">{form.formState.errors.adminEmail.message}</p>
                )}
              </div>

              <div className="flex items-end">
                <Button type="submit" disabled={createMutation.isPending} className="w-full">
                  {createMutation.isPending && <Loader2 className="size-4 animate-spin" />}
                  Provision tenant
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

          {data && (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="text-muted-foreground border-b text-left text-xs uppercase tracking-wide">
                    <th className="px-4 py-3">Company</th>
                    <th className="px-4 py-3">Plan</th>
                    <th className="px-4 py-3">Status</th>
                    <th className="px-4 py-3">Created</th>
                    {canSuspend && <th className="px-4 py-3">Action</th>}
                  </tr>
                </thead>
                <tbody>
                  {data.items.length === 0 && (
                    <tr>
                      <td colSpan={canSuspend ? 5 : 4} className="text-muted-foreground px-4 py-8 text-center">
                        No tenants yet.
                      </td>
                    </tr>
                  )}
                  {data.items.map((tenant) => (
                    <tr key={tenant.id} className="border-b last:border-0">
                      <td className="px-4 py-3">
                        <div className="flex items-center gap-3">
                          <span className="bg-primary/10 text-primary flex size-8 items-center justify-center rounded-md">
                            <Building2 className="size-4" />
                          </span>
                          <div>
                            <p className="font-medium">{tenant.name}</p>
                            <p className="text-muted-foreground font-mono text-xs">{tenant.slug}</p>
                          </div>
                        </div>
                      </td>
                      <td className="px-4 py-3 font-mono text-xs">{tenant.planCode ?? '—'}</td>
                      <td className="px-4 py-3">
                        <span className={cn('inline-flex rounded-full px-2 py-0.5 text-xs font-medium', STATUS_BADGE[tenant.status])}>
                          {STATUS_LABEL[tenant.status]}
                        </span>
                      </td>
                      <td className="text-muted-foreground px-4 py-3 text-xs">{new Date(tenant.createdAt).toLocaleString()}</td>
                      {canSuspend && (
                        <td className="px-4 py-3">
                          {tenant.status !== TenantStatus.Terminated && (
                            <select
                              className="h-8 rounded-md border bg-background px-2 text-xs"
                              value={tenant.status}
                              onChange={(event) =>
                                statusMutation.mutate({ id: tenant.id, status: Number(event.target.value) })
                              }
                            >
                              <option value={TenantStatus.Active}>Set Active</option>
                              <option value={TenantStatus.Suspended}>Suspend</option>
                            </select>
                          )}
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {data && data.totalPages > 1 && (
            <div className="flex items-center justify-between border-t px-4 py-3">
              <span className="text-muted-foreground text-xs">
                {data.total} tenant{data.total === 1 ? '' : 's'} · page {data.page} of {data.totalPages}
              </span>
              <div className="flex items-center gap-2">
                <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage(page - 1)}>
                  <ChevronLeft className="size-4" /> Prev
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={page >= data.totalPages}
                  onClick={() => setPage(page + 1)}
                >
                  Next <ChevronRight className="size-4" />
                </Button>
              </div>
            </div>
          )}
        </CardContent>
      </Card>
    </div>
  )
}