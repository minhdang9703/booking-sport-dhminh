import { getAccessToken, refreshSession } from './authApi'
import { apiBaseUrl, apiRequest, buildQueryString, postJson, putJson } from './apiClient'

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

export async function exportBookingsReport(query: BookingQuery = {}) {
  const queryString = buildQueryString({
    fromDate: query.fromDate,
    toDate: query.toDate,
    courtId: query.courtId,
    status: query.status?.toString(),
  })
  let response = await fetchBookingReport(queryString)

  if (response.status === 401) {
    await refreshSession()
    response = await fetchBookingReport(queryString)
  }

  if (!response.ok) {
    throw new Error(`Export failed with status ${response.status}`)
  }

  const blob = await response.blob()
  const fileName = getFileName(response.headers.get('Content-Disposition'))
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(url)
}

function fetchBookingReport(queryString: string) {
  return fetch(`${apiBaseUrl}/api/admin/reports/bookings/export${queryString}`, {
    headers: getAuthHeaders(),
    credentials: 'include',
  })
}

function getFileName(contentDisposition: string | null) {
  const fallback = 'booking-report.xlsx'

  if (!contentDisposition) {
    return fallback
  }

  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(contentDisposition)
  if (utf8Match?.[1]) {
    return decodeURIComponent(utf8Match[1])
  }

  const asciiMatch = /filename="?([^";]+)"?/i.exec(contentDisposition)
  return asciiMatch?.[1] ?? fallback
}
