import { createRootRoute, createRouter, createRoute, Outlet } from '@tanstack/react-router'
import LandingPage from './components/LandingPage'
import MembershipPage from './components/MembershipPage'
import AboutPage from './components/AboutPage'
import LoginPage from './components/LoginPage'
import RegisterPage from './components/RegisterPage'

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

// Create register page route
const registerRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/register',
  component: RegisterPage
})

// Create the route tree
const routeTree = rootRoute.addChildren([indexRoute, membershipRoute, aboutRoute, loginRoute, registerRoute])

// Create the router
export const router = createRouter({ routeTree })