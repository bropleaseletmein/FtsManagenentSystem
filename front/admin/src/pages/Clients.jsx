import { useEffect, useState } from 'react'
import { api } from '../api'
import Badge from '../components/Badge'
import Modal from '../components/Modal'

function fmt(dt) {
  if (!dt) return '—'
  return new Date(dt).toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

export default function Clients() {
  const [clients, setClients] = useState([])
  const [loading, setLoading] = useState(true)
  const [search, setSearch]   = useState('')
  const [detail, setDetail]   = useState(null)
  const [subs, setSubs]       = useState([])
  const [subsLoading, setSubsLoading] = useState(false)

  useEffect(() => {
    api.get('/clients').then(c => setClients(c ?? [])).finally(() => setLoading(false))
  }, [])

  const openDetail = async (client) => {
    setDetail(client)
    setSubsLoading(true)
    setSubs([])
    api.get(`/clients/${client.id}/subscriptions`)
      .then(s => setSubs(s ?? []))
      .finally(() => setSubsLoading(false))
  }

  const filtered = clients.filter(c =>
    `${c.firstName} ${c.lastName} ${c.email ?? ''} ${c.phone ?? ''}`.toLowerCase().includes(search.toLowerCase()))

  return (
    <div>
      <div className="page-header">
        <div><h2>Клиенты</h2><p>Список зарегистрированных клиентов</p></div>
      </div>

      <div className="table-wrap">
        <div className="table-toolbar">
          <input placeholder="Поиск по имени, email, телефону…" value={search} onChange={e => setSearch(e.target.value)} style={{ width: 300 }} />
          <span style={{ color: 'var(--muted)', fontSize: 12 }}>Всего: {filtered.length}</span>
        </div>
        {loading ? (
          <div className="empty-state"><p>Загрузка…</p></div>
        ) : filtered.length === 0 ? (
          <div className="empty-state"><p>Нет клиентов</p></div>
        ) : (
          <table>
            <thead>
              <tr><th>Имя</th><th>Email</th><th>Телефон</th><th>Дата рождения</th><th></th></tr>
            </thead>
            <tbody>
              {filtered.map(c => (
                <tr key={c.id}>
                  <td><b>{c.firstName} {c.lastName}</b></td>
                  <td>{c.email ?? '—'}</td>
                  <td>{c.phone ?? '—'}</td>
                  <td>{fmt(c.birthDate)}</td>
                  <td>
                    <button className="btn btn-ghost btn-sm" onClick={() => openDetail(c)}>Абонементы</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {detail && (
        <Modal onClose={() => setDetail(null)}>
          <div className="modal-title">{detail.firstName} {detail.lastName}</div>
          <div className="modal-subtitle">Абонементы клиента</div>
          {subsLoading ? (
            <p style={{ color: 'var(--muted)', fontSize: 13 }}>Загрузка…</p>
          ) : subs.length === 0 ? (
            <p style={{ color: 'var(--muted)', fontSize: 13 }}>Нет абонементов</p>
          ) : (
            <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: 13 }}>
              <thead>
                <tr style={{ borderBottom: '1px solid var(--border)' }}>
                  <th style={{ textAlign: 'left', padding: '6px 8px', color: 'var(--muted)', fontSize: 11 }}>Тариф</th>
                  <th style={{ textAlign: 'left', padding: '6px 8px', color: 'var(--muted)', fontSize: 11 }}>Статус</th>
                  <th style={{ textAlign: 'left', padding: '6px 8px', color: 'var(--muted)', fontSize: 11 }}>Истекает</th>
                  <th style={{ textAlign: 'left', padding: '6px 8px', color: 'var(--muted)', fontSize: 11 }}>Посещений</th>
                </tr>
              </thead>
              <tbody>
                {subs.map(s => (
                  <tr key={s.id} style={{ borderBottom: '1px solid var(--border)' }}>
                    <td style={{ padding: '8px 8px' }}>{s.subscriptionType?.name ?? '—'}</td>
                    <td style={{ padding: '8px 8px' }}><Badge status={s.status} /></td>
                    <td style={{ padding: '8px 8px' }}>{fmt(s.expiresAt)}</td>
                    <td style={{ padding: '8px 8px' }}>{s.visitsLeft ?? '∞'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
          <div className="modal-actions">
            <button className="btn btn-ghost" onClick={() => setDetail(null)}>Закрыть</button>
          </div>
        </Modal>
      )}
    </div>
  )
}
