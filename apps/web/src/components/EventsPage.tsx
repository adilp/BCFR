import './EventsPage.css'
import { useState, useEffect } from 'react'
import Navigation from './Navigation'
import EventsList from './EventsList'
import { CalendarIcon, ClockIcon, MapPinIcon, LockClosedIcon } from '@heroicons/react/24/outline'
import { useAuth } from '../contexts/AuthContext'
import { apiClient } from '../services/api'

const EventsPage = () => {
  const { isAuthenticated, user } = useAuth()
  const [hasActiveSubscription, setHasActiveSubscription] = useState(false)
  const [loading, setLoading] = useState(true)
  const pastEvents = [
    {
      day: '10',
      month: 'JAN',
      title: 'Middle East Peace Process: New Perspectives',
      description: 'Former Secretary of State discusses recent developments in regional diplomacy',
      time: '12:00 PM - 1:30 PM',
      location: 'The Club Birmingham, Downtown',
      speaker: 'Sec. John Kerry, Former US Secretary of State',
      zoomLink: 'https://zoom.us/j/archived123',
      attendeeLimit: 200,
      currentAttendees: 195
    },
    {
      day: '05',
      month: 'JAN',
      title: 'China-US Relations in the New Decade',
      description: 'Expert panel on economic and strategic competition',
      time: '6:00 PM - 8:00 PM',
      location: 'Birmingham Museum of Art',
      speaker: 'Panel: Dr. Susan Rice, Prof. Graham Allison, Amb. Jon Huntsman',
      zoomLink: 'https://zoom.us/j/archived456',
      attendeeLimit: 250,
      currentAttendees: 240
    }
  ]

  useEffect(() => {
    checkSubscriptionStatus()
  }, [isAuthenticated])

  const checkSubscriptionStatus = async () => {
    if (!isAuthenticated) {
      setHasActiveSubscription(false)
      setLoading(false)
      return
    }

    try {
      const response = await apiClient.get('/profile/subscription')
      const subscription = response.data
      setHasActiveSubscription(subscription && subscription.status === 'active')
    } catch (error) {
      console.error('Failed to fetch subscription:', error)
      setHasActiveSubscription(false)
    } finally {
      setLoading(false)
    }
  }

  const showDetailedView = isAuthenticated && hasActiveSubscription

  if (!isAuthenticated) {
    return (
      <>
        <Navigation />
        <div className="events-page">
          <div className="events-hero">
            <div className="container">
              <h1 className="events-title">Events & Programs</h1>
              <p className="events-subtitle">
                Sign in to view detailed event information and register for upcoming programs
              </p>
            </div>
          </div>

          <div className="container events-content">
            <div className="events-auth-prompt">
              <LockClosedIcon className="lock-icon" />
              <h2>Members-Only Content</h2>
              <p>Access detailed event information, speaker bios, zoom links, and registration by logging in with your member account.</p>
              <div className="auth-actions">
                <a href="/login" className="btn-primary">Member Login</a>
                <a href="/membership" className="btn-secondary">Become a Member</a>
              </div>
            </div>

            <section className="events-section">
              <h2 className="events-section-title">Upcoming Events Preview</h2>
              <EventsList showHeader={false} showDetails={false} />
            </section>
          </div>
        </div>
      </>
    )
  }

  if (loading) {
    return (
      <>
        <Navigation />
        <div className="events-page">
          <div className="container events-content">
            <div className="loading-message">Loading event details...</div>
          </div>
        </div>
      </>
    )
  }

  if (!hasActiveSubscription) {
    return (
      <>
        <Navigation />
        <div className="events-page">
          <div className="events-hero">
            <div className="container">
              <h1 className="events-title">Events & Programs</h1>
              <p className="events-subtitle">
                Your membership has expired or is inactive. Renew to access full event details.
              </p>
            </div>
          </div>

          <div className="container events-content">
            <div className="events-auth-prompt">
              <LockClosedIcon className="lock-icon" />
              <h2>Subscription Required</h2>
              <p>Hi {user?.firstName}! Your membership needs to be active to view detailed event information and register for programs.</p>
              <div className="auth-actions">
                <a href="/membership" className="btn-primary">Renew Membership</a>
                <a href="/profile" className="btn-secondary">View Profile</a>
              </div>
            </div>

            <section className="events-section">
              <h2 className="events-section-title">Upcoming Events Preview</h2>
              <EventsList showHeader={false} showDetails={false} />
            </section>
          </div>
        </div>
      </>
    )
  }

  return (
    <>
      <Navigation />
      
      <div className="events-page">
        <div className="events-hero">
          <div className="container">
            <h1 className="events-title">Events & Programs</h1>
            <p className="events-subtitle">
              Engage with world leaders, diplomats, and experts on the most pressing global issues of our time
            </p>
          </div>
        </div>

        <div className="container events-content">
          <div className="subscription-banner">
            <span className="subscription-badge">Active Member</span>
            <p>Welcome back, {user?.firstName}! You have full access to all event details and registration.</p>
          </div>

          <section className="events-section">
            <h2 className="events-section-title">Upcoming Events</h2>
            <EventsList showHeader={false} showDetails={showDetailedView} />
            
            <div className="events-info-cards">
              <div className="info-card">
                <CalendarIcon className="info-icon" />
                <h3>Monthly Speaker Series</h3>
                <p>Join us every third Thursday for our signature speaker series featuring global leaders and experts</p>
              </div>
              <div className="info-card">
                <ClockIcon className="info-icon" />
                <h3>Luncheon Programs</h3>
                <p>12:00 PM - 1:30 PM at The Club Birmingham. Lunch included with your membership</p>
              </div>
              <div className="info-card">
                <MapPinIcon className="info-icon" />
                <h3>Location & Parking</h3>
                <p>All events held at premier venues in Birmingham with convenient parking available</p>
              </div>
            </div>
          </section>

          <section className="events-section">
            <h2 className="events-section-title">Past Events</h2>
            <EventsList events={pastEvents} showHeader={false} showDetails={showDetailedView} />
          </section>
        </div>
      </div>
    </>
  )
}

export default EventsPage