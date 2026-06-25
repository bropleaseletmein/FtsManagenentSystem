const LABELS = {
  active:    'Активен',
  frozen:    'Заморожен',
  expired:   'Истёк',
  cancelled: 'Отменён',
  pending:   'Ожидает',
  booked:    'Записан',
  scheduled: 'Запланировано',
  completed: 'Завершено',
}

export default function Badge({ status }) {
  return (
    <span className={`badge badge-${status}`}>
      {LABELS[status] ?? status}
    </span>
  )
}
