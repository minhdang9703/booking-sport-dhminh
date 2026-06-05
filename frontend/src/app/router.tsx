import { createBrowserRouter } from 'react-router-dom'

import { App } from './App'
import { AdminBookingCalendarPage } from '../pages/AdminBookingCalendarPage'
import { AdminBookingsPage } from '../pages/AdminBookingsPage'
import { AdminCourtsPage } from '../pages/AdminCourtsPage'
import { AdminDashboardPage } from '../pages/AdminDashboardPage'
import { AdminPage } from '../pages/AdminPage'
import { AdminPriceRulesPage } from '../pages/AdminPriceRulesPage'
import { AdminRevenuePage } from '../pages/AdminRevenuePage'
import { AdminUsersPage } from '../pages/AdminUsersPage'
import { AvailabilityPage } from '../pages/AvailabilityPage'
import { BookingHistoryPage } from '../pages/BookingHistoryPage'
import { CheckoutPage } from '../pages/CheckoutPage'
import { CourtDetailPage } from '../pages/CourtDetailPage'
import { CourtsPage } from '../pages/CourtsPage'
import { LoginPage } from '../pages/LoginPage'
import { RegisterPage } from '../pages/RegisterPage'

export const router = createBrowserRouter([
  {
    path: '/',
    element: <App />,
    children: [
      { index: true, element: <CourtsPage /> },
      { path: 'courts', element: <CourtsPage /> },
      { path: 'courts/:courtId', element: <CourtDetailPage /> },
      { path: 'courts/:courtId/availability', element: <AvailabilityPage /> },
      { path: 'checkout', element: <CheckoutPage /> },
      { path: 'bookings', element: <BookingHistoryPage /> },
      { path: 'login', element: <LoginPage /> },
      { path: 'register', element: <RegisterPage /> },
      { path: 'admin', element: <AdminPage /> },
      { path: 'admin/dashboard', element: <AdminDashboardPage /> },
      { path: 'admin/bookings', element: <AdminBookingsPage /> },
      { path: 'admin/bookings/calendar', element: <AdminBookingCalendarPage /> },
      { path: 'admin/price-rules', element: <AdminPriceRulesPage /> },
      { path: 'admin/courts', element: <AdminCourtsPage /> },
      { path: 'admin/revenue', element: <AdminRevenuePage /> },
      { path: 'admin/users', element: <AdminUsersPage /> },
    ],
  },
])
