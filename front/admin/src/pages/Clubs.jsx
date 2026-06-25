import { useEffect, useState } from 'react'
import { api } from '../api'
import Modal from '../components/Modal'

const emptyClub = { name: '', address: '', phone: '' }
const emptyHall = { name: '', capacity: '' }

export default function Clubs() {
  const [clubs, setClubs]       = useState([])
  const [loading, setLoading]   = useState(true)
  const [expanded, setExpanded] = useState(null)
  const [halls, setHalls]       = useState({})

  const [clubModal, setClubModal] = useState(null) // null | { mode:'create'|'edit', data }
  const [hallModal, setHallModal] = useState(null) // null | { mode, clubId, data }
  const [form, setForm]           = useState({})
  const [saving, setSaving]       = useState(false)
  const [error, setError]         = useState('')

  const load = () => api.get('/clubs').then(setClubs).finally(() => setLoading(false))
  useEffect(() => { load() }, [])

  const loadHalls = (clubId) =>
    api.get(`/clubs/${clubId}/halls`).then(h => setHalls(prev => ({ ...prev, [clubId]: h })))

  const toggle = (id) => {
    if (expanded === id) { setExpanded(null); return }
    setExpanded(id)
    if (!halls[id]) loadHalls(id)
  }

  const openClub = (mode, data = emptyClub) => { setForm(data); setError(''); setClubModal({ mode, data }) }
  const openHall = (mode, clubId, data = emptyHall) => { setForm(data); setError(''); setHallModal({ mode, clubId, data }) }

  const saveClub = async () => {
    setSaving(true); setError('')
    try {
      if (clubModal.mode === 'create') await api.post('/clubs', form)
      else await api.put(`/clubs/${clubModal.data.id}`, form)
      setClubModal(null); load()
    } catch (e) { setError(e.message) }
    finally { setSaving(false) }
  }

  const deleteClub = async (id) => {
    if (!window.confirm('Удалить клуб?')) return
    await api.del(`/clubs/${id}`); load()
  }

  const saveHall = async () => {
    setSaving(true); setError('')
    try {
      const body = { ...form, capacity: Number(form.capacity) }
      if (hallModal.mode === 'create') await api.post(`/clubs/${hallModal.clubId}/halls`, body)
      else await api.put(`/halls/${hallModal.data.id}`, body)
      setHallModal(null); loadHalls(hallModal.clubId)
    } catch (e) { setError(e.message) }
    finally { setSaving(false) }
  }

  const deleteHall = async (hallId, clubId) => {
    if (!window.confirm('Удалить зал?')) return
    await api.del(`/halls/${hallId}`); loadHalls(clubId)
  }

  const f = (k, v) => setForm(p => ({ ...p, [k]: v }))

  return (
    <div>
      <div className="page-header">
        <div><h2>Клубы и залы</h2><p>Управление филиалами сети</p></div>
        <button className="btn btn-primary" onClick={() => openClub('create')}>+ Добавить клуб</button>
      </div>

      <div className="table-wrap">
        {loading ? (
          <div className="empty-state"><p>Загрузка…</p></div>
        ) : clubs.length === 0 ? (
          <div className="empty-state"><p>Клубов нет</p></div>
        ) : (
          <table>
            <thead><tr><th>Название</th><th>Адрес</th><th>Телефон</th><th>Залы</th><th></th></tr></thead>
            <tbody>
              {clubs.map(c => (<>
                <tr key={c.id}>
                  <td><b>{c.name}</b></td>
                  <td>{c.address}</td>
                  <td>{c.phone ?? '—'}</td>
                  <td>
                    <button className="btn btn-ghost btn-sm" onClick={() => toggle(c.id)}>
                      {expanded === c.id ? '▲ Скрыть' : '▼ Залы'}
                    </button>
                  </td>
                  <td>
                    <div className="td-actions">
                      <button className="btn btn-ghost btn-sm" onClick={() => openClub('edit', c)}>Изм.</button>
                      <button className="btn btn-danger btn-sm" onClick={() => deleteClub(c.id)}>Удал.</button>
                    </div>
                  </td>
                </tr>
                {expanded === c.id && (
                  <tr key={c.id + '-halls'} className="sub-row">
                    <td colSpan={5} style={{ padding: '0 0 0 32px' }}>
                      <table className="sub-table">
                        <thead><tr><td><b>Зал</b></td><td><b>Вместимость</b></td><td></td></tr></thead>
                        <tbody>
                          {(halls[c.id] ?? []).map(h => (
                            <tr key={h.id}>
                              <td>{h.name}</td>
                              <td>{h.capacity}</td>
                              <td>
                                <div className="td-actions">
                                  <button className="btn btn-ghost btn-sm" onClick={() => openHall('edit', c.id, h)}>Изм.</button>
                                  <button className="btn btn-danger btn-sm" onClick={() => deleteHall(h.id, c.id)}>Удал.</button>
                                </div>
                              </td>
                            </tr>
                          ))}
                          <tr>
                            <td colSpan={3}>
                              <button className="btn btn-ghost btn-sm" onClick={() => openHall('create', c.id)}>+ Добавить зал</button>
                            </td>
                          </tr>
                        </tbody>
                      </table>
                    </td>
                  </tr>
                )}
              </>))}
            </tbody>
          </table>
        )}
      </div>

      {clubModal && (
        <Modal onClose={() => setClubModal(null)}>
          <div className="modal-title">{clubModal.mode === 'create' ? 'Новый клуб' : 'Изменить клуб'}</div>
          <div className="modal-subtitle">Заполните данные</div>
          <div className="form-grid">
            <div className="field"><label>Название</label><input value={form.name ?? ''} onChange={e => f('name', e.target.value)} /></div>
            <div className="field"><label>Адрес</label><input value={form.address ?? ''} onChange={e => f('address', e.target.value)} /></div>
            <div className="field"><label>Телефон</label><input value={form.phone ?? ''} onChange={e => f('phone', e.target.value)} /></div>
          </div>
          {error && <div className="error-box" style={{ marginTop: 14 }}>{error}</div>}
          <div className="modal-actions">
            <button className="btn btn-ghost" onClick={() => setClubModal(null)}>Отмена</button>
            <button className="btn btn-primary" onClick={saveClub} disabled={saving}>{saving ? 'Сохранение…' : 'Сохранить'}</button>
          </div>
        </Modal>
      )}

      {hallModal && (
        <Modal onClose={() => setHallModal(null)}>
          <div className="modal-title">{hallModal.mode === 'create' ? 'Новый зал' : 'Изменить зал'}</div>
          <div className="modal-subtitle">Заполните данные</div>
          <div className="form-grid">
            <div className="field"><label>Название</label><input value={form.name ?? ''} onChange={e => f('name', e.target.value)} /></div>
            <div className="field"><label>Вместимость</label><input type="number" value={form.capacity ?? ''} onChange={e => f('capacity', e.target.value)} /></div>
          </div>
          {error && <div className="error-box" style={{ marginTop: 14 }}>{error}</div>}
          <div className="modal-actions">
            <button className="btn btn-ghost" onClick={() => setHallModal(null)}>Отмена</button>
            <button className="btn btn-primary" onClick={saveHall} disabled={saving}>{saving ? 'Сохранение…' : 'Сохранить'}</button>
          </div>
        </Modal>
      )}
    </div>
  )
}
