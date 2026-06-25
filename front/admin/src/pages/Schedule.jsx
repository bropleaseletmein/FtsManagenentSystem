import { useEffect, useState } from 'react'
import { api } from '../api'
import Badge from '../components/Badge'
import Modal from '../components/Modal'

function fmt(dt) {
  if (!dt) return '—'
  const d = new Date(dt)
  return d.toLocaleDateString('ru-RU', { day: '2-digit', month: '2-digit', year: 'numeric' })
    + ' ' + d.toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })
}

export default function Schedule() {
  const [items, setItems]     = useState([])
  const [clubs, setClubs]     = useState([])
  const [classTypes, setClassTypes] = useState([])
  const [trainers, setTrainers] = useState([])
  const [allHalls, setAllHalls] = useState([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal]     = useState(null)
  const [form, setForm]       = useState({})
  const [saving, setSaving]   = useState(false)
  const [error, setError]     = useState('')
  const [statusModal, setStatusModal] = useState(null)

  const load = async () => {
    setLoading(true)
    const from = new Date(Date.now() - 7 * 86400000).toISOString()
    const [sc, cl, ct, st] = await Promise.all([
      api.get(`/schedule?from=${from}`),
      api.get('/clubs'),
      api.get('/class-types'),
      api.get('/staff'),
    ])
    setItems(sc ?? [])
    setClubs(cl ?? [])
    setClassTypes(ct ?? [])
    setTrainers((st ?? []).filter(s => s.roles?.some(r => r.role === 'trainer')))

    const hallsArr = []
    for (const c of (cl ?? [])) {
      const h = await api.get(`/clubs/${c.id}/halls`)
      hallsArr.push(...(h ?? []).map(hall => ({ ...hall, clubName: c.name })))
    }
    setAllHalls(hallsArr)
    setLoading(false)
  }

  useEffect(() => { load() }, []) // eslint-disable-line

  const open = (data = {}) => {
    setForm(data.id ? {
      classTypeId: data.classTypeId,
      hallId: data.hallId,
      trainerId: data.trainerId,
      startsAt: data.startsAt?.slice(0, 16),
      endsAt: data.endsAt?.slice(0, 16),
      capacity: data.capacity,
    } : { capacity: 20 })
    setError('')
    setModal({ mode: data.id ? 'edit' : 'create', data })
  }

  const save = async () => {
    setSaving(true); setError('')
    try {
      const body = { ...form, capacity: Number(form.capacity) }
      if (modal.mode === 'create') await api.post('/schedule', body)
      else await api.put(`/schedule/${modal.data.id}`, body)
      setModal(null); load()
    } catch (e) { setError(e.message) }
    finally { setSaving(false) }
  }

  const updateStatus = async (id, status) => {
    await api.put(`/schedule/${id}/status`, { status })
    setStatusModal(null); load()
  }

  const f = (k, v) => setForm(p => ({ ...p, [k]: v }))

  return (
    <div>
      <div className="page-header">
        <div><h2>Расписание</h2><p>Управление занятиями</p></div>
        <button className="btn btn-primary" onClick={() => open()}>+ Добавить занятие</button>
      </div>

      <div className="table-wrap">
        {loading ? (
          <div className="empty-state"><p>Загрузка…</p></div>
        ) : items.length === 0 ? (
          <div className="empty-state"><p>Занятий нет</p></div>
        ) : (
          <table>
            <thead>
              <tr>
                <th>Дата и время</th>
                <th>Занятие</th>
                <th>Тренер</th>
                <th>Клуб / Зал</th>
                <th>Вместимость</th>
                <th>Записи</th>
                <th>Статус</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.map(s => (
                <tr key={s.id}>
                  <td style={{ whiteSpace: 'nowrap' }}>{fmt(s.startsAt)}</td>
                  <td><b>{s.classTypeName}</b></td>
                  <td>{s.trainerFirstName} {s.trainerLastName}</td>
                  <td>{s.clubName}<div className="td-sub">{s.hallName}</div></td>
                  <td>{s.capacity}</td>
                  <td>{s.bookedCount}</td>
                  <td><Badge status={s.status} /></td>
                  <td>
                    <div className="td-actions">
                      {s.status === 'scheduled' && <>
                        <button className="btn btn-ghost btn-sm" onClick={() => open(s)}>Изм.</button>
                        <button className="btn btn-ghost btn-sm" onClick={() => setStatusModal(s)}>Статус</button>
                      </>}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {modal && (
        <Modal onClose={() => setModal(null)}>
          <div className="modal-title">{modal.mode === 'create' ? 'Новое занятие' : 'Изменить занятие'}</div>
          <div className="modal-subtitle">Заполните данные</div>
          <div className="form-grid">
            <div className="field">
              <label>Тип занятия</label>
              <select value={form.classTypeId ?? ''} onChange={e => f('classTypeId', e.target.value)}>
                <option value="">— выберите —</option>
                {classTypes.map(ct => <option key={ct.id} value={ct.id}>{ct.name}</option>)}
              </select>
            </div>
            <div className="field">
              <label>Зал</label>
              <select value={form.hallId ?? ''} onChange={e => f('hallId', e.target.value)}>
                <option value="">— выберите —</option>
                {allHalls.map(h => <option key={h.id} value={h.id}>{h.clubName} — {h.name}</option>)}
              </select>
            </div>
            <div className="field">
              <label>Тренер</label>
              <select value={form.trainerId ?? ''} onChange={e => f('trainerId', e.target.value)}>
                <option value="">— выберите —</option>
                {trainers.map(t => <option key={t.id} value={t.id}>{t.firstName} {t.lastName}</option>)}
              </select>
            </div>
            <div className="form-row">
              <div className="field"><label>Начало</label><input type="datetime-local" value={form.startsAt ?? ''} onChange={e => f('startsAt', e.target.value)} /></div>
              <div className="field"><label>Конец</label><input type="datetime-local" value={form.endsAt ?? ''} onChange={e => f('endsAt', e.target.value)} /></div>
            </div>
            <div className="field"><label>Вместимость</label><input type="number" value={form.capacity ?? ''} onChange={e => f('capacity', e.target.value)} /></div>
          </div>
          {error && <div className="error-box" style={{ marginTop: 14 }}>{error}</div>}
          <div className="modal-actions">
            <button className="btn btn-ghost" onClick={() => setModal(null)}>Отмена</button>
            <button className="btn btn-primary" onClick={save} disabled={saving}>{saving ? 'Сохранение…' : 'Сохранить'}</button>
          </div>
        </Modal>
      )}

      {statusModal && (
        <Modal onClose={() => setStatusModal(null)}>
          <div className="modal-title">Изменить статус</div>
          <div className="modal-subtitle">{statusModal.classTypeName} — {fmt(statusModal.startsAt)}</div>
          <div style={{ display: 'flex', gap: 10, marginTop: 10 }}>
            <button className="btn btn-ghost" onClick={() => updateStatus(statusModal.id, 'completed')}>Завершено</button>
            <button className="btn btn-danger" onClick={() => updateStatus(statusModal.id, 'cancelled')}>Отменить</button>
          </div>
          <div className="modal-actions">
            <button className="btn btn-ghost" onClick={() => setStatusModal(null)}>Закрыть</button>
          </div>
        </Modal>
      )}
    </div>
  )
}
