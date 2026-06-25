import { useEffect, useState } from 'react'
import { api } from '../api'

function fmt(dt) {
  if (!dt) return '—'
  const d = new Date(dt)
  return d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' })
    + ' ' + d.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
}

const METHOD_LABELS = { card: 'Карта', qr: 'QR-код', bracelet: 'Браслет' }

export default function Visits() {
  const [visits, setVisits]   = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError]     = useState('')

  useEffect(() => {
    api.get('/clients/me/visits')
      .then(data => setVisits(data ?? []))
      .catch(e => setError(String(e)))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div className="page-container">
      <div className="page-header">
        <h1>История посещений</h1>
        <p>Ваши визиты в клубы</p>
      </div>

      {loading && <p className="muted">Загрузка…</p>}
      {error   && <p className="error-text">{error}</p>}

      {!loading && !error && visits.length === 0 && (
        <div className="empty-state">
          <p>Посещений пока нет</p>
        </div>
      )}

      {!loading && !error && visits.length > 0 && (
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Клуб</th>
                <th>Вход</th>
                <th>Выход</th>
                <th>Способ</th>
                <th>Статус</th>
              </tr>
            </thead>
            <tbody>
              {visits.map(v => {
                const inside = !v.exitedAt
                return (
                  <tr key={v.id}>
                    <td><b>{v.club?.name ?? '—'}</b></td>
                    <td style={{ whiteSpace: 'nowrap' }}>{fmt(v.enteredAt)}</td>
                    <td style={{ whiteSpace: 'nowrap' }}>{fmt(v.exitedAt)}</td>
                    <td>{METHOD_LABELS[v.entryMethod] ?? v.entryMethod}</td>
                    <td>
                      <span className={`status-badge ${inside ? 'status-booked' : 'status-completed'}`}>
                        {inside ? 'Внутри' : 'Завершено'}
                      </span>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
