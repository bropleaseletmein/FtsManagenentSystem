import { useEffect, useState } from 'react'
import { api } from '../api'

export default function Plans() {
  const [plans, setPlans]   = useState([])
  const [loading, setLoading] = useState(true)
  const [error, setError]   = useState('')

  useEffect(() => {
    api.get('/subscription-types')
      .then(setPlans)
      .catch(err => setError(err.message))
      .finally(() => setLoading(false))
  }, [])

  return (
    <div>
      <div className="page-header">
        <h2>Доступные абонементы</h2>
        <p>Тарифные планы сети FitnessNetwork</p>
      </div>

      {loading ? (
        <div className="empty-state"><p>Загрузка…</p></div>
      ) : error ? (
        <div className="empty-state"><p>Ошибка: {error}</p></div>
      ) : plans.length === 0 ? (
        <div className="empty-state">
          <p>Нет доступных абонементов</p>
        </div>
      ) : (
        <div className="cards-grid">
          {plans.map(p => (
            <div key={p.id} className="plan-card">
              <div className="plan-name">{p.name}</div>
              <div className="plan-price">
                {Number(p.price).toLocaleString('ru-RU')} ₽
              </div>

              {p.durationDays != null && (
                <div className="plan-feature">
                  Срок: <span>{p.durationDays} дней</span>
                </div>
              )}
              <div className="plan-feature">
                Посещений: <span>{p.visitsLimit != null ? p.visitsLimit : 'безлимит'}</span>
              </div>
              <div className="plan-feature">
                Клубы: <span>{p.isAllClubs ? 'Все клубы сети' : 'Отдельные клубы'}</span>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  )
}
