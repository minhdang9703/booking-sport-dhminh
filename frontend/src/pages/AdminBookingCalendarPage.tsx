import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'

import {
  getBookings,
  updateBookingStatus,
  type BookingResponse,
  type BookingStatus,
} from '../lib/bookingsApi'

type CalendarDay = {
  date: Date
  dateKey: string
  isCurrentMonth: boolean
  isToday: boolean
}

const weekDays = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN']

const statusOptions: Array<{ value: BookingStatus | 'all'; label: string }> = [
  { value: 'all', label: 'Tất cả trạng thái' },
  { value: 1, label: 'Chờ xác nhận' },
  { value: 2, label: 'Đã xác nhận' },
  { value: 3, label: 'Đã hủy' },
  { value: 4, label: 'Hoàn tất' },
]

function toDateKey(date: Date) {
  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')

  return `${year}-${month}-${day}`
}

function parseDateKey(value: string) {
  const [year, month, day] = value.split('-').map(Number)

  return new Date(year, month - 1, day)
}

function addDays(date: Date, days: number) {
  const nextDate = new Date(date)
  nextDate.setDate(nextDate.getDate() + days)

  return nextDate
}

function startOfMonth(date: Date) {
  return new Date(date.getFullYear(), date.getMonth(), 1)
}

function endOfMonth(date: Date) {
  return new Date(date.getFullYear(), date.getMonth() + 1, 0)
}

function startOfWeek(date: Date) {
  const dayIndex = (date.getDay() + 6) % 7
  return addDays(date, -dayIndex)
}

function endOfWeek(date: Date) {
  const dayIndex = (date.getDay() + 6) % 7
  return addDays(date, 6 - dayIndex)
}

function formatMonthTitle(date: Date) {
  return new Intl.DateTimeFormat('vi-VN', {
    month: 'long',
    year: 'numeric',
  }).format(date)
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('vi-VN', {
    weekday: 'long',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(parseDateKey(value))
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

function getStatusMeta(status: BookingStatus) {
  if (status === 1) {
    return {
      label: 'Chờ xác nhận',
      className: 'bg-[#ef9900]/20 text-[#855300]',
      dotClassName: 'bg-[#ef9900]',
    }
  }

  if (status === 2) {
    return {
      label: 'Đã xác nhận',
      className: 'bg-[#22c55e]/20 text-[#006e2f]',
      dotClassName: 'bg-[#22c55e]',
    }
  }

  if (status === 3) {
    return {
      label: 'Đã hủy',
      className: 'bg-[#ffdad6] text-[#ba1a1a]',
      dotClassName: 'bg-[#ba1a1a]',
    }
  }

  return {
    label: 'Hoàn tất',
    className: 'bg-[#d5e0f8]/40 text-[#545f73]',
    dotClassName: 'bg-[#545f73]',
  }
}

function getTimeLabel(booking: BookingResponse) {
  return `${booking.startTime.slice(0, 5)} - ${booking.endTime.slice(0, 5)}`
}

export function AdminBookingCalendarPage() {
  const todayKey = toDateKey(new Date())
  const [calendarMonth, setCalendarMonth] = useState(() => startOfMonth(new Date()))
  const [selectedDate, setSelectedDate] = useState(todayKey)
  const [statusFilter, setStatusFilter] = useState<BookingStatus | 'all'>('all')
  const [bookings, setBookings] = useState<BookingResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [savingBookingId, setSavingBookingId] = useState<string | null>(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  const visibleRange = useMemo(() => {
    const firstDay = startOfWeek(startOfMonth(calendarMonth))
    const lastDay = endOfWeek(endOfMonth(calendarMonth))

    return {
      fromDate: toDateKey(firstDay),
      toDate: toDateKey(lastDay),
    }
  }, [calendarMonth])

  useEffect(() => {
    let isMounted = true

    getBookings({
      fromDate: visibleRange.fromDate,
      toDate: visibleRange.toDate,
      status: statusFilter === 'all' ? undefined : statusFilter,
    })
      .then((response) => {
        if (isMounted) {
          setBookings(response)
          setError('')
        }
      })
      .catch((err: unknown) => {
        if (isMounted) {
          setBookings([])
          setError(
            err instanceof Error ? err.message : 'Không tải được lịch đặt sân.',
          )
        }
      })
      .finally(() => {
        if (isMounted) {
          setIsLoading(false)
        }
      })

    return () => {
      isMounted = false
    }
  }, [visibleRange.fromDate, visibleRange.toDate, statusFilter])

  const calendarDays = useMemo<CalendarDay[]>(() => {
    const firstDay = parseDateKey(visibleRange.fromDate)

    return Array.from({ length: 42 }).map((_, index) => {
      const date = addDays(firstDay, index)
      const dateKey = toDateKey(date)

      return {
        date,
        dateKey,
        isCurrentMonth: date.getMonth() === calendarMonth.getMonth(),
        isToday: dateKey === todayKey,
      }
    })
  }, [calendarMonth, todayKey, visibleRange.fromDate])

  const bookingsByDate = useMemo(() => {
    const map = new Map<string, BookingResponse[]>()

    bookings.forEach((booking) => {
      const items = map.get(booking.bookingDate) ?? []
      items.push(booking)
      map.set(booking.bookingDate, items)
    })

    return map
  }, [bookings])

  const selectedBookings = useMemo(
    () => bookingsByDate.get(selectedDate) ?? [],
    [bookingsByDate, selectedDate],
  )

  const monthStats = useMemo(() => {
    const currentMonthBookings = bookings.filter((booking) => {
      const date = parseDateKey(booking.bookingDate)

      return (
        date.getMonth() === calendarMonth.getMonth() &&
        date.getFullYear() === calendarMonth.getFullYear()
      )
    })

    return {
      total: currentMonthBookings.length,
      pending: currentMonthBookings.filter((booking) => booking.status === 1).length,
      confirmed: currentMonthBookings.filter((booking) => booking.status === 2).length,
      completed: currentMonthBookings.filter((booking) => booking.status === 4).length,
    }
  }, [bookings, calendarMonth])

  function handleMonthChange(direction: -1 | 1) {
    setIsLoading(true)
    setError('')
    setSuccess('')
    setCalendarMonth(
      (current) => new Date(current.getFullYear(), current.getMonth() + direction, 1),
    )
  }

  function handleTodayClick() {
    const today = new Date()
    setIsLoading(true)
    setError('')
    setSuccess('')
    setCalendarMonth(startOfMonth(today))
    setSelectedDate(toDateKey(today))
  }

  function handleStatusFilterChange(value: BookingStatus | 'all') {
    setIsLoading(true)
    setError('')
    setSuccess('')
    setStatusFilter(value)
  }

  async function handleBookingStatusChange(booking: BookingResponse, status: BookingStatus) {
    if (booking.status === status) {
      return
    }

    setSavingBookingId(booking.id)
    setError('')
    setSuccess('')

    try {
      const updatedBooking = await updateBookingStatus(booking.id, { status })
      setBookings((current) =>
        current.map((item) => (item.id === updatedBooking.id ? updatedBooking : item)),
      )
      setSuccess('Cập nhật trạng thái booking thành công.')
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Cập nhật trạng thái booking thất bại.',
      )
    } finally {
      setSavingBookingId(null)
    }
  }

  return (
    <main className="min-h-screen bg-[#f7f9fb] text-[#191c1e]">
      <div className="mx-auto max-w-[1280px] px-4 pb-16 pt-24 sm:px-6">
        <header className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-sm font-bold uppercase tracking-[0.14em] text-[#006e2f]">
              Admin
            </p>
            <h1 className="mt-2 text-3xl font-bold tracking-[-0.01em]">Lịch đặt sân</h1>
            <p className="mt-2 text-[#3d4a3d]">
              Theo dõi booking theo dạng calendar và cập nhật trạng thái trực tiếp.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <AdminLink to="/admin/dashboard">Dashboard</AdminLink>
            <AdminLink to="/admin/bookings">Booking</AdminLink>
            <AdminLink to="/admin/courts">Quản lý sân</AdminLink>
            <AdminLink to="/admin/revenue">Doanh thu</AdminLink>
            <AdminLink to="/admin/price-rules">Bảng giá</AdminLink>
            <AdminLink to="/admin/users">User</AdminLink>
          </div>
        </header>

        <section className="mt-8 grid gap-4 md:grid-cols-4">
          <MetricCard label="Tổng booking" value={monthStats.total} />
          <MetricCard label="Chờ xác nhận" value={monthStats.pending} tone="orange" />
          <MetricCard label="Đã xác nhận" value={monthStats.confirmed} tone="green" />
          <MetricCard label="Hoàn tất" value={monthStats.completed} tone="gray" />
        </section>

        <section className="mt-6 rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <div className="flex items-center gap-3">
              <button
                type="button"
                onClick={() => handleMonthChange(-1)}
                className="h-10 w-10 rounded-lg border border-[#bccbb9] text-lg font-bold text-[#3d4a3d] transition hover:border-[#006e2f] hover:text-[#006e2f]"
                aria-label="Tháng trước"
              >
                ‹
              </button>
              <div className="min-w-[220px] text-center">
                <h2 className="text-xl font-bold capitalize">{formatMonthTitle(calendarMonth)}</h2>
                <p className="text-sm text-[#545f73]">
                  {visibleRange.fromDate} - {visibleRange.toDate}
                </p>
              </div>
              <button
                type="button"
                onClick={() => handleMonthChange(1)}
                className="h-10 w-10 rounded-lg border border-[#bccbb9] text-lg font-bold text-[#3d4a3d] transition hover:border-[#006e2f] hover:text-[#006e2f]"
                aria-label="Tháng sau"
              >
                ›
              </button>
              <button
                type="button"
                onClick={handleTodayClick}
                className="rounded-lg bg-[#006e2f] px-4 py-2 text-sm font-bold text-white"
              >
                Hôm nay
              </button>
            </div>

            <label className="w-full lg:w-[260px]">
              <span className="text-sm font-semibold text-[#3d4a3d]">Lọc trạng thái</span>
              <select
                value={statusFilter}
                onChange={(event) =>
                  handleStatusFilterChange(
                    event.target.value === 'all'
                      ? 'all'
                      : (Number(event.target.value) as BookingStatus),
                  )
                }
                className="mt-2 w-full rounded-lg border border-[#bccbb9] bg-white px-4 py-3 text-sm outline-none transition focus:border-[#006e2f] focus:ring-2 focus:ring-[#006e2f]/15"
              >
                {statusOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
          </div>
        </section>

        {error ? <Alert tone="error" message={error} /> : success ? <Alert tone="success" message={success} /> : null}

        <section className="mt-6 grid gap-6 xl:grid-cols-[1.5fr_420px]">
          <div className="rounded-xl border border-[#bccbb9] bg-white p-4 shadow-sm">
            <div className="grid grid-cols-7 gap-2 border-b border-[#e0e3e5] pb-3">
              {weekDays.map((day) => (
                <div
                  key={day}
                  className="text-center text-xs font-bold uppercase tracking-[0.08em] text-[#545f73]"
                >
                  {day}
                </div>
              ))}
            </div>

            <div className="mt-3 grid grid-cols-7 gap-2">
              {calendarDays.map((day) => {
                const dayBookings = bookingsByDate.get(day.dateKey) ?? []
                const isSelected = selectedDate === day.dateKey

                return (
                  <button
                    key={day.dateKey}
                    type="button"
                    onClick={() => {
                      setSelectedDate(day.dateKey)
                      setSuccess('')
                      setError('')
                    }}
                    className={[
                      'min-h-[112px] rounded-lg border p-3 text-left transition',
                      isSelected
                        ? 'border-[#006e2f] bg-[#e8f5e9] shadow-sm'
                        : 'border-[#e0e3e5] bg-white hover:border-[#006e2f]/60',
                      day.isCurrentMonth ? '' : 'opacity-45',
                      day.isToday ? 'ring-2 ring-[#006e2f]/25' : '',
                    ].join(' ')}
                  >
                    <div className="flex items-start justify-between gap-2">
                      <span className="text-sm font-bold">{day.date.getDate()}</span>
                      {dayBookings.length ? (
                        <span className="rounded-full bg-[#006e2f] px-2 py-0.5 text-[11px] font-bold text-white">
                          {dayBookings.length}
                        </span>
                      ) : null}
                    </div>
                    <div className="mt-3 space-y-1">
                      {dayBookings.slice(0, 3).map((booking) => {
                        const status = getStatusMeta(booking.status)

                        return (
                          <div
                            key={booking.id}
                            className="flex items-center gap-1.5 truncate text-xs font-medium text-[#3d4a3d]"
                          >
                            <span
                              className={['h-2 w-2 shrink-0 rounded-full', status.dotClassName].join(' ')}
                            />
                            <span className="truncate">{booking.courtName}</span>
                          </div>
                        )
                      })}
                      {dayBookings.length > 3 ? (
                        <p className="text-xs font-semibold text-[#006e2f]">
                          +{dayBookings.length - 3} booking khác
                        </p>
                      ) : null}
                    </div>
                  </button>
                )
              })}
            </div>
          </div>

          <aside className="rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
            <div className="flex items-start justify-between gap-3">
              <div>
                <p className="text-sm font-bold uppercase tracking-[0.12em] text-[#006e2f]">
                  Ngày đã chọn
                </p>
                <h2 className="mt-2 text-xl font-bold">{formatDate(selectedDate)}</h2>
              </div>
              {isLoading ? <span className="text-sm font-semibold text-[#006e2f]">Đang tải...</span> : null}
            </div>

            <div className="mt-5 space-y-4">
              {selectedBookings.length ? (
                selectedBookings.map((booking) => (
                  <BookingCard
                    key={booking.id}
                    booking={booking}
                    isSaving={savingBookingId === booking.id}
                    onStatusChange={(status) => handleBookingStatusChange(booking, status)}
                  />
                ))
              ) : (
                <div className="rounded-lg border border-dashed border-[#bccbb9] p-5 text-center">
                  <p className="font-bold">Không có booking</p>
                  <p className="mt-2 text-sm text-[#545f73]">
                    Ngày này chưa có lịch đặt sân theo bộ lọc hiện tại.
                  </p>
                </div>
              )}
            </div>
          </aside>
        </section>
      </div>
    </main>
  )
}

function AdminLink({ to, children }: { to: string; children: string }) {
  return (
    <Link
      to={to}
      className="rounded-lg border border-[#bccbb9] bg-white px-5 py-3 text-sm font-bold text-[#3d4a3d] transition hover:border-[#006e2f] hover:text-[#006e2f]"
    >
      {children}
    </Link>
  )
}

function MetricCard({
  label,
  value,
  tone = 'default',
}: {
  label: string
  value: number
  tone?: 'default' | 'green' | 'orange' | 'gray'
}) {
  const toneClassName =
    tone === 'green'
      ? 'text-[#006e2f]'
      : tone === 'orange'
        ? 'text-[#855300]'
        : tone === 'gray'
          ? 'text-[#545f73]'
          : 'text-[#191c1e]'

  return (
    <article className="rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
      <p className="text-sm font-semibold text-[#3d4a3d]">{label}</p>
      <p className={['mt-3 text-3xl font-bold', toneClassName].join(' ')}>{value}</p>
    </article>
  )
}

function BookingCard({
  booking,
  isSaving,
  onStatusChange,
}: {
  booking: BookingResponse
  isSaving: boolean
  onStatusChange: (status: BookingStatus) => void
}) {
  const status = getStatusMeta(booking.status)

  return (
    <article className="rounded-lg border border-[#e0e3e5] p-4">
      <div className="flex items-start justify-between gap-3">
        <div>
          <p className="font-bold">{booking.courtName}</p>
          <p className="mt-1 text-sm text-[#545f73]">
            {booking.userName} · {getTimeLabel(booking)}
          </p>
        </div>
        <span className={['shrink-0 rounded-full px-3 py-1 text-xs font-bold', status.className].join(' ')}>
          {status.label}
        </span>
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-2">
        <div className="rounded-lg bg-[#f7f9fb] p-3">
          <p className="text-xs font-semibold text-[#545f73]">Mã booking</p>
          <p className="mt-1 text-sm font-bold">#{booking.id.slice(0, 8).toUpperCase()}</p>
        </div>
        <div className="rounded-lg bg-[#f7f9fb] p-3">
          <p className="text-xs font-semibold text-[#545f73]">Tổng tiền</p>
          <p className="mt-1 text-sm font-bold text-[#006e2f]">
            {formatCurrency(booking.totalPrice)}
          </p>
        </div>
      </div>

      {booking.note ? (
        <p className="mt-3 rounded-lg bg-[#f7f9fb] p-3 text-sm text-[#3d4a3d]">{booking.note}</p>
      ) : null}

      <label className="mt-4 block">
        <span className="text-xs font-semibold text-[#545f73]">Cập nhật trạng thái</span>
        <select
          value={booking.status}
          disabled={isSaving}
          onChange={(event) => onStatusChange(Number(event.target.value) as BookingStatus)}
          className="mt-2 w-full rounded-lg border border-[#bccbb9] bg-white px-3 py-2 text-sm font-semibold outline-none transition focus:border-[#006e2f] focus:ring-2 focus:ring-[#006e2f]/15 disabled:cursor-not-allowed disabled:opacity-60"
        >
          {statusOptions
            .filter((option) => option.value !== 'all')
            .map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
        </select>
      </label>
    </article>
  )
}

function Alert({ tone, message }: { tone: 'success' | 'error'; message: string }) {
  return (
    <div
      className={[
        'mt-6 rounded-lg border px-4 py-3 text-sm font-medium',
        tone === 'success'
          ? 'border-[#22c55e]/30 bg-[#22c55e]/10 text-[#006e2f]'
          : 'border-[#f5b4ab] bg-[#ffdad6] text-[#93000a]',
      ].join(' ')}
    >
      {message}
    </div>
  )
}
