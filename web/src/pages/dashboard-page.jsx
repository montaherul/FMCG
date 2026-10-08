import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useAuthStore } from '@/store/auth'

export function DashboardPage() {
  const user = useAuthStore((state) => state.user)

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Dashboard</h1>
        <p className="text-muted-foreground text-sm">Welcome back, {user?.fullName}.</p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <Card>
          <CardHeader>
            <CardDescription>Plane</CardDescription>
            <CardTitle className="text-lg">{user?.isPlatformScope ? 'Platform' : 'Tenant'}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <CardDescription>Roles</CardDescription>
            <CardTitle className="text-lg">{user?.roles.length ?? 0}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <CardDescription>Permissions</CardDescription>
            <CardTitle className="text-lg">{user?.permissions.length ?? 0}</CardTitle>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <CardDescription>Email</CardDescription>
            <CardTitle className="truncate text-lg">{user?.email}</CardTitle>
          </CardHeader>
        </Card>
      </div>

      <Card>
        <CardHeader>
          <CardTitle>Your permissions</CardTitle>
          <CardDescription>The actions your roles currently allow.</CardDescription>
        </CardHeader>
        <CardContent>
          <div className="flex flex-wrap gap-2">
            {(user?.permissions ?? []).map((permission) => (
              <span key={permission} className="bg-muted text-muted-foreground rounded-md px-2 py-1 font-mono text-xs">
                {permission}
              </span>
            ))}
            {user?.permissions.length === 0 && <span className="text-muted-foreground text-sm">No permissions assigned.</span>}
          </div>
        </CardContent>
      </Card>
    </div>
  )
}