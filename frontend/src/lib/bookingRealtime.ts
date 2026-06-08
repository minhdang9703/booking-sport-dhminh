import * as signalR from '@microsoft/signalr'

import { getAccessToken } from './authApi'
import { apiBaseUrl } from './apiClient'
import type { BookingResponse, BookingStatus } from './bookingsApi'

export type BookingRealtimeEvent = {
  eventType: string
  bookingId: string
  userId: string
  courtId: string
  bookingDate: string
  startTime: string
  endTime: string
  status: BookingStatus
  booking?: BookingResponse | null
}

type CourtBookingHandlers = {
  onAvailabilityChanged: (event: BookingRealtimeEvent) => void
  onError?: (message: string) => void
}

type AdminBookingHandlers = {
  onBookingCreated: (event: BookingRealtimeEvent) => void
  onBookingStatusUpdated: (event: BookingRealtimeEvent) => void
  onError?: (message: string) => void
}

function createBookingConnection() {
  return new signalR.HubConnectionBuilder()
    .withUrl(`${apiBaseUrl}/hubs/bookings`, {
      accessTokenFactory: () => getAccessToken() ?? '',
      withCredentials: true,
    })
    .withAutomaticReconnect()
    .build()
}

export function subscribeToCourtBookings(
  courtId: string,
  handlers: CourtBookingHandlers,
) {
  const connection = createBookingConnection()
  let isDisposed = false

  connection.on('BookingAvailabilityChanged', handlers.onAvailabilityChanged)
  connection.onreconnected(() => {
    if (!isDisposed) {
      void connection.invoke('JoinCourt', courtId)
    }
  })

  void connection
    .start()
    .then(() => connection.invoke('JoinCourt', courtId))
    .catch((error: unknown) => {
      handlers.onError?.(
        error instanceof Error ? error.message : 'Khong the ket noi realtime booking.',
      )
    })

  return () => {
    isDisposed = true
    connection.off('BookingAvailabilityChanged', handlers.onAvailabilityChanged)
    void connection.stop()
  }
}

export function subscribeToAdminBookings(handlers: AdminBookingHandlers) {
  const connection = createBookingConnection()
  let isDisposed = false

  connection.on('BookingCreated', handlers.onBookingCreated)
  connection.on('BookingStatusUpdated', handlers.onBookingStatusUpdated)
  connection.onreconnected(() => {
    if (!isDisposed) {
      void connection.invoke('JoinAdminBookings')
    }
  })

  void connection
    .start()
    .then(() => connection.invoke('JoinAdminBookings'))
    .catch((error: unknown) => {
      handlers.onError?.(
        error instanceof Error ? error.message : 'Khong the ket noi realtime booking.',
      )
    })

  return () => {
    isDisposed = true
    connection.off('BookingCreated', handlers.onBookingCreated)
    connection.off('BookingStatusUpdated', handlers.onBookingStatusUpdated)
    void connection.stop()
  }
}
