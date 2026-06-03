export const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5000'

type ApiErrorResponse = {
  message?: string
}

export async function apiRequest<TResponse>(
  path: string,
  options?: RequestInit,
): Promise<TResponse> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
    ...options,
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

  return response.json() as Promise<TResponse>
}

export function postJson<TResponse, TBody>(
  path: string,
  body: TBody,
): Promise<TResponse> {
  return apiRequest<TResponse>(path, {
    method: 'POST',
    body: JSON.stringify(body),
  })
}
