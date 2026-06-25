import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { api, parseJwt } from '../api'
import Badge from '../components/Badge'

function fmtDate(dt) {
  if (!dt) return '—'
  return new Date(dt).toLocaleDateString('ru-RU', { day: '2-digit', month: 'long', year: 'numeric' })
}

export default function Dashboard() {
  const { client, token, refreshSubs } = useAuth()
  const [subs, setSubs]       = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError]     = useState('')
  const navigate = useNavigate()

  useEffect(() => {
    const payload = parseJwt(token)
    api.get(`/clients/${payload.sub}/subscriptions`)
      .then(data => { setSubs(data); refreshSubs() })
      .catch(err => setError(err.message))
      .finally(() => setLoading(false))
  }, []) // eslint-disable-line

  const active = subs.filter(s => s.status === 'active')
  const frozen = subs.filter(s => s.status === 'frozen')

  if (loading) return <div className="loading-screen">Загрузка…</div>
  if (error)   return <div className="empty-state"><p>Ошибка: {error}</p></div>

  return (
    <div>
      <div className="page-header">
        <div>
          <h2>Добро пожаловать{client ? `, ${client.firstName}` : ''}!</h2>
          <p>
            {active.length > 0
              ? `Активных абонементов: ${active.length}`
              : 'Нет активных абонементов'}
          </p>
        </div>
      </div>

      <div className="stats-row">
        <div className="stat-card">
          <div className="stat-label">Всего абонементов</div>
          <div className="stat-value">{subs.length}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Активных</div>
          <div className="stat-value" style={{ color: 'var(--success)' }}>{active.length}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Замороженных</div>
          <div className="stat-value" style={{ color: '#1d4ed8' }}>{frozen.length}</div>
        </div>
      </div>

      <div className="section-title">Мои абонементы</div>
      {subs.length === 0 ? (
        <div className="empty-state">
          <p>Нет абонементов</p>
          <button className="btn btn-primary" style={{ marginTop: 14 }}
            onClick={() => navigate('/plans')}>
            Смотреть тарифы
          </button>
        </div>
      ) : (
        <div className="cards-grid">
          {subs.map(s => (
            <div key={s.id} className="sub-card">
              <div className="sub-name">{s.subscriptionType?.name ?? '—'}</div>
              <div style={{ marginBottom: 10 }}><Badge status={s.status} /></div>
              <div className="sub-detail">Начало: <b>{fmtDate(s.startedAt)}</b></div>
              {s.expiresAt && (
                <div className="sub-detail">Действует до: <b>{fmtDate(s.expiresAt)}</b></div>
              )}
              <div className="sub-detail">
                Посещений: <b>{s.visitsLeft != null ? s.visitsLeft : 'безлимит'}</b>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
