export default function Modal({ onClose, children }) {
  return (
    <div className="modal-overlay" onClick={e => e.target === e.currentTarget && onClose()}>
      <div className="modal-box">
        {children}
      </div>
    </div>
  )
}
