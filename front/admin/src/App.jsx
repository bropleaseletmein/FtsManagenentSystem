import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { AuthProvider, useAuth } from './context/AuthContext'
import Layout from './components/Layout'
import Login from './pages/Login'
import Dashboard from './pages/Dashboard'
import Clubs from './pages/Clubs'
import Staff from './pages/Staff'
import Schedule from './pages/Schedule'
import Clients from './pages/Clients'
import SubscriptionTypes from './pages/SubscriptionTypes'
import ClassTypes from './pages/ClassTypes'
import Visits from './pages/Visits'
import Reports from './pages/Reports'
import Chat from './pages/Chat'

function Protected({ children }) {
  const { token, loading } = useAuth()
  if (loading) return <div className="loading-screen">Загрузка…</div>
  return token ? children : <Navigate to="/login" replace />
}

function AppRoutes() {
  const { token } = useAuth()
  return (
    <Routes>
      <Route path="/login" element={token ? <Navigate to="/" replace /> : <Login />} />
      <Route path="/" element={<Protected><Layout /></Protected>}>
        <Route index element={<Dashboard />} />
        <Route path="clubs" element={<Clubs />} />
        <Route path="staff" element={<Staff />} />
        <Route path="schedule" element={<Schedule />} />
        <Route path="clients" element={<Clients />} />
        <Route path="subscription-types" element={<SubscriptionTypes />} />
        <Route path="class-types" element={<ClassTypes />} />
        <Route path="visits" element={<Visits />} />
        <Route path="reports" element={<Reports />} />
        <Route path="chat"    element={<Chat />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default function App() {
  return (
    <AuthProvider>
      <BrowserRouter>
        <AppRoutes />
      </BrowserRouter>
    </AuthProvider>
  )
}
