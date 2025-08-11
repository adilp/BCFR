import './AboutPage.css'
import { GlobeAltIcon, UserGroupIcon, ChatBubbleLeftRightIcon } from '@heroicons/react/24/solid'
import Navigation from './Navigation'

const AboutPage = () => {
  return (
    <div className="about-page">
      <Navigation />

      {/* Mission Section */}
      <section className="mission-section">
        <div className="mission-container">
          <h1 className="mission-title">
            BCFR is on a mission to connect
            <br />Birmingham to the world through
            <br />informed dialogue and education
          </h1>
        </div>
      </section>

      {/* Values Section */}
      <section className="values-section">
        <div className="values-container">
          <div className="values-content">
            <div className="value-block">
              <p className="value-text">
                We believe that Birmingham's citizens
                want to understand more.
              </p>
            </div>

            <div className="value-block">
              <p className="value-text">
                They want to engage with global issues
                through expert speakers and thoughtful
                discussions.
              </p>
            </div>

            <div className="value-block">
              <p className="value-text">
                They want to share their perspectives
                with fellow members, business leaders,
                and the community. They want to go
                from local to global impact. And they
                want a forum for civil discourse.
              </p>
            </div>
          </div>

          <div className="image-collage">
            <div className="image-card card-1">
              <div className="placeholder-image">
                <GlobeAltIcon className="hero-icon" />
              </div>
            </div>
            <div className="image-card card-2">
              <div className="placeholder-image">
                <UserGroupIcon className="hero-icon" />
              </div>
            </div>
            <div className="image-card card-3">
              <div className="placeholder-image">
                <ChatBubbleLeftRightIcon className="hero-icon" />
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* History Section */}
      <section className="history-section">
        <div className="history-container">
          <h2 className="section-title">Our History</h2>
          <div className="history-content">
            <div className="history-text">
              <p>
                Founded in 1943, the Birmingham Committee on Foreign Relations has been
                Alabama's premier forum for international affairs discussion for over 80 years.
              </p>
              <p>
                What began as a small group of civic leaders seeking to understand America's
                role in World War II has grown into a vital institution connecting Birmingham
                to global conversations.
              </p>
              <p>
                Through the decades, we've hosted ambassadors, foreign policy experts,
                journalists, and thought leaders who have helped our members understand
                the complex forces shaping our interconnected world.
              </p>
            </div>
            <div className="timeline-visual">
              <div className="timeline-item">
                <span className="year">1943</span>
                <span className="milestone">Founded during WWII</span>
              </div>
              <div className="timeline-item">
                <span className="year">1960s</span>
                <span className="milestone">Civil Rights era dialogues</span>
              </div>
              <div className="timeline-item">
                <span className="year">1990s</span>
                <span className="milestone">Post-Cold War expansion</span>
              </div>
              <div className="timeline-item">
                <span className="year">Today</span>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Impact Section */}
      <section className="impact-section">
        <div className="impact-container">
          <h2 className="section-title">Our Impact</h2>
          <div className="impact-grid">
            <div className="impact-card">
              <div className="impact-number">100+</div>
              <div className="impact-label">Expert Speakers Presented</div>
            </div>
            <div className="impact-card">
              <div className="impact-number">80+</div>
              <div className="impact-label">Years of Service</div>
            </div>
            <div className="impact-card">
              <div className="impact-number">12</div>
              <div className="impact-label">Annual Programs</div>
            </div>
          </div>
        </div>
      </section>

      {/* CTA Section */}
      <section className="about-cta-section">
        <div className="cta-container">
          <h2>Join Our Global Community</h2>
          <p>Be part of Birmingham's window to the world</p>
          <a href="/membership" className="cta-button">Become a Member</a>
        </div>
      </section>
    </div>
  )
}

export default AboutPage
