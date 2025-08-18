import { createRootRoute, createRouter, createRoute, Outlet } from '@tanstack/react-router'
import LandingPage from './components/LandingPage'
import MembershipPage from './components/MembershipPage'
import AboutPage from './components/AboutPage'
import LoginPage from './components/LoginPage'
import MembershipSuccess from './components/MembershipSuccess'
import ProfilePage from './components/ProfilePage'
import EventsPage from './components/EventsPage'
import AdminDashboard from './components/AdminDashboard'
import AdminProtectedRoute from './components/AdminProtectedRoute'
import { ProtectedRoute } from './components/ProtectedRoute'

// Create root route
const rootRoute = createRootRoute({
  component: () => <Outlet />
})

// Create landing page route
const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  component: LandingPage
})

// Create membership page route
const membershipRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/membership',
  component: MembershipPage
})

// Create about page route
const aboutRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/about',
  component: AboutPage
})

// Create login page route
const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/login',
  component: LoginPage
})

// Create membership success page route
const membershipSuccessRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/membership/success',
  component: MembershipSuccess
})

// Create profile page route (protected)
const profileRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/profile',
  component: () => (
    <ProtectedRoute>
      <ProfilePage />
    </ProtectedRoute>
  )
})

// Create events page route (public with conditional content)
const eventsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/events',
  component: EventsPage
})

// Create admin dashboard route
const adminRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/admin',
  component: () => (
    <AdminProtectedRoute>
      <AdminDashboard />
    </AdminProtectedRoute>
  )
})

// Create the route tree
const routeTree = rootRoute.addChildren([indexRoute, membershipRoute, membershipSuccessRoute, aboutRoute, loginRoute, profileRoute, eventsRoute, adminRoute])

// Create the router
export const router = createRouter({ routeTree })