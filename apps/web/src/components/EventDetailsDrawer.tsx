import { useState, useEffect } from 'react';
import { formatForDateInput, addDays } from '@memberorg/shared';
import Drawer from './shared/Drawer';
import { FormSection, FormGroup, FormGrid } from './shared/FormSection';
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

  const handleSubmit = () => {
    if (validateForm()) {
      onSave(formData);
    }
  };

  const drawerFooter = (
    <>
      <button type="button" className="btn btn-secondary" onClick={onClose}>
        Cancel
      </button>
      <button type="button" className="btn btn-primary" onClick={handleSubmit}>
        {isNew ? 'Create Event' : 'Save Changes'}
      </button>
    </>
  );

  return (
    <Drawer
      isOpen={true}
      onClose={onClose}
      title={isNew ? 'Create New Event' : 'Edit Event'}
      footer={drawerFooter}
      size="lg"
    >
      <form className="drawer-content">
        <FormSection title="Event Information">
          <FormGroup label="Event Title" required error={errors.title}>
            <input
              type="text"
              className="form-input"
              value={formData.title}
              onChange={(e) => handleChange('title', e.target.value)}
              placeholder="e.g., The Future of NATO"
            />
          </FormGroup>

          <FormGroup label="Description" required error={errors.description}>
            <textarea
              className="form-textarea"
              value={formData.description}
              onChange={(e) => handleChange('description', e.target.value)}
              rows={3}
              placeholder="Brief description of the event..."
            />
          </FormGroup>

          <FormGrid columns={3}>
            <FormGroup label="Event Date" required error={errors.eventDate}>
              <input
                type="date"
                className="form-input"
                value={formData.eventDate}
                onChange={(e) => handleChange('eventDate', e.target.value)}
              />
            </FormGroup>

            <FormGroup label="Start Time" required error={errors.eventTime}>
              <input
                type="time"
                className="form-input"
                value={formData.eventTime}
                onChange={(e) => handleChange('eventTime', e.target.value)}
              />
            </FormGroup>

            <FormGroup label="End Time" required error={errors.endTime}>
              <input
                type="time"
                className="form-input"
                value={formData.endTime}
                onChange={(e) => handleChange('endTime', e.target.value)}
              />
            </FormGroup>
          </FormGrid>

          <FormGroup label="Location" required error={errors.location}>
            <input
              type="text"
              className="form-input"
              value={formData.location}
              onChange={(e) => handleChange('location', e.target.value)}
              placeholder="e.g., The Club Birmingham, Downtown"
            />
          </FormGroup>
        </FormSection>

        <FormSection title="Speaker Information">
          <FormGroup label="Speaker Name" required error={errors.speaker}>
            <input
              type="text"
              className="form-input"
              value={formData.speaker}
              onChange={(e) => handleChange('speaker', e.target.value)}
              placeholder="e.g., Ambassador Jane Smith"
            />
          </FormGroup>

          <FormGroup label="Speaker Title">
            <input
              type="text"
              className="form-input"
              value={formData.speakerTitle || ''}
              onChange={(e) => handleChange('speakerTitle', e.target.value)}
              placeholder="e.g., Former US Ambassador to NATO"
            />
          </FormGroup>

          <FormGroup label="Speaker Bio">
            <textarea
              className="form-textarea"
              value={formData.speakerBio || ''}
              onChange={(e) => handleChange('speakerBio', e.target.value)}
              rows={3}
              placeholder="Brief biography of the speaker..."
            />
          </FormGroup>
        </FormSection>

        <FormSection title="RSVP Settings">
          <FormGrid columns={2}>
            <FormGroup label="RSVP Deadline" required error={errors.rsvpDeadline}>
              <input
                type="date"
                className="form-input"
                value={formData.rsvpDeadline}
                onChange={(e) => handleChange('rsvpDeadline', e.target.value)}
              />
            </FormGroup>

            <FormGroup label="Max Attendees">
              <input
                type="number"
                className="form-input"
                value={formData.maxAttendees || ''}
                onChange={(e) => handleChange('maxAttendees', e.target.value ? parseInt(e.target.value) : undefined)}
                placeholder="Leave empty for unlimited"
                min="1"
              />
            </FormGroup>
          </FormGrid>

          <FormGroup label="">
            <label className="checkbox-label">
              <input
                type="checkbox"
                checked={formData.allowPlusOne}
                onChange={(e) => handleChange('allowPlusOne', e.target.checked)}
              />
              <span>Allow attendees to bring a plus one</span>
            </label>
          </FormGroup>

          <FormGroup label="Event Status">
            <select
              className="form-select"
              value={formData.status}
              onChange={(e) => handleChange('status', e.target.value as Event['status'])}
            >
              <option value="draft">Draft</option>
              <option value="published">Published</option>
              <option value="cancelled">Cancelled</option>
            </select>
          </FormGroup>
        </FormSection>
      </form>
    </Drawer>
  );
}

export default EventDetailsDrawer;