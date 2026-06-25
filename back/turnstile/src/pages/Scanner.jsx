import { useCallback, useEffect, useRef, useState } from 'react'
import jsQR from 'jsqr'

const API = 'http://localhost:5224'
const RESET_MS = 4000

export default function Scanner() {
  const [clubs, setClubs]       = useState([])
  const [clubId, setClubId]     = useState(() => localStorage.getItem('turnstile_club') ?? '')
  const [mode, setMode]         = useState('entry')
  const [result, setResult]     = useState(null)
  const [scanning, setScanning] = useState(false)
  const fileRef   = useRef(null)
  const canvasRef = useRef(null)
  const timer     = useRef(null)

  useEffect(() => {
    fetch(`${API}/clubs`)
      .then(r => r.json())
      .then(data => {
        setClubs(data ?? [])
        if (!clubId && data?.length) {
          setClubId(data[0].id)
          localStorage.setItem('turnstile_club', data[0].id)
        }
      })
      .catch(() => {})
  }, []) // eslint-disable-line

  const reset = useCallback(() => {
    setResult(null)
    setScanning(false)
    if (fileRef.current) fileRef.current.value = ''
    clearTimeout(timer.current)
  }, [])

  useEffect(() => {
    if (result) {
      clearTimeout(timer.current)
      timer.current = setTimeout(reset, RESET_MS)
    }
    return () => clearTimeout(timer.current)
  }, [result, reset])

  const handleClub = (id) => {
    setClubId(id)
    localStorage.setItem('turnstile_club', id)
  }

  const processImage = async (file) => {
    if (!file || !clubId) return
    setScanning(true)
    setResult(null)

    const img = new Image()
    img.src = URL.createObjectURL(file)
    await new Promise(res => { img.onload = res })

    const canvas = canvasRef.current
    canvas.width  = img.width
    canvas.height = img.height
    canvas.getContext('2d').drawImage(img, 0, 0)
    URL.revokeObjectURL(img.src)

    const imageData = canvas.getContext('2d').getImageData(0, 0, canvas.width, canvas.height)
    const code = jsQR(imageData.data, imageData.width, imageData.height)

    if (!code) {
      setResult({ allowed: false, message: 'QR-код не распознан' })
      setScanning(false)
      return
    }

    try {
      const res = await fetch(`${API}/turnstile/scan`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ qrToken: code.data, clubId, mode }),
      })
      setResult(await res.json())
    } catch {
      setResult({ allowed: false, message: 'Ошибка соединения с сервером' })
    } finally {
      setScanning(false)
    }
  }

  const clubName = clubs.find(c => c.id === clubId)?.name ?? ''
  const isEntry  = mode === 'entry'
  const allowed  = result?.allowed
  const bgColor  = result === null ? '#0f172a' : allowed ? '#14532d' : '#450a0a'

  return (
    <div style={{ ...s.wrap, background: bgColor }}>
      <canvas ref={canvasRef} style={{ display: 'none' }} />

      <div style={s.header}>
        <span style={s.brand}>FitnessNetwork — Турникет</span>
        <div style={{ display: 'flex', gap: 12, alignItems: 'center' }}>
          <select value={clubId} onChange={e => handleClub(e.target.value)} style={s.select}>
            {clubs.length === 0 && <option value="">Загрузка…</option>}
            {clubs.map(c => <option key={c.id} value={c.id}>{c.name}</option>)}
          </select>

          <div style={s.toggle}>
            <button
              style={{ ...s.toggleBtn, ...(isEntry ? s.toggleActive : {}) }}
              onClick={() => { setMode('entry'); reset() }}
            >
              Вход
            </button>
            <button
              style={{ ...s.toggleBtn, ...(!isEntry ? s.toggleActive : {}) }}
              onClick={() => { setMode('exit'); reset() }}
            >
              Выход
            </button>
          </div>
        </div>
      </div>

      <div style={s.center}>
        {scanning && (
          <h1 style={s.title}>Проверка…</h1>
        )}

        {!scanning && result === null && (
          <>
            <h1 style={s.title}>
              {isEntry ? 'Сканирование на вход' : 'Сканирование на выход'}
            </h1>
            {clubName && <p style={s.sub}>{clubName}</p>}
            <button style={s.uploadBtn} onClick={() => fileRef.current?.click()}>
              Загрузить QR-код
            </button>
          </>
        )}

        {!scanning && result !== null && (
          <>
            <h1 style={{ ...s.resultTitle, color: allowed ? '#86efac' : '#fca5a5' }}>
              {allowed
                ? (isEntry ? 'ДОСТУП РАЗРЕШЁН' : 'ВЫХОД ЗАФИКСИРОВАН')
                : (isEntry ? 'ДОСТУП ЗАПРЕЩЁН' : 'ВЫХОД ОТКЛОНЁН')}
            </h1>
            {result.clientName && <p style={s.clientName}>{result.clientName}</p>}
            <p style={s.msg}>{result.message}</p>
            {result.subscriptionName && <p style={s.subName}>{result.subscriptionName}</p>}
            <button style={{ ...s.uploadBtn, marginTop: 32 }} onClick={reset}>
              Сканировать снова
            </button>
          </>
        )}
      </div>

      <input
        ref={fileRef} type="file" accept="image/*"
        style={{ display: 'none' }}
        onChange={e => processImage(e.target.files?.[0])}
      />
    </div>
  )
}

const s = {
  wrap: {
    minHeight: '100vh', display: 'flex', flexDirection: 'column',
    fontFamily: 'system-ui, sans-serif', color: '#f1f5f9',
    transition: 'background .3s',
  },
  header: {
    display: 'flex', justifyContent: 'space-between', alignItems: 'center',
    padding: '12px 20px', background: 'rgba(0,0,0,.3)',
    borderBottom: '1px solid rgba(255,255,255,.06)', flexWrap: 'wrap', gap: 10,
  },
  brand: { fontSize: 13, fontWeight: 600, color: '#94a3b8', letterSpacing: '.02em' },
  select: {
    background: 'rgba(255,255,255,.08)', border: '1px solid rgba(255,255,255,.12)',
    borderRadius: 4, color: '#f1f5f9', fontSize: 13, padding: '6px 10px',
    cursor: 'pointer', outline: 'none',
  },
  toggle: {
    display: 'flex', background: 'rgba(0,0,0,.3)', borderRadius: 4,
    border: '1px solid rgba(255,255,255,.1)', overflow: 'hidden',
  },
  toggleBtn: {
    padding: '6px 18px', border: 'none', background: 'transparent',
    color: '#64748b', fontSize: 13, fontWeight: 600, cursor: 'pointer',
  },
  toggleActive: {
    background: '#1d4ed8', color: '#fff',
  },
  center: {
    flex: 1, display: 'flex', flexDirection: 'column',
    alignItems: 'center', justifyContent: 'center',
    padding: 40, textAlign: 'center',
  },
  title:       { fontSize: 26, fontWeight: 600, color: '#e2e8f0', margin: '0 0 8px' },
  sub:         { fontSize: 14, color: '#64748b', margin: '0 0 28px' },
  uploadBtn: {
    padding: '12px 36px', borderRadius: 4, border: 'none',
    background: '#1d4ed8', color: '#fff', fontSize: 15, fontWeight: 600,
    cursor: 'pointer',
  },
  resultTitle: { fontSize: 36, fontWeight: 700, margin: '0 0 16px', letterSpacing: 1 },
  clientName:  { fontSize: 28, fontWeight: 700, color: '#f1f5f9', margin: '0 0 8px' },
  msg:         { fontSize: 15, color: '#94a3b8', margin: '0 0 4px' },
  subName:     { fontSize: 13, color: '#64748b', margin: 0 },
}
