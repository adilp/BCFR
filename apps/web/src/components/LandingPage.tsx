import { useState } from 'react'
import './LandingPage.css'
import { GlobeAltIcon, UserGroupIcon, AcademicCapIcon } from '@heroicons/react/24/solid'

const LandingPage = () => {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)

  return (
    <>
      {/* Navigation */}
      <nav className="navbar">
        <div className="nav-container">
          <a href="/" className="logo" style={{ textDecoration: 'none' }}>BCFR</a>
          <div className="nav-menu">
            <a href="/about" className="nav-link">About</a>
            <a href="#events" className="nav-link">Events</a>
            <a href="/membership" className="nav-link">Membership</a>
            <button className="login-btn">Member Login</button>
          </div>
        </div>
      </nav>

      {/* Hero Section */}
      <section className="hero">
        {/* World Map Container */}
        <div className="world-map-container">
          <img src="/world-map.png" alt="World Map" />
        </div>

        <div className="hero-content">
          <h1 className="hero-title">Birmingham Committee on Foreign Relations</h1>
          <p className="hero-subtitle">Connecting Birmingham to the World Through Informed Dialogue since 1943</p>
          <div className="hero-cta">
            <a href="/membership" className="btn-primary">Become a Member</a>
            <a href="#events" className="btn-secondary">View Events</a>
          </div>
        </div>
      </section>

      {/* About Section */}
      <section id="about" className="section">
        <div className="container">
          <div className="section-header">
            <h2 className="section-title">Shaping Global Understanding</h2>
            <p className="section-subtitle">Join Birmingham's premier forum for international affairs discussion and education</p>
          </div>

          <div className="features-grid">
            <div className="feature-card">
              <div className="feature-icon">
                <GlobeAltIcon className="feature-icon-svg" />
              </div>
              <h3 className="feature-title">Global Perspectives</h3>
              <p className="feature-description">Engage with world-renowned speakers and experts on critical international issues</p>
            </div>
            <div className="feature-card">
              <div className="feature-icon">
                <UserGroupIcon className="feature-icon-svg" />
              </div>
              <h3 className="feature-title">Community Connection</h3>
              <p className="feature-description">Network with Birmingham's leaders in business, education, and civic affairs</p>
            </div>
            <div className="feature-card">
              <div className="feature-icon">
                <AcademicCapIcon className="feature-icon-svg" />
              </div>
              <h3 className="feature-title">Educational Programs</h3>
              <p className="feature-description">Access exclusive briefings, discussions, and educational resources</p>
            </div>
          </div>
        </div>
      </section>

      {/* Events Section */}
      <section id="events" className="section events-section">
        <div className="container">
          <div className="section-header">
            <h2 className="section-title">Upcoming Events</h2>
            <p className="section-subtitle">Join us for thought-provoking discussions on global affairs</p>
          </div>

          <div className="event-card">
            <div className="event-date">
              <div className="event-date-day">15</div>
              <div className="event-date-month">FEB</div>
            </div>
            <div className="event-details">
              <h3>The Future of NATO: Challenges and Opportunities</h3>
              <p>Ambassador Jane Smith discusses the evolving role of NATO in global security</p>
            </div>
          </div>

          <div className="event-card">
            <div className="event-date">
              <div className="event-date-day">22</div>
              <div className="event-date-month">FEB</div>
            </div>
            <div className="event-details">
              <h3>Economic Diplomacy in the 21st Century</h3>
              <p>Panel discussion on trade relations and economic partnerships</p>
            </div>
          </div>

          <div className="event-card">
            <div className="event-date">
              <div className="event-date-day">08</div>
              <div className="event-date-month">MAR</div>
            </div>
            <div className="event-details">
              <h3>Climate Change and International Cooperation</h3>
              <p>Exploring global solutions to environmental challenges</p>
            </div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section id="membership" className="section cta-section">
        <div className="container">
          <div className="section-header">
            <h2 className="section-title">Join Our Global Community</h2>
            <p className="section-subtitle">Become part of Birmingham's window to the world</p>
          </div>
          <a href="/membership" className="btn-primary cta-btn">Apply for Membership</a>
        </div>
      </section>

      {/* Footer */}
      <footer className="footer">
        <div className="container">
          <div className="footer-content">
            <div className="footer-section">
              <h4>About BCFR</h4>
              <a href="/about" className="footer-link">About</a>
              <a href="#" className="footer-link">Leadership</a>
              <a href="#" className="footer-link">Partners</a>
            </div>
            <div className="footer-section">
              <h4>Programs</h4>
              <a href="#" className="footer-link">Speaker Series</a>
            </div>
            <div className="footer-section">
              <h4>Get Involved</h4>
              <a href="/membership" className="footer-link">Membership</a>
              <a href="#" className="footer-link">Donate</a>
            </div>
            <div className="footer-section">
              <h4>Contact</h4>
              <p className="footer-contact">
                123 Main Street<br />
                Birmingham, AL 35203<br />
                info@bcfr.org<br />
                (205) 555-0123
              </p>
            </div>
          </div>
          <div className="footer-bottom">
            <p>&copy; 2025 Birmingham Committee on Foreign Relations. All rights reserved.</p>
          </div>
        </div>
      </footer>
    </>
  )
}

export default LandingPage
