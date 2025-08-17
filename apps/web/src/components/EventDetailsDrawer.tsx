import { useState, useEffect } from 'react';
import { XMarkIcon } from '@heroicons/react/24/outline';
import { formatForDateInput, addDays } from '@memberorg/shared';
import './EventDetailsDrawer.css';

interface Event {
  id: string;
  title: string;
  description: string;
  eventDate: string;
  eventTime: string;
  endTime: string;
  location: string;
  speaker: string;
  speakerTitle?: string;
  speakerBio?: string;
  rsvpDeadline: string;
  maxAttendees?: number;
  allowPlusOne: boolean;
  status: 'draft' | 'published' | 'cancelled';
}

interface EventDetailsDrawerProps {
  event: Event;
  isNew?: boolean;
  onClose: () => void;
  onSave: (event: Event) => void;
}

function EventDetailsDrawer({ event, isNew = false, onClose, onSave }: EventDetailsDrawerProps) {
  // Initialize with default values for new events
  const getInitialFormData = () => {
    if (isNew) {
      const today = new Date();
      const nextWeek = addDays(today, 7);
      const rsvpDeadline = addDays(nextWeek, -3);
      
      return {
        id: '',
        title: '',
        description: '',
        eventDate: formatForDateInput(nextWeek),
        eventTime: '12:00',
        endTime: '13:30',
        location: 'The Club Birmingham, Downtown',
        speaker: '',
        speakerTitle: '',
        speakerBio: '',
        rsvpDeadline: formatForDateInput(rsvpDeadline),
        maxAttendees: undefined,
        allowPlusOne: true,
        status: 'draft' as const
      };
    }
    return event;
  };

  const [formData, setFormData] = useState<Event>(getInitialFormData());
  const [errors, setErrors] = useState<Record<string, string>>({});

  useEffect(() => {
    if (!isNew && event) {
      // Format dates for HTML input fields
      setFormData({
        ...event,
        eventDate: formatForDateInput(event.eventDate),
        rsvpDeadline: formatForDateInput(event.rsvpDeadline)
      });
    }
  }, [event, isNew]);

  const handleChange = (field: keyof Event, value: any) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    // Clear error for this field
    if (errors[field]) {
      setErrors(prev => ({ ...prev, [field]: '' }));
    }
  };

  const validateForm = () => {
    const newErrors: Record<string, string> = {};

    if (!formData.title || !formData.title.trim()) {
      newErrors.title = 'Title is required';
    }
    if (!formData.description || !formData.description.trim()) {
      newErrors.description = 'Description is required';
    }
    if (!formData.eventDate) {
      newErrors.eventDate = 'Event date is required';
    }
    if (!formData.eventTime) {
      newErrors.eventTime = 'Start time is required';
    }
    if (!formData.endTime) {
      newErrors.endTime = 'End time is required';
    }
    if (!formData.location || !formData.location.trim()) {
      newErrors.location = 'Location is required';
    }
    if (!formData.speaker || !formData.speaker.trim()) {
      newErrors.speaker = 'Speaker is required';
    }
    if (!formData.rsvpDeadline) {
      newErrors.rsvpDeadline = 'RSVP deadline is required';
    }

    // Validate RSVP deadline is before event date
    if (formData.rsvpDeadline && formData.eventDate) {
      if (new Date(formData.rsvpDeadline) >= new Date(formData.eventDate)) {
        newErrors.rsvpDeadline = 'RSVP deadline must be before event date';
      }
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (validateForm()) {
      onSave(formData);
    }
  };

  return (
    <div className="event-drawer-overlay" onClick={onClose}>
      <div className="event-drawer-container" onClick={e => e.stopPropagation()}>
        <div className="drawer-header">
          <h2>{isNew ? 'Create New Event' : 'Edit Event'}</h2>
          <button className="close-btn" onClick={onClose}>
            <XMarkIcon className="icon-md" />
          </button>
        </div>

        <form onSubmit={handleSubmit} className="drawer-content">
          <div className="form-section">
            <h3>Event Information</h3>
            
            <div className="form-group">
              <label className="form-label required">Event Title</label>
              <input
                type="text"
                className={`form-input ${errors.title ? 'error' : ''}`}
                value={formData.title}
                onChange={(e) => handleChange('title', e.target.value)}
                placeholder="e.g., The Future of NATO"
              />
              {errors.title && <span className="error-message">{errors.title}</span>}
            </div>

            <div className="form-group">
              <label className="form-label required">Description</label>
              <textarea
                className={`form-textarea ${errors.description ? 'error' : ''}`}
                value={formData.description}
                onChange={(e) => handleChange('description', e.target.value)}
                rows={3}
                placeholder="Brief description of the event..."
              />
              {errors.description && <span className="error-message">{errors.description}</span>}
            </div>

            <div className="form-row">
              <div className="form-group">
                <label className="form-label required">Event Date</label>
                <input
                  type="date"
                  className={`form-input ${errors.eventDate ? 'error' : ''}`}
                  value={formData.eventDate}
                  onChange={(e) => handleChange('eventDate', e.target.value)}
                />
                {errors.eventDate && <span className="error-message">{errors.eventDate}</span>}
              </div>

              <div className="form-group">
                <label className="form-label required">Start Time</label>
                <input
                  type="time"
                  className={`form-input ${errors.eventTime ? 'error' : ''}`}
                  value={formData.eventTime}
                  onChange={(e) => handleChange('eventTime', e.target.value)}
                />
                {errors.eventTime && <span className="error-message">{errors.eventTime}</span>}
              </div>

              <div className="form-group">
                <label className="form-label required">End Time</label>
                <input
                  type="time"
                  className={`form-input ${errors.endTime ? 'error' : ''}`}
                  value={formData.endTime}
                  onChange={(e) => handleChange('endTime', e.target.value)}
                />
                {errors.endTime && <span className="error-message">{errors.endTime}</span>}
              </div>
            </div>

            <div className="form-group">
              <label className="form-label required">Location</label>
              <input
                type="text"
                className={`form-input ${errors.location ? 'error' : ''}`}
                value={formData.location}
                onChange={(e) => handleChange('location', e.target.value)}
                placeholder="e.g., The Club Birmingham, Downtown"
              />
              {errors.location && <span className="error-message">{errors.location}</span>}
            </div>
          </div>

          <div className="form-section">
            <h3>Speaker Information</h3>
            
            <div className="form-group">
              <label className="form-label required">Speaker Name</label>
              <input
                type="text"
                className={`form-input ${errors.speaker ? 'error' : ''}`}
                value={formData.speaker}
                onChange={(e) => handleChange('speaker', e.target.value)}
                placeholder="e.g., Ambassador Jane Smith"
              />
              {errors.speaker && <span className="error-message">{errors.speaker}</span>}
            </div>

            <div className="form-group">
              <label className="form-label">Speaker Title</label>
              <input
                type="text"
                className="form-input"
                value={formData.speakerTitle || ''}
                onChange={(e) => handleChange('speakerTitle', e.target.value)}
                placeholder="e.g., Former US Ambassador to NATO"
              />
            </div>

            <div className="form-group">
              <label className="form-label">Speaker Bio</label>
              <textarea
                className="form-textarea"
                value={formData.speakerBio || ''}
                onChange={(e) => handleChange('speakerBio', e.target.value)}
                rows={3}
                placeholder="Brief biography of the speaker..."
              />
            </div>
          </div>

          <div className="form-section">
            <h3>RSVP Settings</h3>
            
            <div className="form-row">
              <div className="form-group">
                <label className="form-label required">RSVP Deadline</label>
                <input
                  type="date"
                  className={`form-input ${errors.rsvpDeadline ? 'error' : ''}`}
                  value={formData.rsvpDeadline}
                  onChange={(e) => handleChange('rsvpDeadline', e.target.value)}
                />
                {errors.rsvpDeadline && <span className="error-message">{errors.rsvpDeadline}</span>}
              </div>

              <div className="form-group">
                <label className="form-label">Max Attendees</label>
                <input
                  type="number"
                  className="form-input"
                  value={formData.maxAttendees || ''}
                  onChange={(e) => handleChange('maxAttendees', e.target.value ? parseInt(e.target.value) : undefined)}
                  placeholder="Leave empty for unlimited"
                  min="1"
                />
              </div>
            </div>

            <div className="form-group">
              <label className="checkbox-label">
                <input
                  type="checkbox"
                  checked={formData.allowPlusOne}
                  onChange={(e) => handleChange('allowPlusOne', e.target.checked)}
                />
                <span>Allow attendees to bring a plus one</span>
              </label>
            </div>

            <div className="form-group">
              <label className="form-label">Event Status</label>
              <select
                className="form-select"
                value={formData.status}
                onChange={(e) => handleChange('status', e.target.value as Event['status'])}
              >
                <option value="draft">Draft</option>
                <option value="published">Published</option>
                <option value="cancelled">Cancelled</option>
              </select>
            </div>
          </div>

          <div className="drawer-footer">
            <button type="button" className="btn btn-secondary" onClick={onClose}>
              Cancel
            </button>
            <button type="submit" className="btn btn-primary">
              {isNew ? 'Create Event' : 'Save Changes'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

export default EventDetailsDrawer;