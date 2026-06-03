import {
  apiRequest,
  buildQueryString,
  deleteRequest,
  postJson,
  putJson,
} from './apiClient'
import { getAccessToken } from './authApi'

export type UserRole = 1 | 2 | 3

export type UserResponse = {
  id: string
  fullName: string
  email: string
  phoneNumber?: string | null
  role: UserRole
  createdAt: string
  updatedAt?: string | null
}

export type UserCreateRequest = {
  fullName: string
  email: string
  password: string
  phoneNumber?: string
  role: UserRole
}

export type UserUpdateRequest = {
  fullName: string
  phoneNumber?: string
  role: UserRole
}

export type UserQuery = {
  keyword?: string
  role?: UserRole
  includeDeleted?: boolean
}

function getAuthHeaders() {
  const token = getAccessToken()

  return token ? { Authorization: `Bearer ${token}` } : undefined
}

export function getUsers(query: UserQuery = {}) {
  const queryString = buildQueryString({
    keyword: query.keyword,
    role: query.role?.toString(),
    includeDeleted: query.includeDeleted ? 'true' : undefined,
  })

  return apiRequest<UserResponse[]>(`/api/users${queryString}`, {
    headers: getAuthHeaders(),
  })
}

export function createUser(request: UserCreateRequest) {
  return postJson<UserResponse, UserCreateRequest>(
    '/api/users',
    request,
    getAuthHeaders(),
  )
}

export function updateUser(id: string, request: UserUpdateRequest) {
  return putJson<UserResponse, UserUpdateRequest>(
    `/api/users/${id}`,
    request,
    getAuthHeaders(),
  )
}

export function deleteUser(id: string) {
  return deleteRequest(`/api/users/${id}`, getAuthHeaders())
}
