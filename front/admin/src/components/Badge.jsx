const MAP = {
  active:    ['badge-active',    'Активен'],
  frozen:    ['badge-frozen',    'Заморожен'],
  expired:   ['badge-expired',   'Истёк'],
  cancelled: ['badge-cancelled', 'Отменён'],
  pending:   ['badge-pending',   'Ожидание'],
  scheduled: ['badge-scheduled', 'Запланировано'],
  completed: ['badge-completed', 'Завершено'],
  booked:    ['badge-booked',    'Записан'],
  admin:     ['badge-admin',     'Админ'],
  trainer:   ['badge-trainer',   'Тренер'],
}

export default function Badge({ status }) {
  const [cls, label] = MAP[status] ?? ['badge-expired', status ?? '—']
  return <span className={`badge ${cls}`}>{label}</span>
}
