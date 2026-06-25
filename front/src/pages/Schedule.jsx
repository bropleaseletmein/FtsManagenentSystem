import { useEffect, useState, useRef } from 'react'
import { api } from '../api'
import { useAuth } from '../context/AuthContext'
import Badge from '../components/Badge'
import Modal from '../components/Modal'

function fmt(dt) {
  if (!dt) return '—'
  const d = new Date(dt)
  return d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' })
    + ' ' + d.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
}

export default function Schedule() {
  const { subscriptions, refreshSubs } = useAuth()
  const [items, setItems]     = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError]     = useState('')
  const [booking, setBooking] = useState(null)  // { scheduleItem }
  const [bookError, setBookError] = useState('')
  const [bookLoading, setBookLoading] = useState(false)
  const [selectedSub, setSelectedSub] = useState('')

  const fromRef = useRef()
  const toRef   = useRef()

  const load = async () => {
    setLoading(true)
    setError('')
    try {
      const params = new URLSearchParams()
      const now = new Date()
      const fromVal = fromRef.current?.value
      params.set('from', fromVal ? new Date(fromVal + 'T00:00:00').toISOString() : now.toISOString())
      const toVal = toRef.current?.value
      if (toVal) params.set('to', new Date(toVal + 'T23:59:59').toISOString())
      const data = await api.get(`/schedule?${params}`)
      setItems(data)
    } catch (err) {
      setError(err.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { load() }, []) // eslint-disable-line

  const activeSubs = subscriptions.filter(s => s.status === 'active')

  const openBook = (cs) => {
    setBookError('')
    setBooking(cs)
    setSelectedSub(activeSubs[0]?.id ?? '')
  }

  const confirmBook = async () => {
    if (!selectedSub) { setBookError('Выберите абонемент'); return }
    setBookLoading(true)
    setBookError('')
    try {
      await api.post('/bookings', { clientSubscriptionId: selectedSub, classScheduleId: booking.id })
      setBooking(null)
      await Promise.all([load(), refreshSubs()])
    } catch (err) {
      setBookError(err.message)
    } finally {
      setBookLoading(false)
    }
  }

  return (
    <div>
      <div className="page-header">
        <h2>Расписание занятий</h2>
        <p>Выберите занятие и запишитесь</p>
      </div>

      <div className="table-wrap">
        <div className="table-filters">
          <label>С</label>
          <input type="date" ref={fromRef} />
          <label>По</label>
          <input type="date" ref={toRef} />
          <button className="btn btn-primary btn-sm" onClick={load}>Применить</button>
          <button className="btn btn-ghost btn-sm" onClick={() => {
            if (fromRef.current) fromRef.current.value = ''
            if (toRef.current) toRef.current.value = ''
            load()
          }}>Сбросить</button>
        </div>

        {loading ? (
          <div className="empty-state"><p>Загрузка…</p></div>
        ) : error ? (
          <div className="empty-state"><p>Ошибка: {error}</p></div>
        ) : items.length === 0 ? (
          <div className="empty-state">
            <p>Нет запланированных занятий</p>
          </div>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Дата и время</th>
                <th>Занятие</th>
                <th>Тренер</th>
                <th>Клуб / Зал</th>
                <th>Места</th>
                <th>Статус</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.map(cs => {
                const booked  = cs.bookedCount ?? 0
                const free    = cs.capacity - booked
                const trainer = `${cs.trainerFirstName ?? ''} ${cs.trainerLastName ?? ''}`.trim() || '—'
                const canBook = cs.status === 'scheduled' && free > 0

                let spotsEl
                if (free <= 0)      spotsEl = <span className="spots-full">{booked} / {cs.capacity} — Мест нет</span>
                else if (free <= 3) spotsEl = <span className="spots-low">{booked} / {cs.capacity} — осталось {free}</span>
                else                spotsEl = <span className="spots-ok">{booked} / {cs.capacity}</span>

                return (
                  <tr key={cs.id}>
                    <td style={{ whiteSpace: 'nowrap' }}>{fmt(cs.startsAt)}</td>
                    <td><b>{cs.classTypeName ?? '—'}</b></td>
                    <td>{trainer}</td>
                    <td>
                      {cs.clubName ?? '—'}
                      <div className="td-sub">{cs.hallName ?? ''}</div>
                    </td>
                    <td>{spotsEl}</td>
                    <td><Badge status={cs.status} /></td>
                    <td>
                      {canBook && (
                        <button className="btn btn-primary btn-sm" onClick={() => openBook(cs)}>
                          Записаться
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

      {/* Booking modal */}
      {booking && (
        <Modal onClose={() => setBooking(null)}>
          <div className="modal-title">Запись на занятие</div>
          <div className="modal-subtitle">Подтвердите запись</div>

          <div className="modal-info">
            <div className="modal-row">
              <span className="modal-row-label">Занятие</span>
              <span className="modal-row-value">{booking.classTypeName ?? '—'}</span>
            </div>
            <div className="modal-row" style={{ marginTop: 6 }}>
              <span className="modal-row-label">Дата</span>
              <span className="modal-row-value">{fmt(booking.startsAt)}</span>
            </div>
            <div className="modal-row" style={{ marginTop: 6 }}>
              <span className="modal-row-label">Тренер</span>
              <span className="modal-row-value">
                {`${booking.trainerFirstName ?? ''} ${booking.trainerLastName ?? ''}`.trim() || '—'}
              </span>
            </div>
            <div className="modal-row" style={{ marginTop: 6 }}>
              <span className="modal-row-label">Клуб</span>
              <span className="modal-row-value">{booking.clubName ?? '—'}</span>
            </div>
          </div>

          {activeSubs.length === 0 ? (
            <div className="error-box">Нет активных абонементов для записи</div>
          ) : activeSubs.length > 1 ? (
            <div className="modal-field">
              <label>Абонемент</label>
              <select value={selectedSub} onChange={e => setSelectedSub(e.target.value)}>
                {activeSubs.map(s => (
                  <option key={s.id} value={s.id}>
                    {s.subscriptionType?.name ?? s.id}
                    {s.visitsLeft != null ? ` (осталось: ${s.visitsLeft})` : ' (безлимит)'}
                  </option>
                ))}
              </select>
            </div>
          ) : (
            <div style={{ fontSize: 13, color: 'var(--muted)', marginBottom: 14 }}>
              Абонемент: <b style={{ color: 'var(--text)' }}>{activeSubs[0].subscriptionType?.name}</b>
            </div>
          )}

          {bookError && <div className="error-box">{bookError}</div>}

          <div className="modal-actions">
            <button className="btn btn-ghost" onClick={() => setBooking(null)}>Отмена</button>
            <button
              className="btn btn-primary"
              onClick={confirmBook}
              disabled={bookLoading || activeSubs.length === 0}
            >
              {bookLoading ? 'Запись…' : 'Записаться'}
            </button>
          </div>
        </Modal>
      )}
    </div>
  )
}
