import { apiRequest, buildQueryString } from './apiClient'
import { getAccessToken } from './authApi'

export type RevenuePeriod = 'Day' | 'Week' | 'Month'

export type RevenuePoint = {
  label: string
  fromDate: string
  toDate: string
  revenue: number
  completedBookingCount: number
}

export type RevenueDailyPoint = {
  date: string
  revenue: number
  completedBookingCount: number
}

export type RevenueDashboardResponse = {
  fromDate: string
  toDate: string
  totalRevenue: number
  completedBookingCount: number
  averageBookingValue: number
  period: number
  revenuePoints: RevenuePoint[]
  dailyRevenue: RevenueDailyPoint[]
}

export type RevenueDashboardQuery = {
  fromDate?: string
  toDate?: string
  period?: RevenuePeriod
}

function getAuthHeaders() {
  const token = getAccessToken()

  return token ? { Authorization: `Bearer ${token}` } : undefined
}

export function getRevenueDashboard(query: RevenueDashboardQuery) {
  const queryString = buildQueryString({
    fromDate: query.fromDate,
    toDate: query.toDate,
    period: query.period,
  })

  return apiRequest<RevenueDashboardResponse>(
    `/api/dashboard/revenue${queryString}`,
    {
      headers: getAuthHeaders(),
    },
  )
}
