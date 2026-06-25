import { useEffect, useState } from 'react'
import { api } from '../api'
import Modal from '../components/Modal'

export default function ClassTypes() {
  const [items, setItems]   = useState([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal]   = useState(null)
  const [form, setForm]     = useState({})
  const [saving, setSaving] = useState(false)
  const [error, setError]   = useState('')

  const load = () => api.get('/class-types').then(setItems).finally(() => setLoading(false))
  useEffect(() => { load() }, [])

  const open = (data = {}) => { setForm(data); setError(''); setModal({ mode: data.id ? 'edit' : 'create', data }) }

  const save = async () => {
    setSaving(true); setError('')
    try {
      if (modal.mode === 'create') await api.post('/class-types', form)
      else await api.put(`/class-types/${modal.data.id}`, form)
      setModal(null); load()
    } catch (e) { setError(e.message) }
    finally { setSaving(false) }
  }

  const remove = async (id) => {
    if (!window.confirm('Удалить тип занятия?')) return
    await api.del(`/class-types/${id}`); load()
  }

  const f = (k, v) => setForm(p => ({ ...p, [k]: v }))

  return (
    <div>
      <div className="page-header">
        <div><h2>Типы занятий</h2><p>Виды тренировок</p></div>
        <button className="btn btn-primary" onClick={() => open()}>+ Добавить</button>
      </div>

      <div className="table-wrap">
        {loading ? (
          <div className="empty-state"><p>Загрузка…</p></div>
        ) : items.length === 0 ? (
          <div className="empty-state"><p>Нет типов занятий</p></div>
        ) : (
          <table>
            <thead><tr><th>Название</th><th>Описание</th><th></th></tr></thead>
            <tbody>
              {items.map(ct => (
                <tr key={ct.id}>
                  <td><b>{ct.name}</b></td>
                  <td>{ct.description ?? '—'}</td>
                  <td>
                    <div className="td-actions">
                      <button className="btn btn-ghost btn-sm" onClick={() => open(ct)}>Изм.</button>
                      <button className="btn btn-danger btn-sm" onClick={() => remove(ct.id)}>Удал.</button>
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
          <div className="modal-title">{modal.mode === 'create' ? 'Новый тип' : 'Изменить тип'}</div>
          <div className="modal-subtitle">Заполните данные</div>
          <div className="form-grid">
            <div className="field"><label>Название</label><input value={form.name ?? ''} onChange={e => f('name', e.target.value)} /></div>
            <div className="field"><label>Описание</label><textarea rows={3} value={form.description ?? ''} onChange={e => f('description', e.target.value)} /></div>
          </div>
          {error && <div className="error-box" style={{ marginTop: 14 }}>{error}</div>}
          <div className="modal-actions">
            <button className="btn btn-ghost" onClick={() => setModal(null)}>Отмена</button>
            <button className="btn btn-primary" onClick={save} disabled={saving}>{saving ? 'Сохранение…' : 'Сохранить'}</button>
          </div>
        </Modal>
      )}
    </div>
  )
}
