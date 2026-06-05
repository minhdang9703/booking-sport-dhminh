import { getAccessToken } from './authApi'
import { apiRequest, buildQueryString, postJson, putJson } from './apiClient'

export type BookingStatus = 1 | 2 | 3 | 4
export type PaymentType = 1 | 2 | 3

export type BookingCreateRequest = {
  courtId: string
  bookingDate: string
  startTime: string
  endTime: string
  paymentType: PaymentType
  note?: string
}

export type BookingResponse = {
  id: string
  userId: string
  userName: string
  courtId: string
  courtName: string
  bookingDate: string
  startTime: string
  endTime: string
  hourlyPriceSnapshot: number
  status: BookingStatus
  totalPrice: number
  paymentType: PaymentType
  note?: string | null
  createdAt: string
  updatedAt?: string | null
}

export type BookingQuery = {
  fromDate?: string
  toDate?: string
  courtId?: string
  status?: BookingStatus
}

export type BookingUpdateStatusRequest = {
  status: BookingStatus
}

function getAuthHeaders() {
  const token = getAccessToken()

  return token ? { Authorization: `Bearer ${token}` } : undefined
}

export function createBooking(request: BookingCreateRequest) {
  return postJson<BookingResponse, BookingCreateRequest>(
    '/api/bookings',
    request,
    getAuthHeaders(),
  )
}

export function getMyBookings() {
  return apiRequest<BookingResponse[]>('/api/bookings/my', {
    headers: getAuthHeaders(),
  })
}

export function getBookings(query: BookingQuery = {}) {
  const queryString = buildQueryString({
    fromDate: query.fromDate,
    toDate: query.toDate,
    courtId: query.courtId,
    status: query.status?.toString(),
  })

  return apiRequest<BookingResponse[]>(`/api/bookings${queryString}`, {
    headers: getAuthHeaders(),
  })
}

export function updateBookingStatus(
  id: string,
  request: BookingUpdateStatusRequest,
) {
  return putJson<BookingResponse, BookingUpdateStatusRequest>(
    `/api/bookings/${id}/status`,
    request,
    getAuthHeaders(),
  )
}
