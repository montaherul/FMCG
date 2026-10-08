import { createContext, useContext, useEffect, useState } from 'react'

const STORAGE_KEY = 'tobacco.theme'
const ThemeContext = createContext(null)

function applyTheme(theme) {
  const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches
  const isDark = theme === 'dark' || (theme === 'system' && prefersDark)
  document.documentElement.classList.toggle('dark', isDark)
}

export function ThemeProvider({ children }) {
  const [theme, setTheme] = useState(() => localStorage.getItem(STORAGE_KEY) ?? 'system')

  useEffect(() => {
    localStorage.setItem(STORAGE_KEY, theme)
    applyTheme(theme)
  }, [theme])

  const value = {
    theme,
    setTheme,
    toggle: () => setTheme(document.documentElement.classList.contains('dark') ? 'light' : 'dark'),
  }

  return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
}

export function useTheme() {
  const context = useContext(ThemeContext)
  if (!context) {
    throw new Error('useTheme must be used within a ThemeProvider')
  }
  return context
}