import { LayoutDashboard, LogOut, Moon, ShieldCheck, Sun, Users } from 'lucide-react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { Button } from '@/components/ui/button'
import { useTheme } from '@/components/theme-provider'
import { cn } from '@/lib/utils'
import { useAuthStore } from '@/store/auth'

interface NavItem {
  to: string
  label: string
  icon: typeof LayoutDashboard
  permission?: string
}

const navItems: NavItem[] = [
  { to: '/dashboard', label: 'Dashboard', icon: LayoutDashboard },
  { to: '/users', label: 'Users', icon: Users, permission: 'user.view' },
  { to: '/roles', label: 'Roles & Permissions', icon: ShieldCheck, permission: 'role.view' },
]

export function AppLayout() {
  const { theme, toggle } = useTheme()
  const user = useAuthStore((state) => state.user)
  const hasPermission = useAuthStore((state) => state.hasPermission)
  const logout = useAuthStore((state) => state.logout)
  const navigate = useNavigate()

  const visibleItems = navItems.filter((item) => !item.permission || hasPermission(item.permission))

  async function handleLogout() {
    await logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="bg-background flex min-h-svh">
      <aside className="bg-sidebar text-sidebar-foreground hidden w-64 flex-col border-r md:flex">
        <div className="flex h-14 items-center gap-2 border-b px-4 font-semibold">
          <span className="bg-primary text-primary-foreground flex size-7 items-center justify-center rounded-md text-xs">
            TS
          </span>
          Tobacco SaaS
        </div>
        <nav className="flex flex-1 flex-col gap-1 p-3">
          {visibleItems.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                cn(
                  'flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                  isActive
                    ? 'bg-sidebar-accent text-sidebar-accent-foreground'
                    : 'text-muted-foreground hover:bg-sidebar-accent/60',
                )
              }
            >
              <item.icon className="size-4" />
              {item.label}
            </NavLink>
          ))}
        </nav>
      </aside>

      <div className="flex flex-1 flex-col">
        <header className="bg-background/80 flex h-14 items-center justify-between border-b px-4 backdrop-blur">
          <div className="text-sm font-medium">
            {user?.isPlatformScope ? 'Platform' : 'Tenant'} workspace
          </div>
          <div className="flex items-center gap-3">
            <span className="text-muted-foreground hidden text-sm sm:inline">{user?.fullName}</span>
            <Button variant="ghost" size="icon" onClick={toggle} title="Toggle theme">
              {theme === 'dark' ? <Sun className="size-4" /> : <Moon className="size-4" />}
            </Button>
            <Button variant="outline" size="sm" onClick={handleLogout}>
              <LogOut className="size-4" />
              Sign out
            </Button>
          </div>
        </header>

        <main className="flex-1 p-4 md:p-6">
          <Outlet />
        </main>
      </div>
    </div>
  )
}
