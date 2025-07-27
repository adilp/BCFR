import { createRootRoute, createRouter, createRoute, Outlet } from '@tanstack/react-router'
import LandingPage from './components/LandingPage'
import MembershipPage from './components/MembershipPage'
import AboutPage from './components/AboutPage'

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

// Create the route tree
const routeTree = rootRoute.addChildren([indexRoute, membershipRoute, aboutRoute])

// Create the router
export const router = createRouter({ routeTree })