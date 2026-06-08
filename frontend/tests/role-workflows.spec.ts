import { expect, test } from '@playwright/test'

import {
  adminCredentials,
  addDays,
  createBookingViaApi,
  findAvailableSlot,
  findCourtByName,
  formatTime,
  loginViaApi,
  registerUniqueUser,
  seedAuthSession,
  updateBookingStatusViaApi,
} from './helpers/api'

test.describe.configure({ mode: 'serial' })

test.describe('admin workflows', () => {
  test('admin can create, update, and delete a court', async ({ page, request }) => {
    test.setTimeout(60_000)

    const adminAuth = await loginViaApi(
      request,
      adminCredentials.email,
      adminCredentials.password,
    )
    const uniqueName = `PW Sân 5 ${Date.now()}`
    const updatedName = `${uniqueName} Updated`

    await seedAuthSession(page, adminAuth)
    await page.goto('/admin/courts')
    await page.getByRole('button', { name: /\+.*sân/i }).click()

    const form = page.locator('form').last()
    await form.locator('input').nth(0).fill(uniqueName)
    await form.locator('input').nth(1).fill('Sân 5')
    await form.locator('select').selectOption('1')
    await form.locator('button[type="submit"]').click()

    const createdRow = page.locator('div', { hasText: uniqueName }).filter({ hasText: 'Sân 5' }).last()
    await expect(createdRow).toBeVisible()

    await createdRow.getByRole('button').filter({ hasText: /S/ }).first().click()
    const editForm = page.locator('form').last()
    await editForm.locator('input').nth(0).fill(updatedName)
    await editForm.locator('select').selectOption('3')
    await editForm.locator('button[type="submit"]').click()

    const updatedRow = page.locator('div', { hasText: updatedName }).filter({ hasText: 'Sân 5' }).last()
    await expect(updatedRow).toBeVisible()

    await updatedRow.getByRole('button').last().click()
    await page.locator('div.fixed').last().getByRole('button').last().click()

    await expect(page.locator('div', { hasText: updatedName })).toHaveCount(0)
  })

  test('admin can create, update, and delete a disabled price rule', async ({
    page,
    request,
  }) => {
    test.setTimeout(60_000)

    const adminAuth = await loginViaApi(
      request,
      adminCredentials.email,
      adminCredentials.password,
    )
    const uniqueName = `PW disabled rule ${Date.now()}`

    await seedAuthSession(page, adminAuth)
    await page.goto('/admin/price-rules')
    await page.getByRole('button', { name: /\+.*khung/i }).click()

    const form = page.locator('form').last()
    await form.locator('input').nth(0).fill(uniqueName)
    await form.locator('select').selectOption('1')
    await form.locator('input[type="time"]').nth(0).fill('05:00')
    await form.locator('input[type="time"]').nth(1).fill('06:00')
    await form.locator('input[type="number"]').fill('123000')
    await form.locator('input[type="checkbox"]').uncheck()
    await form.locator('button[type="submit"]').click()

    const createdRow = page.locator(
      `xpath=//p[normalize-space()="${uniqueName}"]/ancestor::div[contains(@class,"lg:grid-cols")][1]`,
    )
    await expect(createdRow).toBeVisible()

    await createdRow.getByRole('button').first().click()
    const editForm = page.locator('form').last()
    await editForm.locator('input').nth(0).fill(`${uniqueName} updated`)
    await editForm.locator('input[type="number"]').fill('124000')
    await editForm.locator('button[type="submit"]').click()

    const updatedRow = page.locator(
      `xpath=//p[normalize-space()="${uniqueName} updated"]/ancestor::div[contains(@class,"lg:grid-cols")][1]`,
    )
    await expect(updatedRow).toBeVisible()

    await updatedRow.getByRole('button').last().click()
    await page.locator('div.fixed').last().getByRole('button').last().click()

    await expect(page.locator('div', { hasText: `${uniqueName} updated` })).toHaveCount(0)
  })

  test('admin can update booking status from booking management', async ({
    page,
    request,
  }) => {
    test.setTimeout(60_000)

    const adminAuth = await loginViaApi(
      request,
      adminCredentials.email,
      adminCredentials.password,
    )
    const user = await registerUniqueUser(request, 'pw-admin-booking')
    const court = await findCourtByName(request, 'Sân 5A')
    const slot = await findAvailableSlot(request, court.id)
    const booking = await createBookingViaApi(
      request,
      user.auth.accessToken,
      slot,
      'Playwright admin status booking',
    )

    await seedAuthSession(page, adminAuth)
    await page.goto('/admin/bookings')
    await page.locator('input[type="date"]').nth(0).fill(slot.date)
    await page.locator('input[type="date"]').nth(1).fill(slot.date)
    await page.locator('input').last().fill(user.fullName)

    const row = page.locator('tbody tr', { hasText: user.fullName })
    await expect(row).toBeVisible()
    await row.locator('select').selectOption('2')
    await expect(row.locator('select')).toHaveValue('2')

    const updated = await updateBookingStatusViaApi(
      request,
      adminAuth.accessToken,
      booking.id,
      3,
    )

    expect(updated.status).toBe(3)
  })

  test('admin can view booking on calendar and change status', async ({
    page,
    request,
  }) => {
    test.setTimeout(60_000)

    const adminAuth = await loginViaApi(
      request,
      adminCredentials.email,
      adminCredentials.password,
    )
    const user = await registerUniqueUser(request, 'pw-calendar')
    const court = await findCourtByName(request, 'Sân 5B')
    const slot = await findAvailableSlot(request, court.id)
    await createBookingViaApi(
      request,
      user.auth.accessToken,
      slot,
      'Playwright calendar booking',
    )

    await seedAuthSession(page, adminAuth)
    await page.goto('/admin/bookings/calendar')

    const day = addDays(new Date(`${slot.date}T00:00:00`), 0).getDate().toString()
    await page.getByRole('button', { name: new RegExp(`^${day} .*${court.name}`) }).click()
    const sidebarCard = page.locator('aside article', { hasText: user.fullName })
    await expect(sidebarCard).toBeVisible()
    await sidebarCard.locator('select').selectOption('2')
    await expect(sidebarCard.locator('select')).toHaveValue('2')
  })

  test('admin can review completed booking in revenue dashboard', async ({
    page,
    request,
  }) => {
    test.setTimeout(60_000)

    const adminAuth = await loginViaApi(
      request,
      adminCredentials.email,
      adminCredentials.password,
    )
    const user = await registerUniqueUser(request, 'pw-revenue')
    const court = await findCourtByName(request, 'Sân 5A')
    const slot = await findAvailableSlot(request, court.id)
    const booking = await createBookingViaApi(
      request,
      user.auth.accessToken,
      slot,
      'Playwright revenue booking',
    )
    await updateBookingStatusViaApi(request, adminAuth.accessToken, booking.id, 4)

    await seedAuthSession(page, adminAuth)
    await page.goto('/admin/revenue')
    await page.locator('input[type="date"]').nth(0).fill(slot.date)
    await page.locator('input[type="date"]').nth(1).fill(slot.date)
    await page.locator('input').last().fill(user.fullName)

    await expect(page.locator('tbody tr', { hasText: user.fullName })).toBeVisible()
    await expect(page.locator('tbody tr', { hasText: `${formatTime(slot.startTime)} - ${formatTime(slot.endTime)}` })).toBeVisible()

    const periodSelect = page.locator('select').first()
    await periodSelect.selectOption('Week')
    await expect(periodSelect).toHaveValue('Week')
    await periodSelect.selectOption('Month')
    await expect(periodSelect).toHaveValue('Month')

  })
})
