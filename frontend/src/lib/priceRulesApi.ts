import { getAccessToken } from './authApi'
import { apiRequest, buildQueryString, deleteRequest, postJson, putJson } from './apiClient'

export type PriceRule = {
  id: string
  name: string
  dayOfWeek: number
  startTime: string
  endTime: string
  hourlyPrice: number
  isEnabled: boolean
  createdAt: string
  updatedAt?: string | null
}

export type PriceRuleQuery = {
  dayOfWeek?: number
  isEnabled?: boolean
}

export type PriceRuleCreateRequest = {
  name: string
  dayOfWeek: number
  startTime: string
  endTime: string
  hourlyPrice: number
  isEnabled: boolean
}

export type PriceRuleUpdateRequest = PriceRuleCreateRequest

function getAuthHeaders() {
  const token = getAccessToken()

  return token ? { Authorization: `Bearer ${token}` } : undefined
}

export function getPriceRules(query: PriceRuleQuery = {}) {
  const queryString = buildQueryString({
    dayOfWeek: query.dayOfWeek?.toString(),
    isEnabled:
      typeof query.isEnabled === 'boolean' ? query.isEnabled.toString() : undefined,
  })

  return apiRequest<PriceRule[]>(`/api/price-rules${queryString}`)
}

export function createPriceRule(request: PriceRuleCreateRequest) {
  return postJson<PriceRule, PriceRuleCreateRequest>(
    '/api/price-rules',
    request,
    getAuthHeaders(),
  )
}

export function updatePriceRule(id: string, request: PriceRuleUpdateRequest) {
  return putJson<PriceRule, PriceRuleUpdateRequest>(
    `/api/price-rules/${id}`,
    request,
    getAuthHeaders(),
  )
}

export function deletePriceRule(id: string) {
  return deleteRequest(`/api/price-rules/${id}`, getAuthHeaders())
}
