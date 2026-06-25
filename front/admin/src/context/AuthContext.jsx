import { createContext, useContext, useState, useEffect } from 'react'
import { api } from '../api'

const AuthContext = createContext(null)
export const useAuth = () => useContext(AuthContext)

function parseJwt(token) {
  try {
    return JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')))
  } catch { return null }
}

export function AuthProvider({ children }) {
  const [token, setToken]   = useState(() => localStorage.getItem('admin_token'))
  const [staff, setStaff]   = useState(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!token) { setStaff(null); setLoading(false); return }
    const payload = parseJwt(token)
    if (!payload || payload.exp * 1000 < Date.now()) {
      logout(); return
    }
    api.get('/staff/me')
      .then(setStaff)
      .catch(logout)
      .finally(() => setLoading(false))
  }, [token]) // eslint-disable-line

  const login = (tok) => {
    localStorage.setItem('admin_token', tok)
    setToken(tok)
  }

  const logout = () => {
    localStorage.removeItem('admin_token')
    setToken(null)
    setStaff(null)
    setLoading(false)
  }

  const roles = staff?.roles?.map(r => r.role) ?? []
  const isAdmin   = roles.includes('admin')
  const isTrainer = roles.includes('trainer')

  return (
    <AuthContext.Provider value={{ token, staff, loading, login, logout, isAdmin, isTrainer }}>
      {children}
    </AuthContext.Provider>
  )
}
