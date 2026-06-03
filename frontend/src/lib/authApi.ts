import { postJson } from './apiClient'

export type CurrentUser = {
  id: string
  fullName: string
  email: string
  phoneNumber?: string | null
  role: number | string
}

export type AuthResponse = {
  accessToken: string
  expiresAt: string
  user: CurrentUser
}

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

const authStorageKey = 'bookingSport.auth'

export function login(request: LoginRequest): Promise<AuthResponse> {
  return postJson<AuthResponse, LoginRequest>('/api/auth/login', request)
}

export function register(request: RegisterRequest): Promise<AuthResponse> {
  return postJson<AuthResponse, RegisterRequest>('/api/auth/register', request)
}

export function saveAuthSession(response: AuthResponse) {
  localStorage.setItem(authStorageKey, JSON.stringify(response))
}

export function getAuthSession(): AuthResponse | null {
  const rawSession = localStorage.getItem(authStorageKey)

  if (!rawSession) {
    return null
  }

  try {
    const session = JSON.parse(rawSession) as AuthResponse

    if (session.expiresAt && new Date(session.expiresAt).getTime() <= Date.now()) {
      clearAuthSession()
      return null
    }

    return session
  } catch {
    clearAuthSession()
    return null
  }
}

export function getAccessToken() {
  return getAuthSession()?.accessToken
}

export function clearAuthSession() {
  localStorage.removeItem(authStorageKey)
}

export function isAdminUser(user?: CurrentUser | null) {
  return user?.role === 3 || user?.role === 'Admin'
}
