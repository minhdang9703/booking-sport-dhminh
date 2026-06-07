import {
  clearAuthSession,
  getAccessToken,
  setAuthSession,
  type AuthResponse,
} from './authStore'

export const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'

type ApiErrorResponse = {
  message?: string
}

export async function apiRequest<TResponse>(
  path: string,
  options?: RequestInit,
  hasRetried = false,
): Promise<TResponse> {
  const headers = new Headers(options?.headers)
  const accessToken = getAccessToken()

  if (!headers.has('Content-Type') && options?.body) {
    headers.set('Content-Type', 'application/json')
  }

  if (accessToken && !headers.has('Authorization')) {
    headers.set('Authorization', `Bearer ${accessToken}`)
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers,
    credentials: 'include',
  })

  if (response.status === 401 && !hasRetried && shouldTryRefresh(path)) {
    const refreshed = await tryRefreshSession()

    if (refreshed) {
      return apiRequest<TResponse>(path, options, true)
    }
  }

  if (!response.ok) {
    let errorMessage = `API request failed with status ${response.status}`

    try {
      const error = (await response.json()) as ApiErrorResponse
      errorMessage = error.message ?? errorMessage
    } catch {
      // Keep the HTTP status fallback when the API does not return JSON.
    }

    throw new Error(errorMessage)
  }

  if (response.status === 204) {
    return undefined as TResponse
  }

  return response.json() as Promise<TResponse>
}

function shouldTryRefresh(path: string) {
  return ![
    '/api/auth/login',
    '/api/auth/register',
    '/api/auth/refresh',
    '/api/auth/logout',
  ].includes(path)
}

async function tryRefreshSession() {
  const response = await fetch(`${apiBaseUrl}/api/auth/refresh`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify({}),
    credentials: 'include',
  })

  if (!response.ok) {
    clearAuthSession()
    return false
  }

  const session = (await response.json()) as AuthResponse
  setAuthSession(session)

  return true
}

export function postJson<TResponse, TBody>(
  path: string,
  body: TBody,
  headers?: HeadersInit,
): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: 'POST',
    headers,
    body: JSON.stringify(body),
  })
}

export function putJson<TResponse, TBody>(
  path: string,
  body: TBody,
  headers?: HeadersInit,
): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: 'PUT',
    headers,
    body: JSON.stringify(body),
  })
}

export function deleteRequest(path: string, headers?: HeadersInit): Promise<void> {
  return apiRequest<void>(path, {
    method: 'DELETE',
    headers,
  })
}

export function buildQueryString(params: Record<string, string | undefined>) {
  const searchParams = new URLSearchParams()

  Object.entries(params).forEach(([key, value]) => {
    if (value) {
      searchParams.set(key, value)
    }
  })

  const queryString = searchParams.toString()

  return queryString ? `?${queryString}` : ''
}
