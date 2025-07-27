import { useState } from 'react'
import './Navigation.css'

const Navigation = () => {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)

  const toggleMobileMenu = () => {
    setIsMobileMenuOpen(!isMobileMenuOpen)
  }

  const closeMobileMenu = () => {
    setIsMobileMenuOpen(false)
  }

  return (
    <>
      <nav className="navbar">
        <div className="nav-container">
          <a href="/" className="logo" style={{ textDecoration: 'none' }}>BCFR</a>
          
          {/* Desktop Menu */}
          <div className="nav-menu desktop-menu">
            <a href="/about" className="nav-link">About</a>
            <a href="#events" className="nav-link">Events</a>
            <a href="/membership" className="nav-link">Membership</a>
            <a href="/login" className="login-btn">Member Login</a>
          </div>

          {/* Mobile Menu Button */}
          <button 
            className="mobile-menu-btn"
            onClick={toggleMobileMenu}
            aria-label="Toggle mobile menu"
          >
            <span className="menu-text">Menu</span>
            <span className="menu-icon">
              <span className={`hamburger ${isMobileMenuOpen ? 'open' : ''}`}></span>
            </span>
          </button>
        </div>

        {/* Mobile Dropdown Menu */}
        <div className={`mobile-menu ${isMobileMenuOpen ? 'open' : ''}`}>
          <a href="/" className="mobile-nav-link" onClick={closeMobileMenu}>Home</a>
          <a href="/about" className="mobile-nav-link" onClick={closeMobileMenu}>About</a>
          <a href="#events" className="mobile-nav-link" onClick={closeMobileMenu}>Events</a>
          <a href="/membership" className="mobile-nav-link" onClick={closeMobileMenu}>Membership</a>
          <a href="/login" className="mobile-login-btn" onClick={closeMobileMenu}>Member Login</a>
        </div>
      </nav>

      {/* Overlay for mobile menu */}
      {isMobileMenuOpen && (
        <div className="mobile-menu-overlay" onClick={closeMobileMenu}></div>
      )}
    </>
  )
}

export default Navigation