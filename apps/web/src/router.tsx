import { createRootRoute, createRouter, createRoute, Outlet } from '@tanstack/react-router'
import LandingPage from './components/LandingPage'
import MembershipPage from './components/MembershipPage'
import AboutPage from './components/AboutPage'
import LoginPage from './components/LoginPage'
import MembershipSuccess from './components/MembershipSuccess'
import ProfilePage from './components/ProfilePage'
import EventsPage from './components/EventsPage'

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

// Create profile page route
const profileRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/profile',
  component: ProfilePage
})

// Create events page route
const eventsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/events',
  component: EventsPage
})

// Create the route tree
const routeTree = rootRoute.addChildren([indexRoute, membershipRoute, membershipSuccessRoute, aboutRoute, loginRoute, profileRoute, eventsRoute])

// Create the router
export const router = createRouter({ routeTree })