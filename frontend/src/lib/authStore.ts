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

declare global {
  interface Window {
    __bookingSportAuthSession?: AuthResponse
  }
}

type AuthListener = (session: AuthResponse | null) => void

let authSession: AuthResponse | null =
  typeof window === 'undefined' ? null : window.__bookingSportAuthSession ?? null
const listeners = new Set<AuthListener>()

export function setAuthSession(session: AuthResponse) {
  authSession = session
  notifyListeners()
}

export function getAuthSession() {
  if (!authSession) {
    return null
  }

  if (authSession.expiresAt && new Date(authSession.expiresAt).getTime() <= Date.now()) {
    return {
      ...authSession,
      accessToken: '',
    }
  }

  return authSession
}

export function getAccessToken() {
  const session = getAuthSession()

  return session?.accessToken || undefined
}

export function clearAuthSession() {
  authSession = null
  notifyListeners()
}

export function subscribeAuthSession(listener: AuthListener) {
  listeners.add(listener)

  return () => {
    listeners.delete(listener)
  }
}

function notifyListeners() {
  listeners.forEach((listener) => listener(authSession))
}
