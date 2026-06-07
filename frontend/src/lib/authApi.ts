import { apiRequest, postJson } from './apiClient'
import {
  clearAuthSession,
  getAccessToken,
  getAuthSession,
  setAuthSession,
  subscribeAuthSession,
  type AuthResponse,
  type CurrentUser,
} from './authStore'

export type { AuthResponse, CurrentUser }
export { clearAuthSession, getAccessToken, getAuthSession, subscribeAuthSession }

export type LoginRequest = {
  email: string
  password: string
}

export type RegisterRequest = {
  fullName: string
  email: string
  password: string
  phoneNumber?: string
}

export function login(request: LoginRequest): Promise<AuthResponse> {
  return postJson<AuthResponse, LoginRequest>('/api/auth/login', request)
}

export function register(request: RegisterRequest): Promise<AuthResponse> {
  return postJson<AuthResponse, RegisterRequest>('/api/auth/register', request)
}

export function saveAuthSession(response: AuthResponse) {
  setAuthSession(response)
}

export async function refreshSession() {
  const response = await postJson<AuthResponse, Record<string, never>>(
    '/api/auth/refresh',
    {},
  )
  saveAuthSession(response)

  return response
}

export async function logout() {
  await apiRequest<void>('/api/auth/logout', { method: 'POST' })
  clearAuthSession()
}

export type AuthSettingsResponse = {
  accessTokenMinutes: number
  refreshTokenDays: number
  minAccessTokenMinutes: number
  maxAccessTokenMinutes: number
  minRefreshTokenDays: number
  maxRefreshTokenDays: number
}

export type UserSessionSettingsResponse = AuthSettingsResponse & {
  defaultAccessTokenMinutes: number
  defaultRefreshTokenDays: number
}

export type SessionSettingsUpdateRequest = {
  accessTokenMinutes: number
  refreshTokenDays: number
}

export function getUserSessionSettings() {
  return apiRequest<UserSessionSettingsResponse>('/api/auth/session-settings')
}

export function updateUserSessionSettings(request: SessionSettingsUpdateRequest) {
  return apiRequest<UserSessionSettingsResponse>('/api/auth/session-settings', {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export function getAdminAuthSettings() {
  return apiRequest<AuthSettingsResponse>('/api/admin/auth-settings')
}

export function updateAdminAuthSettings(request: SessionSettingsUpdateRequest) {
  return apiRequest<AuthSettingsResponse>('/api/admin/auth-settings', {
    method: 'PUT',
    body: JSON.stringify(request),
  })
}

export function isAdminUser(user?: CurrentUser | null) {
  return user?.role === 3 || user?.role === 'Admin'
}
