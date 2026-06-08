import { expect, test } from '@playwright/test'

import {
  apiBaseUrl,
  customerCredentials,
  findAvailableSlot,
  findCourtByName,
  formatTime,
  getAvailableSchedules,
  getCourts,
  loginViaApi,
  registerUniqueUser,
  seedAuthSession,
} from './helpers/api'

async function selectSlotOnGrid(
  page: Parameters<Parameters<typeof test>[1]>[0]['page'],
  slot: { date: string; startTime: string; endTime: string },
) {
  const label = `${slot.date} ${formatTime(slot.startTime)} - ${formatTime(slot.endTime)}`
  const slotButton = page.getByRole('button', { name: label })

  try {
    await slotButton.click({ timeout: 5_000 })
  } catch (error) {
    const continueButton = page.getByRole('button', { name: /Tiếp tục/ })

    if (await continueButton.isEnabled()) {
      return
    }

    throw error
  }
}

test.describe('user booking flows', () => {
  test('smoke: frontend can render and API exposes seeded courts', async ({
    page,
    request,
  }) => {
    await page.goto('/')
    await expect(page.locator('article', { hasText: 'Sân 5A' })).toBeVisible()
    await expect(page.locator('article', { hasText: 'Sân 5B' })).toBeVisible()

    const courts = await getCourts(request)

    expect(courts.map((court) => court.name)).toEqual(
      expect.arrayContaining(['Sân 5A', 'Sân 5B']),
    )
  })

  test('user can log in and filter the court list', async ({ page }) => {
    await page.goto('/login')
    await page.locator('input[name="email"]').fill(customerCredentials.email)
    await page.locator('input[name="password"]').fill(customerCredentials.password)
    await page.locator('button[type="submit"]').click()

    await expect(page).toHaveURL(/\/$/)
    await expect(page.locator('article', { hasText: 'Sân 5A' })).toBeVisible()
    await expect(page.locator('article', { hasText: 'Sân 5B' })).toBeVisible()

    await page.locator('form input').first().fill('Sân 5A')
    await page.locator('form button[type="submit"]').click()

    await expect(page.locator('article', { hasText: 'Sân 5A' })).toHaveCount(1)
    await expect(page.locator('article', { hasText: 'Sân 5B' })).toHaveCount(0)
  })

  test('user can select an available slot and open checkout modal', async ({
    page,
    request,
  }) => {
    const auth = await loginViaApi(
      request,
      customerCredentials.email,
      customerCredentials.password,
    )
    const court = await findCourtByName(request, 'Sân 5A')
    const slot = await findAvailableSlot(request, court.id, {
      minDayOffset: 0,
      maxDayOffset: 0,
    })

    await seedAuthSession(page, auth)
    await page.goto(`/courts/${court.id}/availability`)
    await selectSlotOnGrid(page, slot)

    await expect(page.getByText(`${slot.date} ·`, { exact: false })).toBeVisible()
    await page.getByRole('button', { name: /Tiếp tục/ }).click()

    await expect(page.getByRole('heading', { name: 'Đặt sân', exact: true })).toBeVisible()
    await expect(page.locator('input').nth(1)).toHaveValue(court.courtType)
    await expect(page.locator('input').nth(2)).toHaveValue(formatTime(slot.startTime))
    await expect(page.locator('input').nth(3)).toHaveValue(formatTime(slot.endTime))
  })

  test('user can checkout from availability modal and see booking history', async ({
    page,
    request,
  }) => {
    test.setTimeout(60_000)

    const user = await registerUniqueUser(request, 'pw-booking')
    const court = await findCourtByName(request, 'Sân 5A')
    const slot = await findAvailableSlot(request, court.id, {
      minDayOffset: 1,
      maxDayOffset: 1,
    })

    await seedAuthSession(page, user.auth)
    await page.goto(`/courts/${court.id}/availability`)
    await selectSlotOnGrid(page, slot)
    await page.getByRole('button', { name: /Tiếp tục/ }).click()

    await expect(page.getByRole('heading', { name: 'Đặt sân', exact: true })).toBeVisible()
    await page.locator('textarea').fill('Playwright checkout modal booking')

    const createBookingResponse = page.waitForResponse((response) => {
      return (
        response.url().startsWith(`${apiBaseUrl}/api/bookings`) &&
        response.request().method() === 'POST'
      )
    })

    await page.getByRole('button', { name: /Xác nhận đặt sân/ }).click()

    const response = await createBookingResponse
    expect(response.status()).toBe(201)

    await expect(page.getByRole('heading', { name: 'Đặt sân thành công' })).toBeVisible()

    const schedulesAfterBooking = await getAvailableSchedules(request, court.id, slot.date)
    const stillAvailable = schedulesAfterBooking.some((schedule) => {
      return schedule.startTime <= slot.startTime && schedule.endTime >= slot.endTime
    })

    expect(stillAvailable).toBeFalsy()

    await page.goto('/bookings')
    await expect(page.locator('article', { hasText: court.name }).first()).toBeVisible()
    await expect(page.getByText(`${formatTime(slot.startTime)} - ${formatTime(slot.endTime)}`).first()).toBeVisible()
  })
})
