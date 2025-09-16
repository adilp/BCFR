import { useEffect, useState } from 'react'
import { CheckCircleIcon, XCircleIcon, InformationCircleIcon, ExclamationTriangleIcon } from '@heroicons/react/24/outline'
import './Snackbar.css'

export type SnackbarType = 'success' | 'error' | 'info' | 'warning'

interface SnackbarProps {
  message: string
  type?: SnackbarType
  duration?: number
  onClose?: () => void
  action?: {
    label: string
    onClick: () => void
  }
}

const iconMap = {
  success: CheckCircleIcon,
  error: XCircleIcon,
  info: InformationCircleIcon,
  warning: ExclamationTriangleIcon
}

export function Snackbar({ message, type = 'info', duration = 3000, onClose, action }: SnackbarProps) {
  const [isVisible, setIsVisible] = useState(true)
  const [isLeaving, setIsLeaving] = useState(false)

  useEffect(() => {
    if (duration > 0) {
      const timer = setTimeout(() => {
        handleClose()
      }, duration)
      return () => clearTimeout(timer)
    }
  }, [duration])

  const handleClose = () => {
    setIsLeaving(true)
    setTimeout(() => {
      setIsVisible(false)
      onClose?.()
    }, 300)
  }

  if (!isVisible) return null

  const Icon = iconMap[type]

  return (
    <div className={`snackbar snackbar-${type} ${isLeaving ? 'snackbar-leaving' : ''}`}>
      <div className="snackbar-content">
        <Icon className="snackbar-icon" />
        <span className="snackbar-message">{message}</span>
      </div>
      {action && (
        <button className="snackbar-action" onClick={action.onClick}>
          {action.label}
        </button>
      )}
      <button className="snackbar-close" onClick={handleClose}>
        <XCircleIcon className="snackbar-close-icon" />
      </button>
    </div>
  )
}

interface SnackbarItem {
  id: string
  message: string
  type?: SnackbarType
  duration?: number
  action?: {
    label: string
    onClick: () => void
  }
}

let snackbarQueue: SnackbarItem[] = []
let showSnackbarCallback: ((item: SnackbarItem) => void) | null = null

export function showSnackbar(message: string, type?: SnackbarType, duration?: number, action?: { label: string; onClick: () => void }) {
  const item: SnackbarItem = {
    id: Date.now().toString(),
    message,
    type,
    duration,
    action
  }

  if (showSnackbarCallback) {
    showSnackbarCallback(item)
  } else {
    snackbarQueue.push(item)
  }
}

export function SnackbarContainer() {
  const [snackbars, setSnackbars] = useState<SnackbarItem[]>([])

  useEffect(() => {
    showSnackbarCallback = (item: SnackbarItem) => {
      setSnackbars(prev => [...prev, item])
    }

    // Process any queued snackbars
    if (snackbarQueue.length > 0) {
      snackbarQueue.forEach(item => {
        setSnackbars(prev => [...prev, item])
      })
      snackbarQueue = []
    }

    return () => {
      showSnackbarCallback = null
    }
  }, [])

  const removeSnackbar = (id: string) => {
    setSnackbars(prev => prev.filter(s => s.id !== id))
  }

  return (
    <div className="snackbar-container">
      {snackbars.map((snackbar, index) => (
        <div key={snackbar.id} style={{ transform: `translateY(${index * -70}px)` }}>
          <Snackbar
            message={snackbar.message}
            type={snackbar.type}
            duration={snackbar.duration}
            action={snackbar.action}
            onClose={() => removeSnackbar(snackbar.id)}
          />
        </div>
      ))}
    </div>
  )
}

export default Snackbar