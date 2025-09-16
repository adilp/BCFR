import './EventsList.css'
import { MapPinIcon, ClockIcon, UserGroupIcon, CalendarIcon, CheckCircleIcon, XCircleIcon } from '@heroicons/react/24/outline'
import { useState, useEffect } from 'react'
import { getApiClient } from '@memberorg/api-client'
import type { Event } from '@memberorg/shared'
import { formatDateForDisplay, formatTimeTo12Hour } from '@memberorg/shared'
import { useAuth } from '../contexts/AuthContext'

interface EventDisplay extends Event {
  // For display
  day?: string
  month?: string
  time?: string
  userRsvpStatus?: 'yes' | 'no' | 'pending' | null
  hasPlusOne?: boolean
}

interface EventsListProps {
  events?: EventDisplay[]
  showHeader?: boolean
  showDetails?: boolean
  isPast?: boolean
}

const defaultEvents: EventDisplay[] = [
  {
    id: '1',
    day: '15',
    month: 'FEB',
    title: 'The Future of NATO: Challenges and Opportunities',
    description: 'Ambassador Jane Smith discusses the evolving role of NATO in global security',
    eventDate: '2025-02-15',
    eventTime: '12:00',
    endTime: '13:30',
    time: '12:00 PM - 1:30 PM',
    location: 'The Club Birmingham, Downtown',
    speaker: 'Ambassador Jane Smith, Former US Ambassador to NATO',
    rsvpDeadline: '2025-02-12',
    allowPlusOne: true,
    status: 'published',
    userRsvpStatus: null,
    hasPlusOne: false,
    rsvpStats: {
      yes: 87,
      no: 12,
      pending: 23,
      plusOnes: 0
    }
  },
  {
    id: '2',
    day: '22',
    month: 'FEB',
    title: 'Economic Diplomacy in the 21st Century',
    description: 'Panel discussion on trade relations and economic partnerships',
    eventDate: '2025-02-22',
    eventTime: '18:00',
    endTime: '20:00',
    time: '6:00 PM - 8:00 PM',
    location: 'Birmingham Museum of Art',
    speaker: 'Panel: Dr. Michael Chen, Prof. Sarah Williams, Amb. Robert Davis',
    rsvpDeadline: '2025-02-19',
    allowPlusOne: true,
    status: 'published',
    userRsvpStatus: 'yes',
    hasPlusOne: true,
    rsvpStats: {
      yes: 145,
      no: 8,
      pending: 47,
      plusOnes: 0
    }
  },
  {
    id: '3',
    day: '08',
    month: 'MAR',
    title: 'Climate Change and International Cooperation',
    description: 'Exploring global solutions to environmental challenges',
    eventDate: '2025-03-08',
    eventTime: '12:00',
    endTime: '13:30',
    time: '12:00 PM - 1:30 PM',
    location: 'The Club Birmingham, Downtown',
    speaker: 'Dr. Emily Rodriguez, UN Climate Envoy',
    rsvpDeadline: '2025-03-05',
    allowPlusOne: true,
    status: 'published',
    userRsvpStatus: null,
    hasPlusOne: false,
    rsvpStats: {
      yes: 62,
      no: 5,
      pending: 33,
      plusOnes: 0
    }
  }
]

const EventsList = ({ events: propEvents, showHeader = true, showDetails = false, isPast = false }: EventsListProps) => {
  const { isAuthenticated } = useAuth()
  const [events, setEvents] = useState<EventDisplay[]>(propEvents || [])
  const [loading, setLoading] = useState(!propEvents)
  const [userRsvps, setUserRsvps] = useState<Record<string, { status: 'yes' | 'no' | 'pending', plusOne: boolean }>>({})

  // Helper function to format event for display
  const formatEventForDisplay = (event: Event): EventDisplay => {
    const eventDate = new Date(event.eventDate)
    const startTime = formatTimeTo12Hour(event.eventTime)
    const endTime = formatTimeTo12Hour(event.endTime)
    return {
      ...event,
      day: eventDate.getDate().toString(),
      month: eventDate.toLocaleDateString('en-US', { month: 'short' }).toUpperCase(),
      time: `${startTime} - ${endTime}`
    }
  }

  useEffect(() => {
    if (!propEvents) {
      fetchEvents()
    } else {
      setEvents(propEvents)
    }
  }, [propEvents])

  useEffect(() => {
    // Fetch user's RSVP status for all events when authenticated
    if (isAuthenticated && events.length > 0) {
      fetchUserRsvps()
    }
  }, [isAuthenticated, events])

  const fetchUserRsvps = async () => {
    try {
      const apiClient = getApiClient()
      const rsvpPromises = events.map(async (event) => {
        const rsvp = await apiClient.getMyRsvp(event.id)
        return {
          eventId: event.id,
          rsvp
        }
      })

      const rsvpResults = await Promise.all(rsvpPromises)
      const rsvpMap: Record<string, { status: 'yes' | 'no' | 'pending', plusOne: boolean }> = {}
      
      rsvpResults.forEach(result => {
        if (result.rsvp) {
          rsvpMap[result.eventId] = {
            status: result.rsvp.response,
            plusOne: result.rsvp.hasPlusOne
          }
        }
      })
      
      setUserRsvps(rsvpMap)
    } catch (error) {
      console.error('Failed to fetch user RSVPs:', error)
    }
  }

  const fetchEvents = async () => {
    try {
      setLoading(true)
      const apiClient = getApiClient()
      
      if (isPast) {
        // Fetch past events
        const allEvents = await apiClient.getEvents()
        const now = new Date()
        const pastEvents = allEvents
          .filter((event: Event) => new Date(event.eventDate) < now)
          .sort((a: Event, b: Event) => new Date(b.eventDate).getTime() - new Date(a.eventDate).getTime())
          .slice(0, 2)
          .map((event: Event): EventDisplay => formatEventForDisplay(event))
        setEvents(pastEvents)
      } else {
        // Fetch upcoming events
        const fetchedEvents = await apiClient.getEvents({ status: 'published' })
        const formattedEvents: EventDisplay[] = fetchedEvents.map((event: Event) => formatEventForDisplay(event))
        setEvents(formattedEvents)
      }
    } catch (error) {
      console.error('Failed to fetch events:', error)
      if (!isPast) {
        setEvents(defaultEvents) // Fallback to default events only for upcoming
      } else {
        setEvents([])
      }
    } finally {
      setLoading(false)
    }
  }

  const handleRsvp = async (eventId: string, status: 'yes' | 'no') => {
    if (!isAuthenticated) {
      alert('Please login to RSVP')
      return
    }

    try {
      const apiClient = getApiClient()
      await apiClient.createRsvp(eventId, {
        response: status,
        hasPlusOne: userRsvps[eventId]?.plusOne || false
      })
      
      setUserRsvps(prev => ({
        ...prev,
        [eventId]: { status, plusOne: prev[eventId]?.plusOne || false }
      }))

      // Refresh event to get updated RSVP stats
      const updatedEvent = await apiClient.getEvent(eventId)
      setEvents(prev => prev.map(e => e.id === eventId ? formatEventForDisplay(updatedEvent) : e))
    } catch (error: any) {
      console.error('Failed to submit RSVP:', error)
      alert(error.message || 'Failed to submit RSVP')
    }
  }

  const handlePlusOne = async (eventId: string, hasPlusOne: boolean) => {
    if (!isAuthenticated) return

    try {
      const apiClient = getApiClient()
      const currentStatus = userRsvps[eventId]?.status || 'yes'
      // Only send yes or no to API (pending is not valid for submission)
      const responseStatus = currentStatus === 'pending' ? 'yes' : currentStatus
      await apiClient.createRsvp(eventId, {
        response: responseStatus as 'yes' | 'no',
        hasPlusOne
      })
      
      setUserRsvps(prev => ({
        ...prev,
        [eventId]: { ...prev[eventId], plusOne: hasPlusOne }
      }))
    } catch (error: any) {
      console.error('Failed to update plus one:', error)
      alert(error.message || 'Failed to update plus one')
    }
  }

  if (loading) {
    return (
      <div className="events-container">
        <div style={{ textAlign: 'center', padding: '2rem' }}>Loading events...</div>
      </div>
    )
  }

  return (
    <div className="events-container">
      {showHeader && (
        <div className="section-header">
          <h2 className="section-title">Upcoming Events</h2>
          <p className="section-subtitle">Join us for thought-provoking discussions on global affairs</p>
        </div>
      )}

      <div className="events-list">
        {events.map((event) => (
          <div key={event.id} className={`event-card ${showDetails ? 'detailed' : ''}`}>
            <div className="event-date">
              <div className="event-date-day">{event.day}</div>
              <div className="event-date-month">{event.month}</div>
            </div>
            <div className="event-details">
              <h3>{event.title}</h3>
              <p>{event.description}</p>
              
              {showDetails && (
                <div className="event-details-extended">
                  {event.speaker && (
                    <div className="detail-item">
                      <UserGroupIcon className="detail-icon" />
                      <span className="detail-label">Speaker:</span>
                      <span>{event.speaker}</span>
                    </div>
                  )}
                  
                  <div className="detail-item">
                    <CalendarIcon className="detail-icon" />
                    <span className="detail-label">Date:</span>
                    <span>{formatDateForDisplay(event.eventDate, { format: 'long' })}</span>
                  </div>

                  {event.time && (
                    <div className="detail-item">
                      <ClockIcon className="detail-icon" />
                      <span className="detail-label">Time:</span>
                      <span>{event.time}</span>
                    </div>
                  )}
                  
                  {event.location && (
                    <div className="detail-item">
                      <MapPinIcon className="detail-icon" />
                      <span className="detail-label">Location:</span>
                      <span>{event.location}</span>
                    </div>
                  )}
                  
                  {event.rsvpDeadline && (
                    <div className="detail-item">
                      <CalendarIcon className="detail-icon" />
                      <span className="detail-label">RSVP Deadline:</span>
                      <span>{formatDateForDisplay(event.rsvpDeadline, { format: 'long' })}</span>
                    </div>
                  )}
                  
                  {!isPast && (
                    <div className="event-actions">
                      <div className="rsvp-buttons">
                        <button 
                          className={`btn-rsvp ${(userRsvps[event.id]?.status || event.userRsvpStatus) === 'yes' ? 'active yes' : ''}`}
                          onClick={() => handleRsvp(event.id, 'yes')}
                        >
                          <CheckCircleIcon className="btn-icon" />
                          Yes
                        </button>
                        <button 
                          className={`btn-rsvp ${(userRsvps[event.id]?.status || event.userRsvpStatus) === 'no' ? 'active no' : ''}`}
                          onClick={() => handleRsvp(event.id, 'no')}
                        >
                          <XCircleIcon className="btn-icon" />
                          No
                        </button>
                      </div>
                      
                      {((userRsvps[event.id]?.status || event.userRsvpStatus) === 'yes' && event.allowPlusOne) && (
                        <div className="plus-one-option">
                          <label className="plus-one-label">
                            <input 
                              type="checkbox" 
                              checked={userRsvps[event.id]?.plusOne || event.hasPlusOne || false}
                              onChange={(e) => handlePlusOne(event.id, e.target.checked)}
                            />
                            <span>Bringing a plus one?</span>
                          </label>
                        </div>
                      )}
                    </div>
                  )}
                </div>
              )}
            </div>
          </div>
        ))}
      </div>
    </div>
  )
}

export default EventsList
