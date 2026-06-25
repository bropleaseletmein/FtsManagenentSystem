import { useEffect, useRef, useState } from 'react'
import { HubConnectionBuilder, LogLevel } from '@microsoft/signalr'
import { useAuth } from '../context/AuthContext'
import { api } from '../api'

export default function Chat() {
  const { staff, token } = useAuth()
  const [rooms, setRooms]         = useState([])
  const [roomId, setRoomId]       = useState(null)
  const [messages, setMessages]   = useState([])
  const [text, setText]           = useState('')
  const [connected, setConnected] = useState(false)
  const connRef   = useRef(null)
  const bottomRef = useRef(null)

  const myName = staff ? `${staff.firstName} ${staff.lastName}` : 'Сотрудник'

  useEffect(() => {
    api.get('/chat/rooms').then(r => setRooms(r ?? [])).catch(() => {})
  }, [])

  useEffect(() => {
    if (!token || !roomId) return
    let cancelled = false
    setMessages([])
    setConnected(false)

    const conn = new HubConnectionBuilder()
      .withUrl('/hubs/chat', { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build()

    conn.on('ReceiveMessage', msg => setMessages(prev => [...prev, msg]))

    conn.start().then(async () => {
      if (cancelled) return
      await conn.invoke('JoinRoom', roomId)
      const res = await fetch(`/chat/${roomId}/history`, {
        headers: { Authorization: `Bearer ${token}` },
      })
      const history = res.ok ? await res.json() : []
      setMessages(history)
      setConnected(true)
    }).catch(e => { if (!cancelled) console.error('SignalR:', e) })

    connRef.current = conn
    return () => { cancelled = true; conn.stop() }
  }, [token, roomId])

  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages])

  const send = async () => {
    if (!text.trim() || !connRef.current) return
    await connRef.current.invoke('SendMessage', roomId, text.trim(), myName)
    setText('')
  }

  const onKey = (e) => { if (e.key === 'Enter' && !e.shiftKey) { e.preventDefault(); send() } }

  return (
    <div>
      <div className="page-header">
        <div><h2>Чат с клиентами</h2></div>
      </div>

      <div style={s.layout}>
        <div style={s.sidebar}>
          <div style={s.sidebarTitle}>Диалоги</div>
          {rooms.length === 0 && <div style={s.noRooms}>Нет активных чатов</div>}
          {rooms.map(id => (
            <div
              key={id}
              style={{ ...s.room, ...(id === roomId ? s.roomActive : {}) }}
              onClick={() => setRoomId(id)}
            >
              <div style={s.roomId}>{id.slice(0, 8)}…</div>
            </div>
          ))}
        </div>

        <div style={s.main}>
          {!roomId ? (
            <div style={s.pick}>Выберите диалог слева</div>
          ) : (
            <>
              <div style={s.messages}>
                {messages.length === 0 && <div style={s.pick}>Нет сообщений</div>}
                {messages.map((m, i) => {
                  const isMe = m.senderRole !== 'client'
                  return (
                    <div key={i} style={{ ...s.row, justifyContent: isMe ? 'flex-end' : 'flex-start' }}>
                      <div style={{ ...s.bubble, ...(isMe ? s.bubbleMe : s.bubbleThem) }}>
                        <div style={s.sender}>{m.senderName}</div>
                        <div>{m.text}</div>
                        <div style={s.time}>{new Date(m.sentAt).toLocaleTimeString('ru-RU', { hour: '2-digit', minute: '2-digit' })}</div>
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
                  placeholder={connected ? 'Сообщение…' : 'Подключение…'}
                  disabled={!connected}
                />
                <button className="btn btn-primary" onClick={send} disabled={!connected || !text.trim()}>
                  Отправить
                </button>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  )
}

const s = {
  layout:   { display: 'flex', gap: 0, border: '1px solid var(--border)', borderRadius: 6, overflow: 'hidden', height: 'calc(100vh - 160px)' },
  sidebar:  { width: 220, borderRight: '1px solid var(--border)', overflowY: 'auto', background: 'var(--sidebar-item-bg, #f9fafb)' },
  sidebarTitle: { padding: '12px 14px', fontSize: 11, fontWeight: 700, textTransform: 'uppercase', letterSpacing: '.05em', color: 'var(--muted, #6b7280)', borderBottom: '1px solid var(--border)' },
  noRooms:  { padding: '12px 14px', fontSize: 13, color: 'var(--muted, #6b7280)' },
  room:     { padding: '10px 14px', cursor: 'pointer', borderBottom: '1px solid var(--border)', fontSize: 13 },
  roomActive: { background: 'var(--accent, #1d4ed8)', color: '#fff' },
  roomId:   { fontFamily: 'monospace', fontSize: 12 },
  main:     { flex: 1, display: 'flex', flexDirection: 'column' },
  pick:     { flex: 1, display: 'flex', alignItems: 'center', justifyContent: 'center', color: 'var(--muted, #6b7280)', fontSize: 14 },
  messages: { flex: 1, overflowY: 'auto', padding: 16, display: 'flex', flexDirection: 'column', gap: 8 },
  row:      { display: 'flex' },
  bubble:   { maxWidth: '70%', padding: '8px 12px', borderRadius: 8, fontSize: 14, lineHeight: 1.4 },
  bubbleMe:     { background: 'var(--accent, #1d4ed8)', color: '#fff', borderBottomRightRadius: 2 },
  bubbleThem:   { background: '#f3f4f6', color: '#111', borderBottomLeftRadius: 2 },
  sender:   { fontSize: 11, fontWeight: 600, marginBottom: 3, opacity: 0.7 },
  time:     { fontSize: 10, opacity: 0.6, marginTop: 4, textAlign: 'right' },
  inputRow: { display: 'flex', gap: 8, padding: 12, borderTop: '1px solid var(--border)' },
  input:    { flex: 1, padding: '8px 12px', borderRadius: 6, border: '1px solid var(--border)', fontSize: 14 },
}
