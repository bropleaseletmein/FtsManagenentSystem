import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { api } from '../api'
import { useAuth } from '../context/AuthContext'

export default function Login() {
  const { login } = useAuth()
  const navigate   = useNavigate()
  const [email, setEmail]       = useState('')
  const [password, setPassword] = useState('')
  const [error, setError]       = useState('')
  const [loading, setLoading]   = useState(false)

  const handleSubmit = async e => {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const data = await api.post('/auth/client/login', { email: email.trim(), password })
      await login(data.token)
      navigate('/', { replace: true })
    } catch (err) {
      setError(
        err.message === 'HTTP 401'
          ? 'Неверный email или пароль'
          : err.message
      )
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-logo">
          <h1>FitnessNetwork</h1>
          <p>Личный кабинет клиента</p>
        </div>

        <form onSubmit={handleSubmit}>
          <div className="form-group">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              placeholder="your@email.com"
              value={email}
              onChange={e => setEmail(e.target.value)}
              autoComplete="username"
              required
            />
          </div>
          <div className="form-group">
            <label htmlFor="password">Пароль</label>
            <input
              id="password"
              type="password"
              placeholder="••••••••"
              value={password}
              onChange={e => setPassword(e.target.value)}
              autoComplete="current-password"
              required
            />
          </div>
          {error && <div className="error-box">{error}</div>}
          <button
            type="submit"
            className="btn btn-primary btn-full"
            style={{ marginTop: 16 }}
            disabled={loading}
          >
            {loading ? 'Вход…' : 'Войти'}
          </button>
        </form>
      </div>
    </div>
  )
}
