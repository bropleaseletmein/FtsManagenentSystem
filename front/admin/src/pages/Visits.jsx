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
  const [clubs, setClubs]     = useState([])
  const [loading, setLoading] = useState(true)
  const [clubId, setClubId]   = useState('')
  const [from, setFrom]       = useState(() => {
    const d = new Date(); d.setDate(d.getDate() - 7)
    return d.toISOString().slice(0, 10)
  })
  const [to, setTo] = useState(() => new Date().toISOString().slice(0, 10))

  useEffect(() => {
    api.get('/clubs').then(c => setClubs(c ?? []))
  }, [])

  const load = () => {
    setLoading(true)
    const params = new URLSearchParams()
    if (clubId) params.set('clubId', clubId)
    if (from)   params.set('from', new Date(from).toISOString())
    if (to) {
      const t = new Date(to); t.setHours(23, 59, 59, 999)
      params.set('to', t.toISOString())
    }
    api.get(`/visits?${params}`)
      .then(v => setVisits(v ?? []))
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, []) // eslint-disable-line

  const recordExit = async (id) => {
    await api.put(`/visits/${id}/exit`, {})
    load()
  }

  return (
    <div>
      <div className="page-header">
        <div><h2>История посещений</h2><p>Журнал входов и выходов</p></div>
      </div>

      <div className="table-wrap">
        <div className="table-toolbar" style={{ flexWrap: 'wrap', gap: 10 }}>
          <select value={clubId} onChange={e => setClubId(e.target.value)} style={{ minWidth: 180 }}>
            <option value="">Все клубы</option>
            {clubs.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>
          <input type="date" value={from} onChange={e => setFrom(e.target.value)} />
          <input type="date" value={to}   onChange={e => setTo(e.target.value)} />
          <button className="btn btn-primary btn-sm" onClick={load}>Применить</button>
          <span style={{ color: 'var(--muted)', fontSize: 12, marginLeft: 'auto' }}>Записей: {visits.length}</span>
        </div>

        {loading ? (
          <div className="empty-state"><p>Загрузка…</p></div>
        ) : visits.length === 0 ? (
          <div className="empty-state"><p>Посещений нет</p></div>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Клиент</th>
                <th>Клуб</th>
                <th>Способ входа</th>
                <th>Вход</th>
                <th>Выход</th>
                <th>Статус</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {visits.map(v => {
                const client = v.clientSubscription?.client
                const inside = !v.exitedAt
                return (
                  <tr key={v.id}>
                    <td><b>{client ? `${client.firstName} ${client.lastName}` : '—'}</b></td>
                    <td>{v.club?.name ?? '—'}</td>
                    <td>{METHOD_LABELS[v.entryMethod] ?? v.entryMethod}</td>
                    <td style={{ whiteSpace: 'nowrap' }}>{fmt(v.enteredAt)}</td>
                    <td style={{ whiteSpace: 'nowrap' }}>{fmt(v.exitedAt)}</td>
                    <td>
                      <span style={{
                        display: 'inline-block', padding: '2px 8px', borderRadius: 6, fontSize: 12,
                        background: inside ? 'rgba(99,102,241,.15)' : 'rgba(100,116,139,.12)',
                        color: inside ? '#818cf8' : 'var(--muted)',
                      }}>
                        {inside ? 'Внутри' : 'Вышел'}
                      </span>
                    </td>
                    <td>
                      {inside && (
                        <button className="btn btn-ghost btn-sm" onClick={() => recordExit(v.id)}>
                          Записать выход
                        </button>
                      )}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
