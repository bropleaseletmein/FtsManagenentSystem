import { Outlet, NavLink, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

export default function Layout() {
  const { client, logout } = useAuth()
  const navigate = useNavigate()

  const handleLogout = () => {
    logout()
    navigate('/login', { replace: true })
  }

  return (
    <div className="app-layout">
      <nav className="sidebar">
        <div className="sidebar-brand">
          <span className="brand-name">FitnessNetwork</span>
        </div>

        <ul className="nav-list">
          <li>
            <NavLink to="/dashboard" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              Главная
            </NavLink>
          </li>
          <li>
            <NavLink to="/schedule" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              Расписание
            </NavLink>
          </li>
          <li>
            <NavLink to="/bookings" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              Мои записи
            </NavLink>
          </li>
          <li>
            <NavLink to="/plans" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              Абонементы
            </NavLink>
          </li>
          <li>
            <NavLink to="/visits" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              Посещения
            </NavLink>
          </li>
          <li>
            <NavLink to="/qr" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              QR-код
            </NavLink>
          </li>
          <li>
            <NavLink to="/chat" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
              Чат с поддержкой
            </NavLink>
          </li>
        </ul>

        <div className="sidebar-user">
          <div className="user-name">
            {client ? `${client.firstName} ${client.lastName}` : '—'}
          </div>
          <div className="user-email">{client?.email ?? ''}</div>
          <button className="logout-btn" onClick={handleLogout}>Выйти</button>
        </div>
      </nav>

      <main className="main-content">
        <Outlet />
      </main>
    </div>
  )
}
