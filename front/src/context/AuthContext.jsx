import { createContext, useContext, useState, useEffect, useCallback } from 'react'
import { api, parseJwt } from '../api'

const AuthCtx = createContext(null)

export function AuthProvider({ children }) {
  const [token, setToken] = useState(() => localStorage.getItem('fn_token'))
  const [client, setClient] = useState(null)
  const [subscriptions, setSubscriptions] = useState([])
  const [loading, setLoading] = useState(!!localStorage.getItem('fn_token'))

  const logout = useCallback(() => {
    localStorage.removeItem('fn_token')
    setToken(null)
    setClient(null)
    setSubscriptions([])
  }, [])

  const loadProfile = useCallback(async (t) => {
    try {
      const payload = parseJwt(t)
      const [me, subs] = await Promise.all([
        api.get('/clients/me'),
        api.get(`/clients/${payload.sub}/subscriptions`),
      ])
      setClient(me)
      setSubscriptions(subs)
    } catch {
      logout()
    } finally {
      setLoading(false)
    }
  }, [logout])

  useEffect(() => {
    if (token) loadProfile(token)
    else setLoading(false)
  }, [token, loadProfile])

  const login = useCallback(async (newToken) => {
    localStorage.setItem('fn_token', newToken)
    setToken(newToken)
    // loadProfile fires via the useEffect above
  }, [])

  const refreshSubs = useCallback(async () => {
    const t = localStorage.getItem('fn_token')
    if (!t) return
    const payload = parseJwt(t)
    const subs = await api.get(`/clients/${payload.sub}/subscriptions`).catch(() => [])
    setSubscriptions(subs)
  }, [])

  return (
    <AuthCtx.Provider value={{ token, client, subscriptions, loading, login, logout, refreshSubs }}>
      {children}
    </AuthCtx.Provider>
  )
}

export const useAuth = () => useContext(AuthCtx)
