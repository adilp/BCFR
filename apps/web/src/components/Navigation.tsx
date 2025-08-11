import { useState } from 'react'
import { useNavigate } from '@tanstack/react-router'
import './Navigation.css'
import { useAuth } from '../contexts/AuthContext'
import authService from '../services/auth'

const Navigation = () => {
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false)
  const { user, isAuthenticated, logout } = useAuth()
  const navigate = useNavigate()
  const isAdmin = authService.isAdmin()

  const toggleMobileMenu = () => {
    setIsMobileMenuOpen(!isMobileMenuOpen)
  }

  const closeMobileMenu = () => {
    setIsMobileMenuOpen(false)
  }

  const handleLogout = async () => {
    try {
      await logout()
      navigate({ to: '/login' })
    } catch (error) {
      console.error('Logout failed:', error)
    }
  }

  return (
    <>
      <nav className="navbar">
        <div className="nav-container">
          <a href="/" className="logo" style={{ textDecoration: 'none' }}>BCFR</a>
          
          {/* Desktop Menu */}
          <div className="nav-menu desktop-menu">
            <a href="/about" className="nav-link">About</a>
            <a href="/events" className="nav-link">Events</a>
            <a href="/membership" className="nav-link">Membership</a>
            {isAdmin && (
              <a href="/admin" className="nav-link">Admin</a>
            )}
            {isAuthenticated ? (
              <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
                <span style={{ color: '#333' }}>Hi, {user?.firstName}!</span>
                <a href="/profile" className="nav-link">My Profile</a>
                <button onClick={handleLogout} className="login-btn">Logout</button>
              </div>
            ) : (
              <a href="/login" className="login-btn">Member Login</a>
            )}
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
          <a href="/events" className="mobile-nav-link" onClick={closeMobileMenu}>Events</a>
          <a href="/membership" className="mobile-nav-link" onClick={closeMobileMenu}>Membership</a>
          {isAdmin && (
            <a href="/admin" className="mobile-nav-link" onClick={closeMobileMenu}>Admin</a>
          )}
          {isAuthenticated ? (
            <>
              <div style={{ padding: '0.5rem 1rem', color: '#666' }}>Hi, {user?.firstName}!</div>
              <a href="/profile" className="mobile-nav-link" onClick={closeMobileMenu}>My Profile</a>
              <button onClick={() => { handleLogout(); closeMobileMenu(); }} className="mobile-login-btn">Logout</button>
            </>
          ) : (
            <a href="/login" className="mobile-login-btn" onClick={closeMobileMenu}>Member Login</a>
          )}
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