import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'

import { subscribeToAdminBookings } from '../lib/bookingRealtime'
import {
  exportBookingsReport,
  getBookings,
  updateBookingStatus,
  type BookingResponse,
  type BookingStatus,
} from '../lib/bookingsApi'

const statusOptions: Array<{ value: BookingStatus | 'all'; label: string }> = [
  { value: 'all', label: 'Tất cả trạng thái' },
  { value: 1, label: 'Chờ xác nhận' },
  { value: 2, label: 'Đã xác nhận' },
  { value: 3, label: 'Đã hủy' },
  { value: 4, label: 'Hoàn tất' },
]

function toDateInputValue(date: Date) {
  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')

  return `${year}-${month}-${day}`
}

function getDefaultFromDate() {
  const date = new Date()
  date.setDate(date.getDate() - 30)

  return toDateInputValue(date)
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(value))
}

function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

function getTimeLabel(booking: BookingResponse) {
  return `${booking.startTime.slice(0, 5)} - ${booking.endTime.slice(0, 5)}`
}

function getStatusMeta(status: BookingStatus) {
  if (status === 1) {
    return {
      label: 'Chờ xác nhận',
      className: 'bg-[#ef9900]/20 text-[#855300]',
    }
  }

  if (status === 2) {
    return {
      label: 'Đã xác nhận',
      className: 'bg-[#22c55e]/20 text-[#006e2f]',
    }
  }

  if (status === 3) {
    return {
      label: 'Đã hủy',
      className: 'bg-[#ffdad6] text-[#ba1a1a]',
    }
  }

  return {
    label: 'Hoàn tất',
    className: 'bg-[#d5e0f8]/40 text-[#545f73]',
  }
}

export function AdminBookingsPage() {
  const [fromDate, setFromDate] = useState(getDefaultFromDate)
  const [toDate, setToDate] = useState(() => toDateInputValue(new Date()))
  const [statusFilter, setStatusFilter] = useState<BookingStatus | 'all'>('all')
  const [keyword, setKeyword] = useState('')
  const [bookings, setBookings] = useState<BookingResponse[]>([])
  const [selectedBooking, setSelectedBooking] = useState<BookingResponse | null>(null)
  const [isLoading, setIsLoading] = useState(true)
  const [isExporting, setIsExporting] = useState(false)
  const [savingBookingId, setSavingBookingId] = useState<string | null>(null)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  useEffect(() => {
    let isMounted = true

    getBookings({
      fromDate,
      toDate,
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
            err instanceof Error
              ? err.message
              : 'Không tải được danh sách booking.',
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
  }, [fromDate, toDate, statusFilter])

  useEffect(() => {
    return subscribeToAdminBookings({
      onBookingCreated: (event) => {
        if (!event.booking || !isBookingInCurrentFilter(event.booking)) {
          return
        }

        setBookings((current) => upsertBooking(current, event.booking!))
        setSuccess('Có booking mới vừa được tạo.')
      },
      onBookingStatusUpdated: (event) => {
        if (!event.booking) {
          return
        }

        const updatedBooking = event.booking

        setBookings((current) => {
          const withoutCurrent = current.filter((booking) => booking.id !== updatedBooking.id)

          if (!isBookingInCurrentFilter(updatedBooking)) {
            return withoutCurrent
          }

          return upsertBooking(withoutCurrent, updatedBooking)
        })
        setSelectedBooking((current) =>
          current?.id === updatedBooking.id ? updatedBooking : current,
        )
      },
      onError: () => {
        setError('Không thể kết nối realtime booking. Dữ liệu vẫn được tải theo bộ lọc hiện tại.')
      },
    })
  }, [fromDate, toDate, statusFilter])

  function refreshWithLoading(action: () => void) {
    setIsLoading(true)
    setError('')
    setSuccess('')
    action()
  }

  const filteredBookings = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase()

    if (!normalizedKeyword) {
      return bookings
    }

    return bookings.filter((booking) =>
      `${booking.id} ${booking.userName} ${booking.courtName} ${booking.note ?? ''} ${booking.startTime} ${booking.endTime}`
        .toLowerCase()
        .includes(normalizedKeyword),
    )
  }, [bookings, keyword])

  const stats = useMemo(
    () => ({
      total: bookings.length,
      pending: bookings.filter((booking) => booking.status === 1).length,
      confirmed: bookings.filter((booking) => booking.status === 2).length,
      completed: bookings.filter((booking) => booking.status === 4).length,
      revenue: bookings
        .filter((booking) => booking.status === 4)
        .reduce((sum, booking) => sum + booking.totalPrice, 0),
    }),
    [bookings],
  )

  async function handleStatusChange(booking: BookingResponse, status: BookingStatus) {
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
      setSelectedBooking((current) =>
        current?.id === updatedBooking.id ? updatedBooking : current,
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

  async function handleExport() {
    setIsExporting(true)
    setError('')
    setSuccess('')

    try {
      await exportBookingsReport({
        fromDate,
        toDate,
        status: statusFilter === 'all' ? undefined : statusFilter,
      })
      setSuccess('Xuất file Excel thành công.')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Xuất file Excel thất bại.')
    } finally {
      setIsExporting(false)
    }
  }

  function isBookingInCurrentFilter(booking: BookingResponse) {
    return (
      booking.bookingDate >= fromDate &&
      booking.bookingDate <= toDate &&
      (statusFilter === 'all' || booking.status === statusFilter)
    )
  }

  return (
    <main className="min-h-screen bg-[#f7f9fb] text-[#191c1e]">
      <div className="mx-auto max-w-[1280px] px-4 pb-16 pt-24 sm:px-6">
        <header className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-sm font-bold uppercase tracking-[0.14em] text-[#006e2f]">
              Admin
            </p>
            <h1 className="mt-2 text-3xl font-bold tracking-[-0.01em]">Quản lý booking</h1>
            <p className="mt-2 text-[#3d4a3d]">
              Theo dõi lịch đặt sân, lọc theo ngày và cập nhật trạng thái.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <AdminLink to="/admin/dashboard">Dashboard</AdminLink>
            <AdminLink to="/admin/bookings/calendar">Calendar</AdminLink>
            <AdminLink to="/admin/revenue">Doanh thu</AdminLink>
            <AdminLink to="/admin/courts">Quản lý sân</AdminLink>
            <AdminLink to="/admin/price-rules">Bảng giá</AdminLink>
            <AdminLink to="/admin/users">User</AdminLink>
          </div>
        </header>

        <section className="mt-8 grid gap-4 md:grid-cols-5">
          <MetricCard label="Tổng booking" value={stats.total.toString()} />
          <MetricCard label="Chờ xác nhận" value={stats.pending.toString()} tone="orange" />
          <MetricCard label="Đã xác nhận" value={stats.confirmed.toString()} tone="green" />
          <MetricCard label="Hoàn tất" value={stats.completed.toString()} tone="gray" />
          <MetricCard
            label="Doanh thu completed"
            value={formatCurrency(stats.revenue)}
            tone="green"
          />
        </section>

        <section className="mt-6 rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
          <div className="grid gap-4 lg:grid-cols-[1fr_1fr_220px_1.3fr] lg:items-end">
            <label>
              <span className="text-sm font-semibold text-[#3d4a3d]">Từ ngày</span>
              <input
                type="date"
                value={fromDate}
                onChange={(event) => refreshWithLoading(() => setFromDate(event.target.value))}
                className="mt-2 h-12 w-full rounded-lg bg-[#eceef0] px-4 outline-none focus:ring-2 focus:ring-[#006e2f]/30"
              />
            </label>
            <label>
              <span className="text-sm font-semibold text-[#3d4a3d]">Đến ngày</span>
              <input
                type="date"
                value={toDate}
                onChange={(event) => refreshWithLoading(() => setToDate(event.target.value))}
                className="mt-2 h-12 w-full rounded-lg bg-[#eceef0] px-4 outline-none focus:ring-2 focus:ring-[#006e2f]/30"
              />
            </label>
            <label>
              <span className="text-sm font-semibold text-[#3d4a3d]">Trạng thái</span>
              <select
                value={statusFilter}
                onChange={(event) =>
                  refreshWithLoading(() =>
                    setStatusFilter(
                      event.target.value === 'all'
                        ? 'all'
                        : (Number(event.target.value) as BookingStatus),
                    ),
                  )
                }
                className="mt-2 h-12 w-full rounded-lg bg-[#eceef0] px-4 outline-none focus:ring-2 focus:ring-[#006e2f]/30"
              >
                {statusOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              <span className="text-sm font-semibold text-[#3d4a3d]">Tìm kiếm</span>
              <input
                value={keyword}
                onChange={(event) => setKeyword(event.target.value)}
                placeholder="Mã booking, khách hàng, tên sân..."
                className="mt-2 h-12 w-full rounded-lg bg-[#eceef0] px-4 outline-none focus:ring-2 focus:ring-[#006e2f]/30"
              />
            </label>
          </div>
          <div className="mt-4 flex justify-end">
            <button
              type="button"
              onClick={handleExport}
              disabled={isExporting}
              className="rounded-lg bg-[#006e2f] px-5 py-3 text-sm font-bold text-white transition hover:bg-[#005321] disabled:cursor-not-allowed disabled:bg-[#bccbb9]"
            >
              {isExporting ? 'Đang xuất...' : 'Xuất Excel'}
            </button>
          </div>
        </section>

        {error ? <Alert tone="error" message={error} /> : null}
        {success ? <Alert tone="success" message={success} /> : null}

        <section className="mt-6 rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[1050px] border-collapse">
              <thead>
                <tr className="border-b border-[#e0e3e5] text-left text-sm text-[#545f73]">
                  <th className="py-3 pr-4 font-semibold">Booking</th>
                  <th className="py-3 pr-4 font-semibold">Khách hàng</th>
                  <th className="py-3 pr-4 font-semibold">Sân</th>
                  <th className="py-3 pr-4 font-semibold">Ngày đặt</th>
                  <th className="py-3 pr-4 font-semibold">Giờ</th>
                  <th className="py-3 pr-4 text-right font-semibold">Tổng tiền</th>
                  <th className="py-3 pr-4 font-semibold">Trạng thái</th>
                  <th className="py-3 pr-4 text-right font-semibold">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {isLoading
                  ? Array.from({ length: 6 }).map((_, index) => (
                      <tr key={index} className="border-b border-[#eef1ef]">
                        <td colSpan={8} className="py-4">
                          <div className="h-10 animate-pulse rounded-lg bg-[#eceef0]" />
                        </td>
                      </tr>
                    ))
                  : filteredBookings.map((booking) => (
                      <BookingRow
                        key={booking.id}
                        booking={booking}
                        isSaving={savingBookingId === booking.id}
                        onView={setSelectedBooking}
                        onStatusChange={(status) => handleStatusChange(booking, status)}
                      />
                    ))}
              </tbody>
            </table>
          </div>

          {!isLoading && filteredBookings.length === 0 ? (
            <div className="rounded-lg border border-dashed border-[#bccbb9] p-8 text-center text-sm text-[#545f73]">
              Không có booking phù hợp với bộ lọc hiện tại.
            </div>
          ) : null}
        </section>

        {selectedBooking ? (
          <BookingDetailModal
            booking={selectedBooking}
            isSaving={savingBookingId === selectedBooking.id}
            onClose={() => setSelectedBooking(null)}
            onStatusChange={(status) => handleStatusChange(selectedBooking, status)}
          />
        ) : null}
      </div>
    </main>
  )
}

function upsertBooking(bookings: BookingResponse[], booking: BookingResponse) {
  const exists = bookings.some((item) => item.id === booking.id)

  if (exists) {
    return bookings.map((item) => (item.id === booking.id ? booking : item))
  }

  return [booking, ...bookings]
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
  value: string
  tone?: 'default' | 'green' | 'orange' | 'gray'
}) {
  const valueClassName =
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
      <p className={['mt-3 text-2xl font-bold', valueClassName].join(' ')}>{value}</p>
    </article>
  )
}

function BookingRow({
  booking,
  isSaving,
  onView,
  onStatusChange,
}: {
  booking: BookingResponse
  isSaving: boolean
  onView: (booking: BookingResponse) => void
  onStatusChange: (status: BookingStatus) => void
}) {
  const status = getStatusMeta(booking.status)

  return (
    <tr className="border-b border-[#eef1ef] text-sm last:border-0">
      <td className="py-4 pr-4">
        <p className="font-bold">#{booking.id.slice(0, 8).toUpperCase()}</p>
        <p className="mt-1 text-xs text-[#545f73]">Tạo lúc {formatDateTime(booking.createdAt)}</p>
      </td>
      <td className="py-4 pr-4 font-semibold">{booking.userName}</td>
      <td className="py-4 pr-4 text-[#3d4a3d]">{booking.courtName}</td>
      <td className="py-4 pr-4 text-[#3d4a3d]">{formatDate(booking.bookingDate)}</td>
      <td className="py-4 pr-4 text-[#3d4a3d]">{getTimeLabel(booking)}</td>
      <td className="py-4 pr-4 text-right font-bold text-[#006e2f]">
        {formatCurrency(booking.totalPrice)}
      </td>
      <td className="py-4 pr-4">
        <span className={['rounded-full px-3 py-1 text-xs font-bold', status.className].join(' ')}>
          {status.label}
        </span>
      </td>
      <td className="py-4 pr-4 text-right">
        <div className="flex items-center justify-end gap-2">
          <select
            value={booking.status}
            disabled={isSaving}
            onChange={(event) => onStatusChange(Number(event.target.value) as BookingStatus)}
            className="h-9 rounded-lg border border-[#bccbb9] bg-white px-2 text-xs font-semibold outline-none focus:ring-2 focus:ring-[#006e2f]/20 disabled:opacity-60"
          >
            {statusOptions
              .filter((option) => option.value !== 'all')
              .map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
          </select>
          <button
            type="button"
            onClick={() => onView(booking)}
            className="rounded-lg border border-[#bccbb9] px-3 py-2 text-xs font-bold text-[#3d4a3d] transition hover:border-[#006e2f] hover:text-[#006e2f]"
          >
            Chi tiết
          </button>
        </div>
      </td>
    </tr>
  )
}

function BookingDetailModal({
  booking,
  isSaving,
  onClose,
  onStatusChange,
}: {
  booking: BookingResponse
  isSaving: boolean
  onClose: () => void
  onStatusChange: (status: BookingStatus) => void
}) {
  const status = getStatusMeta(booking.status)

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-[620px] rounded-xl bg-white p-6 shadow-xl">
        <div className="flex items-start justify-between gap-4">
          <div>
            <p className="text-sm font-bold uppercase tracking-[0.12em] text-[#006e2f]">
              Booking #{booking.id.slice(0, 8).toUpperCase()}
            </p>
            <h2 className="mt-2 text-2xl font-bold">{booking.courtName}</h2>
            <p className="mt-1 text-sm text-[#545f73]">
              {formatDate(booking.bookingDate)} · {getTimeLabel(booking)}
            </p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-[#bccbb9] px-3 py-2 text-sm font-bold"
          >
            Đóng
          </button>
        </div>

        <div className="mt-6 grid gap-3 sm:grid-cols-2">
          <DetailItem label="Khách hàng" value={booking.userName} />
          <DetailItem label="Tổng tiền" value={formatCurrency(booking.totalPrice)} />
          <DetailItem label="Ngày tạo" value={formatDateTime(booking.createdAt)} />
          <DetailItem
            label="Cập nhật"
            value={booking.updatedAt ? formatDateTime(booking.updatedAt) : 'Chưa cập nhật'}
          />
        </div>

        <div className="mt-4 rounded-lg bg-[#f7f9fb] p-4">
          <p className="text-sm font-semibold text-[#545f73]">Ghi chú</p>
          <p className="mt-1 text-sm text-[#3d4a3d]">{booking.note || 'Không có ghi chú.'}</p>
        </div>

        <div className="mt-5 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <span className={['inline-flex w-fit rounded-full px-3 py-1 text-xs font-bold', status.className].join(' ')}>
            {status.label}
          </span>
          <label className="w-full sm:w-[260px]">
            <span className="text-xs font-semibold text-[#545f73]">Cập nhật trạng thái</span>
            <select
              value={booking.status}
              disabled={isSaving}
              onChange={(event) => onStatusChange(Number(event.target.value) as BookingStatus)}
              className="mt-2 h-11 w-full rounded-lg border border-[#bccbb9] bg-white px-3 text-sm font-semibold outline-none focus:ring-2 focus:ring-[#006e2f]/20 disabled:opacity-60"
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
        </div>
      </div>
    </div>
  )
}

function DetailItem({ label, value }: { label: string; value: string }) {
  return (
    <div className="rounded-lg bg-[#f7f9fb] p-4">
      <p className="text-xs font-semibold text-[#545f73]">{label}</p>
      <p className="mt-1 font-bold">{value}</p>
    </div>
  )
}

function Alert({ tone, message }: { tone: 'success' | 'error'; message: string }) {
  return (
    <div
      className={[
        'mt-6 rounded-xl border px-4 py-3 text-sm font-medium',
        tone === 'success'
          ? 'border-[#d6f5df] bg-[#e9f9ef] text-[#005321]'
          : 'border-[#ffdad6] bg-[#ffdad6] text-[#93000a]',
      ].join(' ')}
    >
      {message}
    </div>
  )
}
