import { useEffect, useState } from 'react'
import { api } from '../api'

function fmt(dt) {
  if (!dt) return '—'
  const d = new Date(dt)
  return d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' })
    + ' ' + d.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
}

function fmtDate(dt) {
  if (!dt) return '—'
  return new Date(dt).toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' })
}

const TABS = [
  { id: 'occupancy',  label: 'Текущая загрузка' },
  { id: 'attendance', label: 'Посещаемость' },
  { id: 'workload',   label: 'Нагрузка тренеров' },
  { id: 'classes',    label: 'Заполняемость занятий' },
]

export default function Reports() {
  const [tab, setTab]   = useState('occupancy')
  const [clubs, setClubs] = useState([])

  const [occupancy, setOccupancy]           = useState([])
  const [occupancyLoading, setOccupancyLoading] = useState(true)
  const [occupancyError, setOccupancyError] = useState('')

  const [attClub, setAttClub] = useState('')
  const [attFrom, setAttFrom] = useState('')
  const [attTo, setAttTo]     = useState('')
  const [attendance, setAttendance] = useState([])
  const [attLoading, setAttLoading] = useState(false)
  const [attLoaded, setAttLoaded]   = useState(false)

  const [wlClub, setWlClub] = useState('')
  const [wlFrom, setWlFrom] = useState('')
  const [wlTo, setWlTo]     = useState('')
  const [workload, setWorkload] = useState([])
  const [wlLoading, setWlLoading] = useState(false)
  const [wlLoaded, setWlLoaded]   = useState(false)

  const [clFrom, setClFrom] = useState('')
  const [clTo, setClTo]     = useState('')
  const [classes, setClasses] = useState([])
  const [clLoading, setClLoading] = useState(false)
  const [clLoaded, setClLoaded]   = useState(false)

  useEffect(() => {
    api.get('/clubs').then(d => setClubs(d ?? [])).catch(() => {})
    loadOccupancy()
  }, []) // eslint-disable-line

  const loadOccupancy = () => {
    setOccupancyLoading(true)
    setOccupancyError('')
    api.get('/reporting/current-occupancy')
      .then(d => setOccupancy(d ?? []))
      .catch(e => setOccupancyError(e.message))
      .finally(() => setOccupancyLoading(false))
  }

  const loadAttendance = () => {
    setAttLoading(true)
    const p = new URLSearchParams()
    if (attClub) p.set('clubId', attClub)
    if (attFrom) p.set('from', new Date(attFrom + 'T00:00:00').toISOString())
    if (attTo)   p.set('to',   new Date(attTo   + 'T23:59:59').toISOString())
    api.get(`/reporting/attendance?${p}`)
      .then(d => { setAttendance(d ?? []); setAttLoaded(true) })
      .catch(() => {})
      .finally(() => setAttLoading(false))
  }

  const loadWorkload = () => {
    setWlLoading(true)
    const p = new URLSearchParams()
    if (wlClub) p.set('clubId', wlClub)
    if (wlFrom) p.set('from', new Date(wlFrom + 'T00:00:00').toISOString())
    if (wlTo)   p.set('to',   new Date(wlTo   + 'T23:59:59').toISOString())
    api.get(`/reporting/trainers/workload?${p}`)
      .then(d => { setWorkload(d ?? []); setWlLoaded(true) })
      .catch(() => {})
      .finally(() => setWlLoading(false))
  }

  const loadClasses = () => {
    setClLoading(true)
    const p = new URLSearchParams()
    if (clFrom) p.set('from', new Date(clFrom + 'T00:00:00').toISOString())
    if (clTo)   p.set('to',   new Date(clTo   + 'T23:59:59').toISOString())
    api.get(`/reporting/classes/occupancy?${p}`)
      .then(d => { setClasses(d ?? []); setClLoaded(true) })
      .catch(() => {})
      .finally(() => setClLoading(false))
  }

  const clubName = (id) => clubs.find(c => c.id === id)?.name ?? id

  return (
    <div>
      <div className="page-header">
        <div>
          <h2>Отчёты</h2>
          <p>Аналитика по посещаемости и занятиям</p>
        </div>
      </div>

      <div style={s.tabs}>
        {TABS.map(t => (
          <button
            key={t.id}
            style={{ ...s.tabBtn, ...(tab === t.id ? s.tabActive : {}) }}
            onClick={() => setTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* Текущая загрузка */}
      {tab === 'occupancy' && (
        <div>
          <div style={s.sectionHead}>
            <span style={s.hint}>Клиенты, находящиеся в клубах прямо сейчас</span>
            <button className="btn btn-ghost btn-sm" onClick={loadOccupancy}>Обновить</button>
          </div>
          {occupancyLoading ? (
            <div className="empty-state"><p>Загрузка…</p></div>
          ) : occupancyError ? (
            <div className="error-box">{occupancyError}</div>
          ) : occupancy.length === 0 ? (
            <div className="empty-state"><p>Нет активных посещений</p></div>
          ) : (
            <div className="stat-grid">
              {occupancy.map(r => (
                <div key={`occ-${r.clubId}`} className="stat-card">
                  <div className="stat-label">{r.clubName}</div>
                  <div className="stat-value">{r.currentVisitors}</div>
                  <div className="stat-sub">чел. внутри</div>
                </div>
              ))}
            </div>
          )}
        </div>
      )}

      {/* Посещаемость */}
      {tab === 'attendance' && (
        <div className="table-wrap">
          <div className="table-toolbar">
            <select value={attClub} onChange={e => setAttClub(e.target.value)}>
              <option value="">Все клубы</option>
              {clubs.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
            <label>С</label>
            <input type="date" value={attFrom} onChange={e => setAttFrom(e.target.value)} />
            <label>По</label>
            <input type="date" value={attTo} onChange={e => setAttTo(e.target.value)} />
            <button className="btn btn-primary btn-sm" onClick={loadAttendance} disabled={attLoading}>
              {attLoading ? 'Загрузка…' : 'Применить'}
            </button>
            <span className="spacer" />
            {attLoaded && <span style={s.hint}>{attendance.length} строк</span>}
          </div>
          {!attLoaded ? (
            <div className="empty-state"><p>Выберите параметры и нажмите «Применить»</p></div>
          ) : attLoading ? (
            <div className="empty-state"><p>Загрузка…</p></div>
          ) : attendance.length === 0 ? (
            <div className="empty-state"><p>Нет данных за выбранный период</p></div>
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Дата</th>
                  <th>Клуб</th>
                  <th>Посещений</th>
                </tr>
              </thead>
              <tbody>
                {attendance.map((r, i) => (
                  <tr key={r.clubId ? `att-${r.clubId}-${r.date}-${i}` : `att-${i}`}>
                    <td>{fmtDate(r.date)}</td>
                    <td>{clubName(r.clubId)}</td>
                    <td><b>{r.visitCount}</b></td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {/* Нагрузка тренеров */}
      {tab === 'workload' && (
        <div className="table-wrap">
          <div className="table-toolbar">
            <select value={wlClub} onChange={e => setWlClub(e.target.value)}>
              <option value="">Все клубы</option>
              {clubs.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
            </select>
            <label>С</label>
            <input type="date" value={wlFrom} onChange={e => setWlFrom(e.target.value)} />
            <label>По</label>
            <input type="date" value={wlTo} onChange={e => setWlTo(e.target.value)} />
            <button className="btn btn-primary btn-sm" onClick={loadWorkload} disabled={wlLoading}>
              {wlLoading ? 'Загрузка…' : 'Применить'}
            </button>
            <span className="spacer" />
            {wlLoaded && <span style={s.hint}>{workload.length} тренеров</span>}
          </div>
          {!wlLoaded ? (
            <div className="empty-state"><p>Выберите параметры и нажмите «Применить»</p></div>
          ) : wlLoading ? (
            <div className="empty-state"><p>Загрузка…</p></div>
          ) : workload.length === 0 ? (
            <div className="empty-state"><p>Нет данных</p></div>
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Тренер</th>
                  <th>Занятий</th>
                  <th>Записей</th>
                </tr>
              </thead>
              <tbody>
                {workload.map(r => (
                  <tr key={`wl-${r.trainerId}`}>
                    <td><b>{r.trainerName}</b></td>
                    <td>{r.totalClasses}</td>
                    <td>{r.totalBookings}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}

      {/* Заполняемость занятий */}
      {tab === 'classes' && (
        <div className="table-wrap">
          <div className="table-toolbar">
            <label>С</label>
            <input type="date" value={clFrom} onChange={e => setClFrom(e.target.value)} />
            <label>По</label>
            <input type="date" value={clTo} onChange={e => setClTo(e.target.value)} />
            <button className="btn btn-primary btn-sm" onClick={loadClasses} disabled={clLoading}>
              {clLoading ? 'Загрузка…' : 'Применить'}
            </button>
            <span className="spacer" />
            {clLoaded && <span style={s.hint}>{classes.length} занятий</span>}
          </div>
          {!clLoaded ? (
            <div className="empty-state"><p>Выберите период и нажмите «Применить»</p></div>
          ) : clLoading ? (
            <div className="empty-state"><p>Загрузка…</p></div>
          ) : classes.length === 0 ? (
            <div className="empty-state"><p>Нет данных за выбранный период</p></div>
          ) : (
            <table>
              <thead>
                <tr>
                  <th>Дата и время</th>
                  <th>Тип занятия</th>
                  <th>Мест</th>
                  <th>Занято</th>
                  <th>Заполненность</th>
                </tr>
              </thead>
              <tbody>
                {classes.map(r => (
                  <tr key={`cls-${r.id}`}>
                    <td style={{ whiteSpace: 'nowrap' }}>{fmt(r.startsAt)}</td>
                    <td><b>{r.classTypeName}</b></td>
                    <td>{r.capacity}</td>
                    <td>{r.activeBookings}</td>
                    <td>
                      <span style={{
                        fontWeight: 600,
                        color: r.occupancyPct >= 90 ? 'var(--danger)'
                             : r.occupancyPct >= 60 ? 'var(--warn)'
                             : 'var(--success)',
                      }}>
                        {r.occupancyPct}%
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      )}
    </div>
  )
}

const s = {
  tabs: {
    display: 'flex', borderBottom: '1px solid var(--border)', marginBottom: 20,
  },
  tabBtn: {
    padding: '8px 16px', border: 'none', background: 'none', cursor: 'pointer',
    fontSize: 13, fontWeight: 500, color: 'var(--muted)',
    borderBottom: '2px solid transparent', marginBottom: -1,
  },
  tabActive: {
    color: 'var(--accent)', borderBottom: '2px solid var(--accent)',
  },
  sectionHead: {
    display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 16,
  },
  hint: { fontSize: 12, color: 'var(--muted)' },
}
