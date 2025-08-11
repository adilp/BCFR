import './LandingPage.css'
import { GlobeAltIcon, UserGroupIcon, AcademicCapIcon } from '@heroicons/react/24/solid'
import Navigation from './Navigation'
import EventsList from './EventsList'

const LandingPage = () => {

  return (
    <>
      <Navigation />

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
          <EventsList />
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
