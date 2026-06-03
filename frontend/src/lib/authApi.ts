import { postJson } from './apiClient'

export type CurrentUser = {
  id: string
  fullName: string
  email: string
  phoneNumber?: string | null
  role: number
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
