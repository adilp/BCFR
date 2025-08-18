import './EventsPage.css'
import { useState, useEffect } from 'react'
import Navigation from './Navigation'
import EventsList from './EventsList'
import { LockClosedIcon } from '@heroicons/react/24/outline'
import { useAuth } from '../contexts/AuthContext'
import { getApiClient } from '@memberorg/api-client'

const EventsPage = () => {
  const { isAuthenticated, user } = useAuth()
  const [hasActiveSubscription, setHasActiveSubscription] = useState(false)
  const [loading, setLoading] = useState(true)

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
      const apiClient = getApiClient()
      const subscription = await apiClient.getSubscription()
      console.log('Subscription data:', subscription)
      
      if (subscription) {
        console.log('Subscription status:', subscription.status)
        setHasActiveSubscription(subscription.status === 'active')
      } else {
        console.log('No subscription found')
        setHasActiveSubscription(false)
      }
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
            
          </section>

          <section className="events-section">
            <h2 className="events-section-title">Past Events</h2>
            <EventsList showHeader={false} showDetails={showDetailedView} isPast={true} />
          </section>
        </div>
      </div>
    </>
  )
}

export default EventsPage
