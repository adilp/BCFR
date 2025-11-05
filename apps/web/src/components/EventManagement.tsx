import { useState, useEffect } from 'react';
import EventDetailsDrawer from './EventDetailsDrawer';
import { getApiClient } from '@memberorg/api-client';
import type { Event, EventRsvp, CreateEventRequest } from '@memberorg/shared';
import { formatDateForDisplay, formatTimeTo12Hour } from '@memberorg/shared';
import {
  MagnifyingGlassIcon,
  ChevronRightIcon,
  ChevronDownIcon,
  PencilIcon,
  PlusIcon,
  ArrowDownTrayIcon,
  CalendarIcon,
  CheckCircleIcon,
  XCircleIcon,
  QuestionMarkCircleIcon,
  TrashIcon,
  EnvelopeIcon
} from '@heroicons/react/24/outline';
import './EventManagement.css';


function EventManagement() {
  const [events, setEvents] = useState<Event[]>([]);
  const [selectedEvent, setSelectedEvent] = useState<Event | null>(null);
  const [expandedRows, setExpandedRows] = useState<Set<string>>(new Set());
  const [eventRsvps, setEventRsvps] = useState<Record<string, EventRsvp[]>>({});
  const [searchTerm, setSearchTerm] = useState('');
  const [filterStatus, setFilterStatus] = useState('all');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [showNewEventDrawer, setShowNewEventDrawer] = useState(false);

  useEffect(() => {
    fetchEvents();
  }, []);

  const fetchEvents = async () => {
    try {
      setLoading(true);
      setError(null);
      const apiClient = getApiClient();
      const events = await apiClient.getEvents();
      setEvents(events);
    } catch (err: any) {
      console.error('Failed to fetch events:', err);
      setError(err.message || 'Failed to load events');
    } finally {
      setLoading(false);
    }
  };


  const fetchEventRsvps = async (eventId: string) => {
    try {
      const apiClient = getApiClient();
      const rsvps = await apiClient.getEventRsvps(eventId);
      setEventRsvps(prev => ({ ...prev, [eventId]: rsvps }));
    } catch (err: any) {
      console.error(`Failed to fetch RSVPs for event ${eventId}:`, err);
    }
  };

  const handleCheckInToggle = async (eventId: string, userId: string, currentCheckedIn: boolean) => {
    try {
      const apiClient = getApiClient();
      const newCheckedInStatus = !currentCheckedIn;

      // Call API to update check-in status
      await apiClient.checkInAttendee(eventId, userId, newCheckedInStatus);

      // Update local state
      setEventRsvps(prev => ({
        ...prev,
        [eventId]: prev[eventId].map(rsvp =>
          rsvp.userId === userId
            ? { ...rsvp, checkedIn: newCheckedInStatus, checkInTime: newCheckedInStatus ? new Date().toISOString() : undefined }
            : rsvp
        )
      }));
    } catch (err: any) {
      console.error(`Failed to update check-in for user ${userId}:`, err);
      alert(err.message || 'Failed to update check-in status');
    }
  };

  const toggleRowExpansion = async (eventId: string) => {
    const newExpanded = new Set(expandedRows);
    if (newExpanded.has(eventId)) {
      newExpanded.delete(eventId);
    } else {
      newExpanded.add(eventId);
      // Load RSVPs if not already loaded
      if (!eventRsvps[eventId]) {
        await fetchEventRsvps(eventId);
      }
    }
    setExpandedRows(newExpanded);
  };

  const handleSaveEvent = async (updatedEvent: Event) => {
    try {
      const apiClient = getApiClient();
      const eventData = {
        title: updatedEvent.title,
        description: updatedEvent.description,
        eventDate: updatedEvent.eventDate,
        eventTime: updatedEvent.eventTime,
        endTime: updatedEvent.endTime,
        location: updatedEvent.location,
        speaker: updatedEvent.speaker,
        speakerTitle: updatedEvent.speakerTitle,
        speakerBio: updatedEvent.speakerBio,
        rsvpDeadline: updatedEvent.rsvpDeadline,
        maxAttendees: updatedEvent.maxAttendees,
        allowPlusOne: updatedEvent.allowPlusOne,
        emailNote: updatedEvent.emailNote,
        status: updatedEvent.status
      };
      
      if (updatedEvent.id) {
        // Update existing event
        await apiClient.updateEvent(updatedEvent.id, eventData);
      } else {
        // Create new event
        await apiClient.createEvent(eventData as CreateEventRequest);
      }
      await fetchEvents(); // Refresh the list
      setSelectedEvent(null);
      setShowNewEventDrawer(false);
    } catch (err: any) {
      console.error('Failed to save event:', err);
      alert(err.message || 'Failed to save event');
    }
  };

  const handleDeleteEvent = async (eventId: string) => {
    if (confirm('Are you sure you want to delete this event?')) {
      try {
        const apiClient = getApiClient();
        await apiClient.deleteEvent(eventId);
        await fetchEvents(); // Refresh the list
      } catch (err: any) {
        console.error('Failed to delete event:', err);
        alert(err.message || 'Failed to delete event');
      }
    }
  };

  const handleExportRsvps = (eventId: string) => {
    const event = events.find(e => e.id === eventId);
    const rsvps = eventRsvps[eventId] || [];

    // Create CSV content
    const csvContent = [
      ['Name', 'Email', 'Response', 'Plus One', 'Response Date', 'Checked In'],
      ...rsvps.map(r => [
        r.userName,
        r.userEmail,
        r.response,
        r.hasPlusOne ? 'Yes' : 'No',
        r.responseDate,
        r.checkedIn ? 'Yes' : 'No'
      ])
    ].map(row => row.join(',')).join('\n');

    // Download CSV
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `${event?.title.replace(/\s+/g, '_')}_rsvps.csv`;
    a.click();
  };

  const handleEmailNonRsvpUsers = async (eventId: string) => {
    const event = events.find(e => e.id === eventId);
    if (!event) return;

    const confirmed = confirm(
      `This will send a reminder email about the "${event.title}" event to all active users who haven't RSVPed yet. Continue?`
    );

    if (!confirmed) return;

    try {
      const apiClient = getApiClient();
      await apiClient.sendEventReminderToNonRsvps(eventId);
      alert('Reminder emails have been sent successfully!');
    } catch (err: any) {
      console.error('Failed to send reminder emails:', err);
      alert(err.message || 'Failed to send reminder emails');
    }
  };

  const filteredEvents = events.filter(event => {
    const matchesSearch = searchTerm === '' || 
      event.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
      event.speaker.toLowerCase().includes(searchTerm.toLowerCase()) ||
      event.location.toLowerCase().includes(searchTerm.toLowerCase());
    
    const matchesStatus = filterStatus === 'all' || event.status === filterStatus;

    return matchesSearch && matchesStatus;
  });

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'published': return 'green';
      case 'draft': return 'yellow';
      case 'cancelled': return 'red';
      default: return 'gray';
    }
  };

  const formatEventDate = (date: string) => {
    return formatDateForDisplay(date, { format: 'short' });
  };

  return (
    <div className="event-management">
      <div className="management-header">
        <h2 className="management-title">Event Management</h2>
        <div className="management-actions">
          <button 
            className="btn btn-primary"
            onClick={() => setShowNewEventDrawer(true)}
          >
            <PlusIcon className="icon-xs" />
            Add Event
          </button>
          <button className="btn btn-secondary">
            <ArrowDownTrayIcon className="icon-xs" />
            Export All
          </button>
        </div>
      </div>

      <div className="management-filters">
        <div className="search-box">
          <MagnifyingGlassIcon className="search-icon" />
          <input
            type="text"
            className="form-input"
            placeholder="Search events..."
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
          />
        </div>
        
        <select
          className="form-select"
          value={filterStatus}
          onChange={(e) => setFilterStatus(e.target.value)}
        >
          <option value="all">All Status</option>
          <option value="published">Published</option>
          <option value="draft">Draft</option>
          <option value="cancelled">Cancelled</option>
        </select>
      </div>

      <div className="spreadsheet-container">
        {loading ? (
          <div style={{ textAlign: 'center', padding: '2rem' }}>
            <div>Loading events...</div>
          </div>
        ) : error ? (
          <div style={{ textAlign: 'center', padding: '2rem', color: '#DC3545' }}>
            <div>Error: {error}</div>
            <button 
              className="btn btn-primary" 
              style={{ marginTop: '1rem' }}
              onClick={fetchEvents}
            >
              Retry
            </button>
          </div>
        ) : filteredEvents.length === 0 ? (
          <div style={{ textAlign: 'center', padding: '2rem', color: '#6C757D' }}>
            No events found. Try adjusting your filters or create a new event.
          </div>
        ) : (
        <table className="spreadsheet-table">
          <thead>
            <tr>
              <th></th>
              <th>Event</th>
              <th>Date & Time</th>
              <th>Location</th>
              <th>Speaker</th>
              <th>RSVP Deadline</th>
              <th>RSVPs</th>
              <th>Status</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {filteredEvents.map((event) => (
            <>
              <tr key={event.id} className="event-row">
                <td>
                  <button 
                    className="expand-btn"
                    onClick={() => toggleRowExpansion(event.id)}
                  >
                    {expandedRows.has(event.id) ? 
                      <ChevronDownIcon className="icon-xs" /> : 
                      <ChevronRightIcon className="icon-xs" />
                    }
                  </button>
                </td>
                <td className="event-title-cell">
                  <div>
                    <div className="event-title">{event.title}</div>
                    <div className="event-description">{event.description}</div>
                  </div>
                </td>
                <td>
                  <div className="date-time-cell">
                    <CalendarIcon className="icon-xs" />
                    <div>
                      <div>{formatEventDate(event.eventDate)}</div>
                      <div className="time-text">{formatTimeTo12Hour(event.eventTime)} - {formatTimeTo12Hour(event.endTime)}</div>
                    </div>
                  </div>
                </td>
                <td>{event.location}</td>
                <td>{event.speaker}</td>
                <td>{formatEventDate(event.rsvpDeadline)}</td>
                <td>
                  {event.rsvpStats && (
                    <div className="rsvp-summary">
                      <span className="rsvp-count yes">
                        <CheckCircleIcon className="icon-xs" /> {event.rsvpStats.yes}
                      </span>
                      <span className="rsvp-count no">
                        <XCircleIcon className="icon-xs" /> {event.rsvpStats.no}
                      </span>
                      <span className="rsvp-count pending">
                        <QuestionMarkCircleIcon className="icon-xs" /> {event.rsvpStats.pending}
                      </span>
                    </div>
                  )}
                </td>
                <td>
                  <span className={`tag tag-${getStatusColor(event.status)}`}>
                    {event.status}
                  </span>
                </td>
                <td>
                  <div className="action-buttons">
                    <button 
                      className="btn btn-ghost btn-icon"
                      onClick={() => setSelectedEvent(event)}
                      title="Edit"
                    >
                      <PencilIcon className="icon-sm" />
                    </button>
                    <button 
                      className="btn btn-ghost btn-icon"
                      onClick={() => handleDeleteEvent(event.id)}
                      title="Delete"
                    >
                      <TrashIcon className="icon-sm" />
                    </button>
                  </div>
                </td>
              </tr>
              {expandedRows.has(event.id) && (
                <tr className="expanded-row">
                  <td colSpan={9}>
                    <div className="expanded-content">
                      <div className="rsvp-header">
                        <h4>RSVP List</h4>
                        <div style={{ display: 'flex', gap: '0.5rem' }}>
                          <button
                            className="btn btn-secondary btn-sm"
                            onClick={() => handleExportRsvps(event.id)}
                          >
                            <ArrowDownTrayIcon className="icon-xs" />
                            Export RSVPs
                          </button>
                          <button
                            className="btn btn-secondary btn-sm"
                            onClick={() => handleEmailNonRsvpUsers(event.id)}
                            title="Send reminder email to users who haven't RSVPed"
                          >
                            <EnvelopeIcon className="icon-xs" />
                            Remind Non-RSVPs
                          </button>
                        </div>
                      </div>
                      
                      <div className="rsvp-table-container">
                        <table className="rsvp-table">
                          <thead>
                            <tr>
                              <th>Name</th>
                              <th>Email</th>
                              <th>Response</th>
                              <th>Plus One</th>
                              <th>Response Date</th>
                              <th>Checked In</th>
                            </tr>
                          </thead>
                          <tbody>
                            {(eventRsvps[event.id] || []).map((rsvp) => (
                              <tr key={rsvp.id}>
                                <td>{rsvp.userName}</td>
                                <td>{rsvp.userEmail}</td>
                                <td>
                                  <span className={`rsvp-response ${rsvp.response}`}>
                                    {rsvp.response}
                                  </span>
                                </td>
                                <td>{rsvp.hasPlusOne ? 'Yes' : 'No'}</td>
                                <td>{formatEventDate(rsvp.responseDate)}</td>
                                <td>
                                  <input
                                    type="checkbox"
                                    checked={rsvp.checkedIn || false}
                                    onChange={() => handleCheckInToggle(event.id, rsvp.userId, rsvp.checkedIn || false)}
                                    disabled={rsvp.response !== 'yes'}
                                    title={rsvp.response !== 'yes' ? 'Only YES RSVPs can be checked in' : ''}
                                  />
                                </td>
                              </tr>
                            ))}
                          </tbody>
                        </table>
                      </div>
                      
                      <div className="event-stats">
                        <div className="stat-item">
                          <span className="stat-label">Total Attending:</span>
                          <span className="stat-value">
                            {event.rsvpStats?.yes || 0} + {event.rsvpStats?.plusOnes || 0} guests
                          </span>
                        </div>
                        <div className="stat-item">
                          <span className="stat-label">Response Rate:</span>
                          <span className="stat-value">
                            {Math.round(((event.rsvpStats?.yes || 0) + (event.rsvpStats?.no || 0)) / 
                              ((event.rsvpStats?.yes || 0) + (event.rsvpStats?.no || 0) + (event.rsvpStats?.pending || 0)) * 100)}%
                          </span>
                        </div>
                      </div>
                    </div>
                  </td>
                </tr>
              )}
            </>
            ))}
          </tbody>
        </table>
        )}
      </div>

      {(selectedEvent || showNewEventDrawer) && (
        <EventDetailsDrawer
            event={selectedEvent || {
              id: '',
              title: '',
              description: '',
              eventDate: '',
              eventTime: '',
              endTime: '',
              location: '',
              speaker: '',
              speakerTitle: '',
              speakerBio: '',
              rsvpDeadline: '',
              maxAttendees: 150,
              allowPlusOne: true,
              status: 'draft' as 'draft'
            } as Event}
            isNew={!selectedEvent}
            onClose={() => {
              setSelectedEvent(null);
              setShowNewEventDrawer(false);
            }}
            onSave={handleSaveEvent}
          />
      )}
    </div>
  );
}

export default EventManagement;
