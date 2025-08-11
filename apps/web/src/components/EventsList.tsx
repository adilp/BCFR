import './EventsList.css'
import { MapPinIcon, ClockIcon, UserGroupIcon, VideoCameraIcon } from '@heroicons/react/24/outline'

interface Event {
  day: string
  month: string
  title: string
  description: string
  // Detailed info only for authenticated users
  time?: string
  location?: string
  speaker?: string
  zoomLink?: string
  registrationLink?: string
  attendeeLimit?: number
  currentAttendees?: number
}

interface EventsListProps {
  events?: Event[]
  showHeader?: boolean
  showDetails?: boolean
}

const defaultEvents: Event[] = [
  {
    day: '15',
    month: 'FEB',
    title: 'The Future of NATO: Challenges and Opportunities',
    description: 'Ambassador Jane Smith discusses the evolving role of NATO in global security',
    time: '12:00 PM - 1:30 PM',
    location: 'The Club Birmingham, Downtown',
    speaker: 'Ambassador Jane Smith, Former US Ambassador to NATO',
    zoomLink: 'https://zoom.us/j/123456789',
    registrationLink: '#',
    attendeeLimit: 150,
    currentAttendees: 87
  },
  {
    day: '22',
    month: 'FEB',
    title: 'Economic Diplomacy in the 21st Century',
    description: 'Panel discussion on trade relations and economic partnerships',
    time: '6:00 PM - 8:00 PM',
    location: 'Birmingham Museum of Art',
    speaker: 'Panel: Dr. Michael Chen, Prof. Sarah Williams, Amb. Robert Davis',
    zoomLink: 'https://zoom.us/j/987654321',
    registrationLink: '#',
    attendeeLimit: 200,
    currentAttendees: 145
  },
  {
    day: '08',
    month: 'MAR',
    title: 'Climate Change and International Cooperation',
    description: 'Exploring global solutions to environmental challenges',
    time: '12:00 PM - 1:30 PM',
    location: 'The Club Birmingham, Downtown',
    speaker: 'Dr. Emily Rodriguez, UN Climate Envoy',
    zoomLink: 'https://zoom.us/j/456789123',
    registrationLink: '#',
    attendeeLimit: 150,
    currentAttendees: 62
  }
]

const EventsList = ({ events = defaultEvents, showHeader = true, showDetails = false }: EventsListProps) => {
  return (
    <div className="events-container">
      {showHeader && (
        <div className="section-header">
          <h2 className="section-title">Upcoming Events</h2>
          <p className="section-subtitle">Join us for thought-provoking discussions on global affairs</p>
        </div>
      )}

      <div className="events-list">
        {events.map((event, index) => (
          <div key={index} className={`event-card ${showDetails ? 'detailed' : ''}`}>
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
                  
                  {event.zoomLink && (
                    <div className="detail-item">
                      <VideoCameraIcon className="detail-icon" />
                      <span className="detail-label">Virtual:</span>
                      <a href={event.zoomLink} target="_blank" rel="noopener noreferrer" className="zoom-link">
                        Join via Zoom
                      </a>
                    </div>
                  )}
                  
                  {event.attendeeLimit && (
                    <div className="detail-item">
                      <span className="detail-label">Capacity:</span>
                      <span className="capacity-info">
                        {event.currentAttendees}/{event.attendeeLimit} registered
                        <span className={`capacity-bar ${event.currentAttendees! / event.attendeeLimit > 0.8 ? 'almost-full' : ''}`}>
                          <span 
                            className="capacity-fill" 
                            style={{ width: `${(event.currentAttendees! / event.attendeeLimit) * 100}%` }}
                          />
                        </span>
                      </span>
                    </div>
                  )}
                  
                  {event.registrationLink && (
                    <div className="event-actions">
                      <a href={event.registrationLink} className="btn-register">Register for Event</a>
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