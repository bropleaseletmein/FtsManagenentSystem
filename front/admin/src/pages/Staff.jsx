import { useEffect, useState } from 'react'
import { api } from '../api'
import Badge from '../components/Badge'
import Modal from '../components/Modal'

export default function Staff() {
  const [staff, setStaff]     = useState([])
  const [clubs, setClubs]     = useState([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal]     = useState(null)
  const [form, setForm]       = useState({})
  const [saving, setSaving]   = useState(false)
  const [error, setError]     = useState('')
  const [search, setSearch]   = useState('')

  const load = () =>
    Promise.all([api.get('/staff'), api.get('/clubs')])
      .then(([s, c]) => { setStaff(s ?? []); setClubs(c ?? []) })
      .finally(() => setLoading(false))

  useEffect(() => { load() }, [])

  const open = (mode, data = {}) => {
    setForm(data.id ? { ...data, roles: data.roles?.map(r => r.role) ?? [] } : { roles: ['trainer'] })
    setError('')
    setModal({ mode, data })
  }

  const save = async () => {
    setSaving(true); setError('')
    try {
      const body = {
        firstName: form.firstName,
        lastName:  form.lastName,
        email:     form.email,
        clubId:    form.clubId,
        roles:     Array.isArray(form.roles) ? form.roles : [form.roles],
        ...(form.password ? { password: form.password } : {}),
      }
      if (modal.mode === 'create') await api.post('/staff', body)
      else await api.put(`/staff/${modal.data.id}`, body)
      setModal(null); load()
    } catch (e) { setError(e.message) }
    finally { setSaving(false) }
  }

  const remove = async (id) => {
    if (!window.confirm('Деактивировать сотрудника?')) return
    await api.del(`/staff/${id}`); load()
  }

  const f = (k, v) => setForm(p => ({ ...p, [k]: v }))
  const toggleRole = (r) => {
    const cur = form.roles ?? []
    setForm(p => ({ ...p, roles: cur.includes(r) ? cur.filter(x => x !== r) : [...cur, r] }))
  }

  const filtered = staff.filter(s =>
    `${s.firstName} ${s.lastName} ${s.email}`.toLowerCase().includes(search.toLowerCase()))

  return (
    <div>
      <div className="page-header">
        <div><h2>Сотрудники</h2><p>Управление персоналом</p></div>
        <button className="btn btn-primary" onClick={() => open('create')}>+ Добавить</button>
      </div>

      <div className="table-wrap">
        <div className="table-toolbar">
          <input placeholder="Поиск…" value={search} onChange={e => setSearch(e.target.value)} />
        </div>
        {loading ? (
          <div className="empty-state"><p>Загрузка…</p></div>
        ) : filtered.length === 0 ? (
          <div className="empty-state"><p>Нет сотрудников</p></div>
        ) : (
          <table>
            <thead><tr><th>Имя</th><th>Email</th><th>Клуб</th><th>Роли</th><th></th></tr></thead>
            <tbody>
              {filtered.map(s => (
                <tr key={s.id}>
                  <td><b>{s.firstName} {s.lastName}</b></td>
                  <td>{s.email}</td>
                  <td>{s.club?.name ?? '—'}</td>
                  <td style={{ display: 'flex', gap: 4, flexWrap: 'wrap' }}>
                    {s.roles?.map(r => <Badge key={r.role} status={r.role} />)}
                  </td>
                  <td>
                    <div className="td-actions">
                      <button className="btn btn-ghost btn-sm" onClick={() => open('edit', s)}>Изм.</button>
                      <button className="btn btn-danger btn-sm" onClick={() => remove(s.id)}>Удал.</button>
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
          <div className="modal-title">{modal.mode === 'create' ? 'Новый сотрудник' : 'Изменить сотрудника'}</div>
          <div className="modal-subtitle">Заполните данные</div>
          <div className="form-grid">
            <div className="form-row">
              <div className="field"><label>Имя</label><input value={form.firstName ?? ''} onChange={e => f('firstName', e.target.value)} /></div>
              <div className="field"><label>Фамилия</label><input value={form.lastName ?? ''} onChange={e => f('lastName', e.target.value)} /></div>
            </div>
            <div className="field"><label>Email</label><input type="email" value={form.email ?? ''} onChange={e => f('email', e.target.value)} /></div>
            <div className="field"><label>Пароль {modal.mode === 'edit' ? '(оставьте пустым, чтобы не менять)' : ''}</label><input type="password" value={form.password ?? ''} onChange={e => f('password', e.target.value)} /></div>
            <div className="field">
              <label>Клуб</label>
              <select value={form.clubId ?? ''} onChange={e => f('clubId', e.target.value)}>
                <option value="">— выберите —</option>
                {clubs.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
              </select>
            </div>
            <div className="field">
              <label>Роли</label>
              <div style={{ display: 'flex', gap: 12, marginTop: 4 }}>
                {['admin', 'trainer'].map(r => (
                  <label key={r} style={{ display: 'flex', alignItems: 'center', gap: 6, cursor: 'pointer' }}>
                    <input type="checkbox" checked={(form.roles ?? []).includes(r)} onChange={() => toggleRole(r)} />
                    {r === 'admin' ? 'Администратор' : 'Тренер'}
                  </label>
                ))}
              </div>
            </div>
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
