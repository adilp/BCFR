import type { ReactNode } from 'react';
import './FormSection.css';

interface FormSectionProps {
  title: string;
  description?: string;
  children: ReactNode;
  actions?: ReactNode;
}

export function FormSection({ title, description, children, actions }: FormSectionProps) {
  return (
    <div className="form-section">
      <div className="form-section-header">
        <div>
          <h3 className="form-section-title">{title}</h3>
          {description && <p className="form-section-description">{description}</p>}
        </div>
        {actions && <div className="form-section-actions">{actions}</div>}
      </div>
      <div className="form-section-content">{children}</div>
    </div>
  );
}

interface FormGroupProps {
  label: string;
  required?: boolean;
  error?: string;
  help?: string;
  children: ReactNode;
  fullWidth?: boolean;
}

export function FormGroup({ label, required, error, help, children, fullWidth }: FormGroupProps) {
  return (
    <div className={`form-group ${fullWidth ? 'full-width' : ''} ${error ? 'has-error' : ''}`}>
      <label className={`form-label ${required ? 'required' : ''}`}>
        {label}
      </label>
      {children}
      {error && <span className="form-error">{error}</span>}
      {help && !error && <span className="form-help">{help}</span>}
    </div>
  );
}

interface FormGridProps {
  columns?: 1 | 2 | 3 | 4;
  gap?: 'sm' | 'md' | 'lg';
  children: ReactNode;
}

export function FormGrid({ columns = 2, gap = 'md', children }: FormGridProps) {
  return (
    <div className={`form-grid form-grid-${columns} form-grid-gap-${gap}`}>
      {children}
    </div>
  );
}