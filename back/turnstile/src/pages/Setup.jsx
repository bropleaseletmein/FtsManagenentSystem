import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'

export default function Setup() {
  const navigate = useNavigate()
  const [step, setStep]         = useState('login')
  const [email, setEmail]       = useState('')
  const [password, setPassword] = useState('')
  const [token, setToken]       = useState('')
  const [clubs, setClubs]       = useState([])
  const [error, setError]       = useState('')
  const [loading, setLoading]   = useState(false)

  useEffect(() => {
    if (step === 'club' && token) {
      fetch('/clubs', { headers: { Authorization: `Bearer ${token}` } })
        .then(r => r.json())
        .then(setClubs)
        .catch(() => setError('Не удалось загрузить клубы'))
    }
  }, [step, token])

  const login = async (e) => {
    e.preventDefault()
    setLoading(true); setError('')
    try {
      const res = await fetch('/auth/staff/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password }),
      })
      const data = await res.json().catch(() => null)
      if (!res.ok) throw new Error(data?.error ?? 'Неверные данные')
      setToken(data.token)
      setStep('club')
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  const selectClub = (club) => {
    localStorage.setItem('turnstile_token', token)
    localStorage.setItem('turnstile_club_id', club.id)
    localStorage.setItem('turnstile_club_name', club.name)
    navigate('/', { replace: true })
  }

  return (
    <div style={styles.wrap}>
      <div style={styles.card}>
        <div style={styles.logo}>
          <h1 style={styles.logoTitle}>FitnessNetwork</h1>
          <p style={styles.logoSub}>Эмулятор турникета</p>
        </div>

        {step === 'login' && (
          <form onSubmit={login} style={styles.form}>
            <h2 style={styles.stepTitle}>Вход для сотрудников</h2>
            <div style={styles.field}>
              <label style={styles.label}>Email</label>
              <input style={styles.input} type="email" value={email}
                onChange={e => setEmail(e.target.value)} required autoFocus />
            </div>
            <div style={styles.field}>
              <label style={styles.label}>Пароль</label>
              <input style={styles.input} type="password" value={password}
                onChange={e => setPassword(e.target.value)} required />
            </div>
            {error && <div style={styles.error}>{error}</div>}
            <button style={styles.btn} type="submit" disabled={loading}>
              {loading ? 'Вход…' : 'Войти'}
            </button>
          </form>
        )}

        {step === 'club' && (
          <div>
            <h2 style={styles.stepTitle}>Выберите клуб</h2>
            <p style={{ color: '#6b7280', fontSize: 13, marginBottom: 16 }}>
              Этот турникет будет привязан к выбранному клубу
            </p>
            {clubs.length === 0 && <p style={{ color: '#6b7280' }}>Загрузка…</p>}
            <div style={{ display: 'flex', flexDirection: 'column', gap: 8 }}>
              {clubs.map(c => (
                <button key={c.id} style={styles.clubBtn} onClick={() => selectClub(c)}>
                  {c.name}
                </button>
              ))}
            </div>
            {error && <div style={{ ...styles.error, marginTop: 12 }}>{error}</div>}
          </div>
        )}
      </div>
    </div>
  )
}

const styles = {
  wrap: {
    minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
    background: '#111827', fontFamily: 'system-ui, sans-serif',
  },
  card: {
    background: '#1f2937', borderRadius: 6, padding: '36px',
    width: '100%', maxWidth: 360, border: '1px solid rgba(255,255,255,.08)',
  },
  logo: { marginBottom: 28 },
  logoTitle: { color: '#f9fafb', fontSize: 20, fontWeight: 700, margin: 0 },
  logoSub: { color: '#6b7280', fontSize: 13, margin: '3px 0 0' },
  stepTitle: { color: '#f9fafb', fontSize: 16, fontWeight: 600, marginBottom: 18 },
  form: { display: 'flex', flexDirection: 'column', gap: 0 },
  field: { marginBottom: 14 },
  label: { display: 'block', color: '#9ca3af', fontSize: 11, fontWeight: 600, marginBottom: 5, textTransform: 'uppercase', letterSpacing: '.04em' },
  input: {
    width: '100%', padding: '8px 12px', borderRadius: 4, border: '1px solid #374151',
    background: '#111827', color: '#f9fafb', fontSize: 13, outline: 'none',
    boxSizing: 'border-box',
  },
  error: {
    background: '#450a0a', border: '1px solid #7f1d1d',
    color: '#fca5a5', borderRadius: 4, padding: '9px 12px', fontSize: 13,
  },
  btn: {
    marginTop: 8, padding: '10px', borderRadius: 4, border: 'none',
    background: '#1d4ed8', color: '#fff', fontSize: 14, fontWeight: 600,
    cursor: 'pointer', width: '100%',
  },
  clubBtn: {
    display: 'block', padding: '12px 16px',
    background: '#111827', border: '1px solid #374151', borderRadius: 4,
    color: '#f9fafb', fontSize: 14, cursor: 'pointer', width: '100%', textAlign: 'left',
  },
}
