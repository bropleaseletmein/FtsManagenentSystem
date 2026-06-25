import { useEffect, useState } from 'react'
import { api } from '../api'
import { useAuth } from '../context/AuthContext'
import Badge from '../components/Badge'

function fmt(dt) {
  if (!dt) return '—'
  const d = new Date(dt)
  return d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' })
    + ' ' + d.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
}

export default function Bookings() {
  const { refreshSubs } = useAuth()
  const [items, setItems]     = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError]     = useState('')

  const load = async () => {
    setLoading(true)
    setError('')
    try {
      const data = await api.get('/bookings/my')
      setItems(data)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, [])

  const handleCancel = async (id) => {
    if (!window.confirm('Отменить запись на занятие?')) return
    try {
      await api.del(`/bookings/${id}`)
      await Promise.all([load(), refreshSubs()])
    } catch (err) {
      alert('Ошибка: ' + err.message)
    }
  }

  return (
    <div>
      <div className="page-header">
        <h2>Мои записи</h2>
        <p>Список записей на занятия</p>
      </div>

      <div className="table-wrap">
        {loading ? (
          <div className="empty-state"><p>Загрузка…</p></div>
        ) : error ? (
          <div className="empty-state"><p>Ошибка: {error}</p></div>
        ) : items.length === 0 ? (
          <div className="empty-state">
            <p>Записей пока нет</p>
          </div>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Занятие</th>
                <th>Дата</th>
                <th>Тренер</th>
                <th>Клуб / Зал</th>
                <th>Статус</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.map(b => {
                const trainer = `${b.trainerFirstName ?? ''} ${b.trainerLastName ?? ''}`.trim() || '—'
                return (
                  <tr key={b.id}>
                    <td><b>{b.classTypeName ?? '—'}</b></td>
                    <td style={{ whiteSpace: 'nowrap' }}>{fmt(b.startsAt)}</td>
                    <td>{trainer}</td>
                    <td>
                      {b.clubName ?? '—'}
                      <div className="td-sub">{b.hallName ?? ''}</div>
                    </td>
                    <td><Badge status={b.status} /></td>
                    <td>
                      {b.status === 'booked' && (
                        <button
                          className="btn btn-danger btn-sm"
                          onClick={() => handleCancel(b.id)}
                        >
                          Отменить
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
