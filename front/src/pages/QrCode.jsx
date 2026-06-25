import { useEffect, useRef, useState } from 'react'
import QRCode from 'qrcode'
import { api } from '../api'

export default function QrCode() {
  const canvasRef  = useRef(null)
  const [loading, setLoading] = useState(true)
  const [error, setError]     = useState('')
  const [expiresAt, setExpiresAt] = useState(null)

  const generate = async () => {
    setLoading(true)
    setError('')
    try {
      const { token } = await api.get('/clients/me/qr-token')
      // decode expiry from JWT payload
      const payload = JSON.parse(atob(token.split('.')[1].replace(/-/g, '+').replace(/_/g, '/')))
      setExpiresAt(new Date(payload.exp * 1000))
      await QRCode.toCanvas(canvasRef.current, token, {
        width: 280,
        margin: 2,
        color: { dark: '#1e293b', light: '#ffffff' },
      })
    } catch (e) {
      setError(e.message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => { generate() }, []) // eslint-disable-line

  const fmt = (d) => d ? d.toLocaleDateString('ru-RU', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  }) : '—'

  return (
    <div className="page-container" style={{ maxWidth: 400, margin: '0 auto', textAlign: 'center' }}>
      <div className="page-header" style={{ textAlign: 'left' }}>
        <h1>Мой QR-код</h1>
        <p>Покажите на турникете при входе в клуб</p>
      </div>

      <div style={{
        background: 'var(--card)', borderRadius: 16, padding: 32,
        boxShadow: '0 2px 12px rgba(0,0,0,.08)',
        display: 'inline-flex', flexDirection: 'column', alignItems: 'center', gap: 16,
      }}>
        {loading && <p style={{ color: 'var(--muted)' }}>Генерация…</p>}
        {error   && <p style={{ color: '#ef4444' }}>{error}</p>}
        <canvas ref={canvasRef} style={{ display: loading || error ? 'none' : 'block', borderRadius: 8 }} />
        {!loading && !error && expiresAt && (
          <p style={{ fontSize: 12, color: 'var(--muted)', margin: 0 }}>
            Действителен до: {fmt(expiresAt)}
          </p>
        )}
        <button className="btn btn-ghost" onClick={generate} disabled={loading}>
          Обновить
        </button>
      </div>
    </div>
  )
}
