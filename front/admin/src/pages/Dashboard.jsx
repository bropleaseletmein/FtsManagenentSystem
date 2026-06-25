import { useEffect, useState } from 'react'
import { api } from '../api'

function fmt(dt) {
  if (!dt) return '—'
  return new Date(dt).toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

export default function Dashboard() {
  const [clubs, setClubs]       = useState([])
  const [clients, setClients]   = useState([])
  const [schedule, setSchedule] = useState([])
  const [hallCount, setHallCount] = useState(0)
  const [loading, setLoading]   = useState(true)

  useEffect(() => {
    Promise.all([
      api.get('/clubs'),
      api.get('/clients'),
      api.get(`/schedule?from=${new Date().toISOString()}`),
    ]).then(([c, cl, sc]) => {
      const clubs = c ?? []
      setClubs(clubs)
      setClients(cl ?? [])
      setSchedule(sc ?? [])
      return Promise.all(clubs.map(club => api.get(`/clubs/${club.id}/halls`).catch(() => [])))
    }).then(hallsPerClub => {
      setHallCount(hallsPerClub.reduce((sum, h) => sum + h.length, 0))
    }).finally(() => setLoading(false))
  }, [])

  const upcoming = schedule.filter(s => s.status === 'scheduled').slice(0, 5)

  if (loading) return <div className="empty-state"><p>Загрузка…</p></div>

  return (
    <div>
      <div className="page-header">
        <div>
          <h2>Дашборд</h2>
          <p>Обзор системы FitnessNetwork</p>
        </div>
      </div>

      <div className="stat-grid">
        <div className="stat-card">
          <div className="stat-label">Клубов</div>
          <div className="stat-value">{clubs.length}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Клиентов</div>
          <div className="stat-value">{clients.length}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Предстоящих занятий</div>
          <div className="stat-value">{schedule.filter(s => s.status === 'scheduled').length}</div>
        </div>
        <div className="stat-card">
          <div className="stat-label">Залов всего</div>
          <div className="stat-value">{hallCount}</div>
        </div>
      </div>

      <div className="table-wrap" style={{ marginTop: 0 }}>
        <div className="table-toolbar">
          <b style={{ fontSize: 14 }}>Ближайшие занятия</b>
        </div>
        {upcoming.length === 0 ? (
          <div className="empty-state"><p>Нет предстоящих занятий</p></div>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Дата</th>
                <th>Занятие</th>
                <th>Тренер</th>
                <th>Клуб / Зал</th>
                <th>Мест занято</th>
              </tr>
            </thead>
            <tbody>
              {upcoming.map(s => (
                <tr key={s.id}>
                  <td style={{ whiteSpace: 'nowrap' }}>{fmt(s.startsAt)}</td>
                  <td><b>{s.classTypeName}</b></td>
                  <td>{s.trainerFirstName} {s.trainerLastName}</td>
                  <td>{s.clubName}<div className="td-sub">{s.hallName}</div></td>
                  <td>{s.bookedCount} / {s.capacity}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  )
}
