import { useEffect, useRef, useState } from 'react'
import { HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr'
import { useAuth } from '../context/AuthContext'
import { parseJwt } from '../api'

export default function Chat() {
  const { client, token } = useAuth()
  const [messages, setMessages] = useState([])
  const [text, setText]         = useState('')
  const [status, setStatus]     = useState('disconnected') // disconnected | connecting | connected | reconnecting
  const [error, setError]       = useState('')
  const connRef  = useRef(null)
  const bottomRef = useRef(null)

  const roomId = token ? parseJwt(token).sub : null
  const myName = client ? `${client.firstName} ${client.lastName}` : 'Клиент'

  useEffect(() => {
    if (!token || !roomId) return
    let cancelled = false

    const conn = new HubConnectionBuilder()
      .withUrl('/hubs/chat', { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    conn.on('ReceiveMessage', msg => setMessages(prev => [...prev, msg]))
    conn.onreconnecting(() => setStatus('reconnecting'))
    conn.onreconnected(() => setStatus('connected'))
    conn.onclose(() => { if (!cancelled) setStatus('disconnected') })

    setStatus('connecting')
    conn.start()
      .then(async () => {
        if (cancelled) return
        await conn.invoke('JoinRoom', roomId)
        const res = await fetch(`/chat/${roomId}/history`, {
          headers: { Authorization: `Bearer ${token}` },
        })
        const history = res.ok ? await res.json() : []
        setMessages(history)
        setStatus('connected')
      })
      .catch(e => {
        if (cancelled) return  // StrictMode cleanup — не показываем ошибку
        console.error('SignalR connect error:', e)
        setError('Не удалось подключиться: ' + (e.message ?? e))
        setStatus('disconnected')
      })

    connRef.current = conn
    return () => { cancelled = true; conn.stop() }
  }, [token, roomId]) // eslint-disable-line

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const send = async () => {
    const conn = connRef.current
    if (!text.trim() || !conn || conn.state !== HubConnectionState.Connected) return
    setError('')
    try {
      await conn.invoke('SendMessage', roomId, text.trim(), myName)
      setText('')
    } catch (e) {
      setError('Ошибка отправки: ' + (e.message ?? e))
    }
  }

  const onKey = (e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send() } }

  const reconnect = () => {
    const conn = connRef.current
    if (!conn || status === 'connected' || status === 'connecting') return
    setError('')
    setStatus('connecting')
    conn.start()
      .then(async () => {
        await conn.invoke('JoinRoom', roomId)
        setStatus('connected')
      })
      .catch(e => {
        setError('Не удалось подключиться: ' + (e.message ?? e))
        setStatus('disconnected')
      })
  }

  const statusLabel = {
    connected:    'Подключено',
    connecting:   'Подключение…',
    reconnecting: 'Переподключение…',
    disconnected: 'Нет соединения',
  }[status]

  const canSend = status === 'connected' && text.trim().length > 0

  return (
    <div className="page-content">
      <div className="page-header">
        <div>
          <h2>Чат с поддержкой</h2>
          <p style={{ color: status === 'connected' ? 'var(--success,#16a34a)' : 'var(--muted,#6b7280)' }}>
            {statusLabel}
          </p>
        </div>
      </div>

      {error && (
        <div className="error-box" style={{ marginBottom: 12, display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <span>{error}</span>
          <button className="btn btn-ghost btn-sm" onClick={reconnect}>Переподключить</button>
        </div>
      )}

      <div style={s.wrap}>
        <div style={s.messages}>
          {messages.length === 0 && (
            <div style={s.empty}>Нет сообщений. Напишите нам!</div>
          )}
          {messages.map((m, i) => {
            const isMe = m.senderRole === 'client'
            return (
              <div key={i} style={{ ...s.row, justifyContent: isMe ? 'flex-end' : 'flex-start' }}>
                <div style={{ ...s.bubble, ...(isMe ? s.bubbleMe : s.bubbleThem) }}>
                  {!isMe && <div style={s.sender}>{m.senderName}</div>}
                  <div>{m.text}</div>
                  <div style={s.time}>
                    {new Date(m.sentAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}
                  </div>
                </div>
              </div>
            )
          })}
          <div ref={bottomRef} />
        </div>

        <div style={s.inputRow}>
          <input
            style={s.input}
            value={text}
            onChange={e => setText(e.target.value)}
            onKeyDown={onKey}
            placeholder={status === 'connected' ? 'Сообщение…' : statusLabel}
            disabled={status !== 'connected'}
          />
          <button className="btn btn-primary" onClick={send} disabled={!canSend}>
            Отправить
          </button>
        </div>
      </div>
    </div>
  )
}

const s = {
  wrap:       { display: 'flex', flexDirection: 'column', gap: 12, height: 'calc(100vh - 220px)' },
  messages:   { flex: 1, overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: 8, padding: '4px 0' },
  empty:      { color: 'var(--muted,#6b7280)', textAlign: 'center', marginTop: 40, fontSize: 14 },
  row:        { display: 'flex' },
  bubble:     { maxWidth: '70%', padding: '8px 12px', borderRadius: 8, fontSize: 14, lineHeight: 1.4 },
  bubbleMe:   { background: 'var(--accent,#1d4ed8)', color: '#fff', borderBottomRightRadius: 2 },
  bubbleThem: { background: '#f3f4f6', color: '#111', borderBottomLeftRadius: 2 },
  sender:     { fontSize: 11, fontWeight: 600, marginBottom: 3, opacity: 0.7 },
  time:       { fontSize: 10, opacity: 0.6, marginTop: 4, textAlign: 'right' },
  inputRow:   { display: 'flex', gap: 8 },
  input:      { flex: 1, padding: '8px 12px', borderRadius: 6, border: '1px solid var(--border,#e5e7eb)', fontSize: 14 },
}
