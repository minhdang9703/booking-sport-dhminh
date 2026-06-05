import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'

import { getBookings, type BookingResponse, type BookingStatus } from '../lib/bookingsApi'
import {
  getRevenueDashboard,
  type RevenueDashboardResponse,
  type RevenuePeriod,
} from '../lib/dashboardApi'

const completedStatus: BookingStatus = 4

const periodOptions: Array<{ value: RevenuePeriod; label: string }> = [
  { value: 'Day', label: 'Ngày' },
  { value: 'Week', label: 'Tuần' },
  { value: 'Month', label: 'Tháng' },
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

function getBookingTime(booking: BookingResponse) {
  return `${booking.startTime.slice(0, 5)} - ${booking.endTime.slice(0, 5)}`
}

export function AdminRevenuePage() {
  const [fromDate, setFromDate] = useState(getDefaultFromDate)
  const [toDate, setToDate] = useState(() => toDateInputValue(new Date()))
  const [period, setPeriod] = useState<RevenuePeriod>('Day')
  const [keyword, setKeyword] = useState('')
  const [dashboard, setDashboard] = useState<RevenueDashboardResponse | null>(null)
  const [bookings, setBookings] = useState<BookingResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let isMounted = true

    Promise.all([
      getRevenueDashboard({ fromDate, toDate, period }),
      getBookings({ fromDate, toDate, status: completedStatus }),
    ])
      .then(([dashboardResponse, bookingResponse]) => {
        if (isMounted) {
          setDashboard(dashboardResponse)
          setBookings(bookingResponse)
          setError('')
        }
      })
      .catch((err: unknown) => {
        if (isMounted) {
          setDashboard(null)
          setBookings([])
          setError(
            err instanceof Error
              ? err.message
              : 'Không tải được dữ liệu doanh thu.',
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
  }, [fromDate, toDate, period])

  function refreshWithLoading(action: () => void) {
    setIsLoading(true)
    setError('')
    action()
  }

  const maxRevenue = useMemo(() => {
    if (!dashboard?.revenuePoints.length) {
      return 0
    }

    return Math.max(...dashboard.revenuePoints.map((point) => point.revenue))
  }, [dashboard])

  const filteredBookings = useMemo(() => {
    const normalizedKeyword = keyword.trim().toLowerCase()

    if (!normalizedKeyword) {
      return bookings
    }

    return bookings.filter((booking) =>
      `${booking.courtName} ${booking.userName} ${booking.id} ${booking.startTime} ${booking.endTime}`
        .toLowerCase()
        .includes(normalizedKeyword),
    )
  }, [bookings, keyword])

  const topCourts = useMemo(() => {
    const map = new Map<string, { courtName: string; revenue: number; bookingCount: number }>()

    bookings.forEach((booking) => {
      const current = map.get(booking.courtName) ?? {
        courtName: booking.courtName,
        revenue: 0,
        bookingCount: 0,
      }

      current.revenue += booking.totalPrice
      current.bookingCount += 1
      map.set(booking.courtName, current)
    })

    return Array.from(map.values())
      .sort((left, right) => right.revenue - left.revenue)
      .slice(0, 5)
  }, [bookings])

  const bestPoint = useMemo(() => {
    if (!dashboard?.revenuePoints.length) {
      return null
    }

    return dashboard.revenuePoints.reduce((best, current) =>
      current.revenue > best.revenue ? current : best,
    )
  }, [dashboard])

  return (
    <main className="min-h-screen bg-[#f7f9fb] text-[#191c1e]">
      <div className="mx-auto max-w-[1280px] px-4 pb-16 pt-24 sm:px-6">
        <header className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-sm font-bold uppercase tracking-[0.14em] text-[#006e2f]">
              Admin
            </p>
            <h1 className="mt-2 text-3xl font-bold tracking-[-0.01em]">Quản lý doanh thu</h1>
            <p className="mt-2 text-[#3d4a3d]">
              Theo dõi doanh thu từ booking hoàn tất, phân tích theo thời gian và sân.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <AdminLink to="/admin/dashboard">Dashboard</AdminLink>
            <AdminLink to="/admin/bookings">Booking</AdminLink>
            <AdminLink to="/admin/bookings/calendar">Lịch đặt sân</AdminLink>
            <AdminLink to="/admin/courts">Quản lý sân</AdminLink>
            <AdminLink to="/admin/price-rules">Bảng giá</AdminLink>
            <AdminLink to="/admin/users">User</AdminLink>
          </div>
        </header>

        <section className="mt-8 rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
          <div className="grid gap-4 md:grid-cols-[1fr_1fr_220px]">
            <label>
              <span className="text-sm font-semibold text-[#3d4a3d]">Từ ngày</span>
              <input
                type="date"
                value={fromDate}
                onChange={(event) => refreshWithLoading(() => setFromDate(event.target.value))}
                className="mt-2 w-full rounded-lg border border-[#bccbb9] bg-white px-4 py-3 text-sm outline-none transition focus:border-[#006e2f] focus:ring-2 focus:ring-[#006e2f]/15"
              />
            </label>
            <label>
              <span className="text-sm font-semibold text-[#3d4a3d]">Đến ngày</span>
              <input
                type="date"
                value={toDate}
                onChange={(event) => refreshWithLoading(() => setToDate(event.target.value))}
                className="mt-2 w-full rounded-lg border border-[#bccbb9] bg-white px-4 py-3 text-sm outline-none transition focus:border-[#006e2f] focus:ring-2 focus:ring-[#006e2f]/15"
              />
            </label>
            <label>
              <span className="text-sm font-semibold text-[#3d4a3d]">Nhóm theo</span>
              <select
                value={period}
                onChange={(event) => refreshWithLoading(() => setPeriod(event.target.value as RevenuePeriod))}
                className="mt-2 w-full rounded-lg border border-[#bccbb9] bg-white px-4 py-3 text-sm outline-none transition focus:border-[#006e2f] focus:ring-2 focus:ring-[#006e2f]/15"
              >
                {periodOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
          </div>
        </section>

        {error ? (
          <div className="mt-6 rounded-lg border border-[#f5b4ab] bg-[#ffdad6] px-4 py-3 text-sm font-medium text-[#93000a]">
            {error}
          </div>
        ) : null}

        <section className="mt-6 grid gap-4 lg:grid-cols-4">
          <MetricCard
            label="Tổng doanh thu"
            value={dashboard ? formatCurrency(dashboard.totalRevenue) : '--'}
            hint={
              dashboard
                ? `${formatDate(dashboard.fromDate)} - ${formatDate(dashboard.toDate)}`
                : 'Đang tải'
            }
            tone="green"
          />
          <MetricCard
            label="Booking hoàn tất"
            value={dashboard ? dashboard.completedBookingCount.toString() : '--'}
            hint="Nguồn tính: Completed bookings"
          />
          <MetricCard
            label="Giá trị trung bình"
            value={dashboard ? formatCurrency(dashboard.averageBookingValue) : '--'}
            hint="Doanh thu / booking"
          />
          <MetricCard
            label="Mốc cao nhất"
            value={bestPoint ? formatCurrency(bestPoint.revenue) : '--'}
            hint={bestPoint?.label ?? 'Chưa có dữ liệu'}
            tone="orange"
          />
        </section>

        <section className="mt-6 grid gap-6 xl:grid-cols-[1.65fr_0.9fr]">
          <div className="rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
            <div className="flex flex-col gap-1 sm:flex-row sm:items-end sm:justify-between">
              <div>
                <h2 className="text-xl font-bold">Biểu đồ doanh thu</h2>
                <p className="text-sm text-[#545f73]">
                  Dữ liệu được group theo lựa chọn ngày, tuần hoặc tháng.
                </p>
              </div>
              {isLoading ? <span className="text-sm font-semibold text-[#006e2f]">Đang tải...</span> : null}
            </div>

            <div className="mt-6 flex h-[340px] items-end gap-3 overflow-x-auto border-b border-l border-[#e0e3e5] px-2 pb-3">
              {dashboard?.revenuePoints.length ? (
                dashboard.revenuePoints.map((point) => (
                  <div
                    key={`${point.fromDate}-${point.toDate}`}
                    className="flex min-w-[76px] flex-1 flex-col items-center justify-end gap-2"
                  >
                    <span className="text-xs font-semibold text-[#3d4a3d]">
                      {formatCurrency(point.revenue)}
                    </span>
                    <div
                      className="w-full rounded-t-lg bg-[#006e2f]"
                      style={{
                        height:
                          maxRevenue > 0
                            ? `${Math.max((point.revenue / maxRevenue) * 230, 8)}px`
                            : '8px',
                        opacity: point.revenue > 0 ? 1 : 0.25,
                      }}
                    />
                    <span className="line-clamp-2 min-h-8 text-center text-xs font-medium text-[#545f73]">
                      {point.label}
                    </span>
                  </div>
                ))
              ) : (
                <div className="flex h-full w-full items-center justify-center text-sm font-medium text-[#545f73]">
                  Chưa có doanh thu trong khoảng ngày này.
                </div>
              )}
            </div>
          </div>

          <div className="rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
            <h2 className="text-xl font-bold">Top sân theo doanh thu</h2>
            <div className="mt-5 space-y-3">
              {topCourts.length ? (
                topCourts.map((court, index) => (
                  <div
                    key={court.courtName}
                    className="rounded-lg border border-[#e0e3e5] p-4"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-xs font-bold text-[#006e2f]">#{index + 1}</p>
                        <p className="mt-1 font-bold">{court.courtName}</p>
                      </div>
                      <span className="rounded-full bg-[#22c55e]/20 px-3 py-1 text-xs font-bold text-[#006e2f]">
                        {court.bookingCount} booking
                      </span>
                    </div>
                    <p className="mt-3 text-lg font-bold text-[#006e2f]">
                      {formatCurrency(court.revenue)}
                    </p>
                  </div>
                ))
              ) : (
                <p className="rounded-lg border border-dashed border-[#bccbb9] p-4 text-sm text-[#545f73]">
                  Chưa có sân phát sinh doanh thu.
                </p>
              )}
            </div>
          </div>
        </section>

        <section className="mt-6 rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
            <div>
              <h2 className="text-xl font-bold">Booking đã ghi nhận doanh thu</h2>
              <p className="mt-1 text-sm text-[#545f73]">
                Chỉ hiển thị booking có trạng thái Completed trong khoảng ngày đã chọn.
              </p>
            </div>
            <label className="w-full lg:w-[340px]">
              <span className="text-sm font-semibold text-[#3d4a3d]">Tìm kiếm</span>
              <input
                value={keyword}
                onChange={(event) => setKeyword(event.target.value)}
                placeholder="Tên sân, khách hàng, mã booking"
                className="mt-2 w-full rounded-lg border border-[#bccbb9] bg-white px-4 py-3 text-sm outline-none transition focus:border-[#006e2f] focus:ring-2 focus:ring-[#006e2f]/15"
              />
            </label>
          </div>

          <div className="mt-5 overflow-x-auto">
            <table className="w-full min-w-[860px] border-collapse">
              <thead>
                <tr className="border-b border-[#e0e3e5] text-left text-sm text-[#545f73]">
                  <th className="py-3 pr-4 font-semibold">Mã booking</th>
                  <th className="py-3 pr-4 font-semibold">Ngày</th>
                  <th className="py-3 pr-4 font-semibold">Sân</th>
                  <th className="py-3 pr-4 font-semibold">Khách hàng</th>
                  <th className="py-3 pr-4 font-semibold">Giờ</th>
                  <th className="py-3 pr-4 text-right font-semibold">Doanh thu</th>
                </tr>
              </thead>
              <tbody>
                {filteredBookings.map((booking) => (
                  <tr key={booking.id} className="border-b border-[#eef1ef] text-sm last:border-0">
                    <td className="py-4 pr-4 font-bold">#{booking.id.slice(0, 8).toUpperCase()}</td>
                    <td className="py-4 pr-4 text-[#3d4a3d]">{formatDate(booking.bookingDate)}</td>
                    <td className="py-4 pr-4 font-semibold">{booking.courtName}</td>
                    <td className="py-4 pr-4 text-[#3d4a3d]">{booking.userName}</td>
                    <td className="py-4 pr-4 text-[#3d4a3d]">{getBookingTime(booking)}</td>
                    <td className="py-4 pr-4 text-right font-bold text-[#006e2f]">
                      {formatCurrency(booking.totalPrice)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {!filteredBookings.length ? (
              <div className="rounded-lg border border-dashed border-[#bccbb9] p-8 text-center text-sm text-[#545f73]">
                Không có booking Completed phù hợp.
              </div>
            ) : null}
          </div>
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
  hint,
  tone = 'default',
}: {
  label: string
  value: string
  hint: string
  tone?: 'default' | 'green' | 'orange'
}) {
  const valueClassName =
    tone === 'green'
      ? 'text-[#006e2f]'
      : tone === 'orange'
        ? 'text-[#855300]'
        : 'text-[#191c1e]'

  return (
    <article className="rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
      <p className="text-sm font-semibold text-[#3d4a3d]">{label}</p>
      <p className={['mt-3 text-2xl font-bold tracking-[-0.02em]', valueClassName].join(' ')}>
        {value}
      </p>
      <p className="mt-2 text-sm text-[#545f73]">{hint}</p>
    </article>
  )
}
