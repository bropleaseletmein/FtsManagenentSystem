import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider, useAuth } from './context/AuthContext'
import Layout from './components/Layout'
import Login from './pages/Login'
import Dashboard from './pages/Dashboard'
import Schedule from './pages/Schedule'
import Bookings from './pages/Bookings'
import Plans from './pages/Plans'
import Visits from './pages/Visits'
import QrCode from './pages/QrCode'
import Chat from './pages/Chat'

function Protected({ children }) {
  const { token, loading } = useAuth()
  if (loading) return <div className="loading-screen">Загрузка…</div>
  if (!token) return <Navigate to="/login" replace />
  return children
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route
            path="/"
            element={<Protected><Layout /></Protected>}
          >
            <Route index element={<Navigate to="/dashboard" replace />} />
            <Route path="dashboard" element={<Dashboard />} />
            <Route path="schedule"  element={<Schedule />} />
            <Route path="bookings"  element={<Bookings />} />
            <Route path="plans"     element={<Plans />} />
            <Route path="visits"    element={<Visits />} />
            <Route path="qr"        element={<QrCode />} />
            <Route path="chat"      element={<Chat />} />
          </Route>
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  )
}
