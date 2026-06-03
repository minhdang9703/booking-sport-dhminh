export const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'

type ApiErrorResponse = {
  message?: string
}

export async function apiRequest<TResponse>(
  path: string,
  options?: RequestInit,
): Promise<TResponse> {
  const headers = new Headers(options?.headers)

  if (!headers.has('Content-Type') && options?.body) {
    headers.set('Content-Type', 'application/json')
  }

  const response = await fetch(`${apiBaseUrl}${path}`, {
    ...options,
    headers,
  })

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
