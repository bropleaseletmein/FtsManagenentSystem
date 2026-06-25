import { useEffect, useState } from 'react'
import { api } from '../api'
import Modal from '../components/Modal'

export default function SubscriptionTypes() {
  const [items, setItems]   = useState([])
  const [clubs, setClubs]   = useState([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal]   = useState(null)
  const [form, setForm]     = useState({})
  const [saving, setSaving] = useState(false)
  const [error, setError]   = useState('')

  const load = () =>
    Promise.all([api.get('/subscription-types'), api.get('/clubs')])
      .then(([t, c]) => { setItems(t ?? []); setClubs(c ?? []) })
      .finally(() => setLoading(false))

  useEffect(() => { load() }, [])

  const open = (data = {}) => {
    setForm(data.id ? {
      ...data,
      clubIds: data.clubs?.map(c => c.clubId) ?? [],
    } : { isAllClubs: false, clubIds: [] })
    setError('')
    setModal({ mode: data.id ? 'edit' : 'create', data })
  }

  const save = async () => {
    setSaving(true); setError('')
    try {
      const body = {
        name: form.name,
        durationDays: form.durationDays ? Number(form.durationDays) : null,
        visitsLimit: form.visitsLimit ? Number(form.visitsLimit) : null,
        price: Number(form.price),
        isAllClubs: form.isAllClubs === true || form.isAllClubs === 'true',
        clubIds: form.isAllClubs ? [] : (form.clubIds ?? []),
      }
      if (modal.mode === 'create') await api.post('/subscription-types', body)
      else await api.put(`/subscription-types/${modal.data.id}`, body)
      setModal(null); load()
    } catch (e) { setError(e.message) }
    finally { setSaving(false) }
  }

  const remove = async (id) => {
    if (!window.confirm('Удалить тариф?')) return
    await api.del(`/subscription-types/${id}`); load()
  }

  const f = (k, v) => setForm(p => ({ ...p, [k]: v }))
  const toggleClub = (id) => {
    const cur = form.clubIds ?? []
    setForm(p => ({ ...p, clubIds: cur.includes(id) ? cur.filter(x => x !== id) : [...cur, id] }))
  }

  return (
    <div>
      <div className="page-header">
        <div><h2>Тарифы</h2><p>Типы абонементов</p></div>
        <button className="btn btn-primary" onClick={() => open()}>+ Добавить тариф</button>
      </div>

      <div className="table-wrap">
        {loading ? (
          <div className="empty-state"><p>Загрузка…</p></div>
        ) : items.length === 0 ? (
          <div className="empty-state"><p>Тарифов нет</p></div>
        ) : (
          <table>
            <thead>
              <tr><th>Название</th><th>Цена</th><th>Срок (дней)</th><th>Посещений</th><th>Клубы</th><th></th></tr>
            </thead>
            <tbody>
              {items.map(t => (
                <tr key={t.id}>
                  <td><b>{t.name}</b></td>
                  <td>{Number(t.price).toLocaleString('ru-RU')} ₽</td>
                  <td>{t.durationDays ?? '—'}</td>
                  <td>{t.visitsLimit ?? '∞'}</td>
                  <td>{t.isAllClubs ? 'Все клубы' : (t.clubs?.length ? t.clubs.map(c => c.club?.name ?? '').join(', ') : '—')}</td>
                  <td>
                    <div className="td-actions">
                      <button className="btn btn-ghost btn-sm" onClick={() => open(t)}>Изм.</button>
                      <button className="btn btn-danger btn-sm" onClick={() => remove(t.id)}>Удал.</button>
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
          <div className="modal-title">{modal.mode === 'create' ? 'Новый тариф' : 'Изменить тариф'}</div>
          <div className="modal-subtitle">Заполните данные</div>
          <div className="form-grid">
            <div className="field"><label>Название</label><input value={form.name ?? ''} onChange={e => f('name', e.target.value)} /></div>
            <div className="form-row">
              <div className="field"><label>Цена (₽)</label><input type="number" value={form.price ?? ''} onChange={e => f('price', e.target.value)} /></div>
              <div className="field"><label>Срок (дней)</label><input type="number" value={form.durationDays ?? ''} onChange={e => f('durationDays', e.target.value)} placeholder="без ограничений" /></div>
            </div>
            <div className="field"><label>Лимит посещений</label><input type="number" value={form.visitsLimit ?? ''} onChange={e => f('visitsLimit', e.target.value)} placeholder="без ограничений" /></div>
            <div className="field">
              <label style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer' }}>
                <input type="checkbox" checked={!!form.isAllClubs} onChange={e => f('isAllClubs', e.target.checked)} />
                Действует во всех клубах
              </label>
            </div>
            {!form.isAllClubs && (
              <div className="field">
                <label>Клубы</label>
                <div style={{ display: 'flex', flexDirection: 'column', gap: 6, marginTop: 4 }}>
                  {clubs.map(c => (
                    <label key={c.id} style={{ display: 'flex', alignItems: 'center', gap: 8, cursor: 'pointer', fontSize: 13 }}>
                      <input type="checkbox" checked={(form.clubIds ?? []).includes(c.id)} onChange={() => toggleClub(c.id)} />
                      {c.name}
                    </label>
                  ))}
                </div>
              </div>
            )}
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
