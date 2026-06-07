import { type APIRequestContext, expect, type Page } from '@playwright/test'

export const apiBaseUrl = process.env.E2E_API_URL ?? 'http://localhost:5259'

export const adminCredentials = {
  email: 'admin@test.local',
  password: 'Test@123456',
}

export const customerCredentials = {
  email: 'customer@test.local',
  password: 'Test@123456',
}

export type AuthResponse = {
  accessToken: string
  expiresAt: string
  user: {
    id: string
    fullName: string
    email: string
    phoneNumber?: string | null
    role: number | string
  }
}

export type Court = {
  id: string
  name: string
  courtType: string
  status: number
}

export type AvailableSchedule = {
  priceRuleId: string
  courtId: string
  courtName: string
  date: string
  dayOfWeek: number
  startTime: string
  endTime: string
  hourlyPrice: number
  isAvailable: boolean
}

export type Slot = AvailableSchedule & {
  startTime: string
  endTime: string
}

export type BookingResponse = {
  id: string
  userId: string
  userName: string
  courtId: string
  courtName: string
  bookingDate: string
  startTime: string
  endTime: string
  hourlyPriceSnapshot: number
  status: number
  totalPrice: number
  paymentType: number
  note?: string | null
  createdAt: string
}

export type PriceRule = {
  id: string
  name: string
  dayOfWeek: number
  startTime: string
  endTime: string
  hourlyPrice: number
  isEnabled: boolean
}

export function toDateInputValue(date: Date) {
  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')

  return `${year}-${month}-${day}`
}

export function addDays(date: Date, days: number) {
  const nextDate = new Date(date)
  nextDate.setDate(date.getDate() + days)

  return nextDate
}

export function formatTime(value: string) {
  return value.slice(0, 5)
}

export function addMinutes(value: string, minutes: number) {
  const [hour, minute] = value.split(':').map(Number)
  const total = hour * 60 + minute + minutes
  const nextHour = Math.floor(total / 60) % 24
  const nextMinute = total % 60

  return `${nextHour.toString().padStart(2, '0')}:${nextMinute
    .toString()
    .padStart(2, '0')}:00`
}

function minutes(value: string) {
  const [hour, minute] = value.split(':').map(Number)

  return hour * 60 + minute
}

export async function loginViaApi(
  request: APIRequestContext,
  email: string,
  password: string,
) {
  const response = await request.post(`${apiBaseUrl}/api/auth/login`, {
    data: { email, password },
  })

  expect(response.ok()).toBeTruthy()

  return (await response.json()) as AuthResponse
}

export async function seedAuthSession(page: Page, auth: AuthResponse) {
  await page.addInitScript((session) => {
    ;(window as typeof window & { __bookingSportAuthSession?: unknown }).__bookingSportAuthSession = session
  }, auth)
  await page.goto('/')
}

export async function registerUniqueUser(
  request: APIRequestContext,
  prefix = 'pw-user',
) {
  const unique = `${Date.now()}-${Math.floor(Math.random() * 100000)}`
  const password = 'Pass@123456'
  const email = `${prefix}-${unique}@example.com`
  const phoneNumber = `09${unique.replace(/\D/g, '').slice(-8).padStart(8, '0')}`
  const fullName = `PW User ${unique}`

  const response = await request.post(`${apiBaseUrl}/api/auth/register`, {
    data: {
      fullName,
      email,
      password,
      phoneNumber,
    },
  })

  expect(response.ok()).toBeTruthy()

  return {
    auth: (await response.json()) as AuthResponse,
    email,
    password,
    fullName,
    phoneNumber,
  }
}

export async function getCourts(request: APIRequestContext) {
  const response = await request.get(`${apiBaseUrl}/api/courts`)

  expect(response.ok()).toBeTruthy()

  return (await response.json()) as Court[]
}

export async function findCourtByName(request: APIRequestContext, name: string) {
  const court = (await getCourts(request)).find((item) => item.name === name)

  expect(court, `Court ${name} should exist. Did seed 018 run?`).toBeTruthy()

  return court!
}

export async function getAvailableSchedules(
  request: APIRequestContext,
  courtId: string,
  date: string,
) {
  const response = await request.get(
    `${apiBaseUrl}/api/courts/${courtId}/available-schedules?date=${date}`,
  )

  expect(response.ok()).toBeTruthy()

  return (await response.json()) as AvailableSchedule[]
}

export async function findAvailableSlot(
  request: APIRequestContext,
  courtId: string,
  options: { minDayOffset?: number; maxDayOffset?: number } = {},
) {
  const minDayOffset = options.minDayOffset ?? 0
  const maxDayOffset = options.maxDayOffset ?? 21
  const today = new Date()
  today.setHours(0, 0, 0, 0)

  for (let dayOffset = minDayOffset; dayOffset <= maxDayOffset; dayOffset += 1) {
    const date = toDateInputValue(addDays(today, dayOffset))
    const schedules = await getAvailableSchedules(request, courtId, date)
    const schedule = schedules
      .slice()
      .reverse()
      .find((item) => minutes(item.endTime) - minutes(item.startTime) >= 30)

    if (schedule) {
      const endTotal = minutes(schedule.endTime)
      const startTotal = endTotal - 30
      const startHour = Math.floor(startTotal / 60) % 24
      const startMinute = startTotal % 60
      const startTime = `${startHour.toString().padStart(2, '0')}:${startMinute
        .toString()
        .padStart(2, '0')}:00`

      return {
        ...schedule,
        startTime,
        endTime: schedule.endTime,
      } satisfies Slot
    }
  }

  throw new Error('No available 30-minute slot found.')
}

export async function createBookingViaApi(
  request: APIRequestContext,
  accessToken: string,
  slot: Slot,
  note = 'Created by Playwright',
) {
  const response = await request.post(`${apiBaseUrl}/api/bookings`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
    data: {
      courtId: slot.courtId,
      bookingDate: slot.date,
      startTime: slot.startTime,
      endTime: slot.endTime,
      paymentType: 1,
      note,
    },
  })

  expect(response.ok()).toBeTruthy()

  return (await response.json()) as BookingResponse
}

export async function updateBookingStatusViaApi(
  request: APIRequestContext,
  accessToken: string,
  bookingId: string,
  status: number,
) {
  const response = await request.put(`${apiBaseUrl}/api/bookings/${bookingId}/status`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
    data: { status },
  })

  expect(response.ok()).toBeTruthy()

  return (await response.json()) as BookingResponse
}

export async function createPriceRuleViaApi(
  request: APIRequestContext,
  accessToken: string,
  payload: {
    name: string
    dayOfWeek: number
    startTime: string
    endTime: string
    hourlyPrice: number
    isEnabled: boolean
  },
) {
  const response = await request.post(`${apiBaseUrl}/api/price-rules`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
    data: payload,
  })

  expect(response.ok()).toBeTruthy()

  return (await response.json()) as PriceRule
}

export async function deletePriceRuleViaApi(
  request: APIRequestContext,
  accessToken: string,
  id: string,
) {
  const response = await request.delete(`${apiBaseUrl}/api/price-rules/${id}`, {
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  expect(response.ok()).toBeTruthy()
}
