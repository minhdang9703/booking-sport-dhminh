import { getAccessToken } from './authApi'
import { apiRequest, buildQueryString, deleteRequest, postJson, putJson } from './apiClient'

export type CourtStatus = 1 | 2 | 3

export type Court = {
  id: string
  venueId: string
  venueName: string
  sportId: string
  sportName: string
  name: string
  status: CourtStatus
  description?: string | null
  createdAt: string
  updatedAt?: string | null
}

export type CourtQuery = {
  keyword?: string
  sportId?: string
  venueId?: string
  status?: string
}

export type CourtCreateRequest = {
  venueId: string
  sportId: string
  name: string
  status: CourtStatus
  description?: string
}

export type CourtUpdateRequest = {
  name: string
  status: CourtStatus
  description?: string
}

export type AvailableSchedule = {
  scheduleId: string
  courtId: string
  courtName: string
  date: string
  dayOfWeek: number
  startTime: string
  endTime: string
  price: number
  isAvailable: boolean
}

export function getCourts(query: CourtQuery = {}) {
  return apiRequest<Court[]>(`/api/courts${buildQueryString(query)}`)
}

export function getCourtById(courtId: string) {
  return apiRequest<Court>(`/api/courts/${courtId}`)
}

export function getAvailableSchedules(courtId: string, date: string) {
  return apiRequest<AvailableSchedule[]>(
    `/api/courts/${courtId}/available-schedules${buildQueryString({ date })}`,
  )
}

function getAuthHeaders() {
  const token = getAccessToken()

  return token ? { Authorization: `Bearer ${token}` } : undefined
}

export function createCourt(request: CourtCreateRequest) {
  return postJson<Court, CourtCreateRequest>('/api/courts', request, getAuthHeaders())
}

export function updateCourt(courtId: string, request: CourtUpdateRequest) {
  return putJson<Court, CourtUpdateRequest>(
    `/api/courts/${courtId}`,
    request,
    getAuthHeaders(),
  )
}

export function deleteCourt(courtId: string) {
  return deleteRequest(`/api/courts/${courtId}`, getAuthHeaders())
}
