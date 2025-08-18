import type { ReactNode } from 'react';
import { XMarkIcon } from '@heroicons/react/24/outline';
import './Drawer.css';

interface DrawerProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: ReactNode;
  footer?: ReactNode;
  size?: 'sm' | 'md' | 'lg' | 'xl';
  position?: 'left' | 'right';
}

function Drawer({ 
  isOpen, 
  onClose, 
  title, 
  children, 
  footer,
  size = 'lg',
  position = 'right' 
}: DrawerProps) {
  if (!isOpen) return null;

  return (
    <>
      <div className="drawer-overlay active" onClick={onClose} />
      <div className={`drawer drawer-${size} drawer-${position} open`}>
        <div className="drawer-header">
          <h2 className="drawer-title">{title}</h2>
          <button className="drawer-close" onClick={onClose} aria-label="Close drawer">
            <XMarkIcon className="icon-md" />
          </button>
        </div>
        
        <div className="drawer-body">
          {children}
        </div>
        
        {footer && (
          <div className="drawer-footer">
            {footer}
          </div>
        )}
      </div>
    </>
  );
}

export default Drawer;