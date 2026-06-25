import { NavLink, Outlet, useLocation } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

const NAV = [
  { section: 'Основное' },
  { to: '/',                    label: 'Дашборд' },
  { section: 'Управление' },
  { to: '/clubs',               label: 'Клубы и залы' },
  { to: '/staff',               label: 'Сотрудники' },
  { to: '/clients',             label: 'Клиенты' },
  { section: 'Занятия' },
  { to: '/schedule',            label: 'Расписание' },
  { to: '/class-types',         label: 'Типы занятий' },
  { section: 'Абонементы' },
  { to: '/subscription-types',  label: 'Тарифы' },
  { section: 'Контроль доступа' },
  { to: '/visits',              label: 'Посещения' },
  { section: 'Аналитика' },
  { to: '/reports',             label: 'Отчёты' },
  { section: 'Коммуникации' },
  { to: '/chat',                label: 'Чат с клиентами' },
]

const PAGE_TITLES = {
  '/':                   'Дашборд',
  '/clubs':              'Клубы и залы',
  '/staff':              'Сотрудники',
  '/clients':            'Клиенты',
  '/schedule':           'Расписание',
  '/class-types':        'Типы занятий',
  '/subscription-types': 'Тарифы',
  '/visits':             'Посещения',
  '/reports':            'Отчёты',
  '/chat':               'Чат с клиентами',
}

export default function Layout() {
  const { staff, logout } = useAuth()
  const { pathname } = useLocation()
  const pageTitle = PAGE_TITLES[pathname] ?? 'Панель управления'

  return (
    <div className="layout">
      <aside className="sidebar">
        <div className="sidebar-brand">
          <div className="sidebar-brand-name">FitnessNetwork</div>
          <div className="sidebar-brand-sub">Панель управления</div>
        </div>

        <nav className="sidebar-nav">
          {NAV.map((item, i) =>
            item.section ? (
              <div key={i} className="nav-section">{item.section}</div>
            ) : (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/'}
                className={({ isActive }) => 'nav-link' + (isActive ? ' active' : '')}
              >
                {item.label}
              </NavLink>
            )
          )}
        </nav>

        <div className="sidebar-footer">
          <div className="sidebar-user">
            <b>{staff ? `${staff.firstName} ${staff.lastName}` : '…'}</b>
            {staff?.roles?.map(r => r.role).join(', ') ?? ''}
          </div>
          <button
            className="btn btn-ghost btn-sm"
            style={{ width: '100%', justifyContent: 'center' }}
            onClick={logout}
          >
            Выйти
          </button>
        </div>
      </aside>

      <div className="main">
        <div className="topbar">{pageTitle}</div>
        <div className="page">
          <Outlet />
        </div>
      </div>
    </div>
  )
}
