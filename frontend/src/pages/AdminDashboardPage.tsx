import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'

import {
  getRevenueDashboard,
  type RevenueDashboardResponse,
  type RevenuePeriod,
} from '../lib/dashboardApi'

const periodOptions: Array<{ value: RevenuePeriod; label: string }> = [
  { value: 'Day', label: 'Ngày' },
  { value: 'Week', label: 'Tuần' },
  { value: 'Month', label: 'Tháng' },
]

function toDateInputValue(date: Date) {
  return date.toISOString().slice(0, 10)
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

function getDefaultFromDate() {
  const date = new Date()
  date.setDate(date.getDate() - 30)

  return toDateInputValue(date)
}

export function AdminDashboardPage() {
  const [fromDate, setFromDate] = useState(getDefaultFromDate)
  const [toDate, setToDate] = useState(() => toDateInputValue(new Date()))
  const [period, setPeriod] = useState<RevenuePeriod>('Day')
  const [dashboard, setDashboard] = useState<RevenueDashboardResponse | null>(
    null,
  )
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let isMounted = true

    getRevenueDashboard({ fromDate, toDate, period })
      .then((response) => {
        if (isMounted) {
          setDashboard(response)
        }
      })
      .catch((err: unknown) => {
        if (isMounted) {
          setDashboard(null)
          setError(
            err instanceof Error
              ? err.message
              : 'Không tải được dữ liệu dashboard.',
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

  function handleFromDateChange(value: string) {
    setIsLoading(true)
    setError('')
    setFromDate(value)
  }

  function handleToDateChange(value: string) {
    setIsLoading(true)
    setError('')
    setToDate(value)
  }

  function handlePeriodChange(value: RevenuePeriod) {
    setIsLoading(true)
    setError('')
    setPeriod(value)
  }

  const maxRevenue = useMemo(() => {
    if (!dashboard?.revenuePoints.length) {
      return 0
    }

    return Math.max(...dashboard.revenuePoints.map((point) => point.revenue))
  }, [dashboard])

  const periodLabel =
    periodOptions.find((option) => option.value === period)?.label ?? 'Ngày'

  return (
    <main className="min-h-screen bg-[#f7f9fb] text-[#191c1e]">
      <div className="mx-auto max-w-[1280px] px-4 pb-16 pt-24 sm:px-6">
        <header className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-sm font-bold uppercase tracking-[0.14em] text-[#006e2f]">
              Admin
            </p>
            <h1 className="mt-2 text-3xl font-bold tracking-[-0.01em]">
              Dashboard
            </h1>
            <p className="mt-2 text-[#3d4a3d]">
              Theo dõi doanh thu, số lượt đặt sân hoàn tất và hiệu suất theo thời gian.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <Link
              to="/admin"
              className="rounded-lg bg-[#006e2f] px-5 py-3 text-sm font-bold text-white shadow-sm"
            >
              Dashboard
            </Link>
            <Link
              to="/admin/courts"
              className="rounded-lg border border-[#bccbb9] bg-white px-5 py-3 text-sm font-bold text-[#3d4a3d] transition hover:border-[#006e2f] hover:text-[#006e2f]"
            >
              Quản lý sân
            </Link>
            <Link
              to="/admin/bookings"
              className="rounded-lg border border-[#bccbb9] bg-white px-5 py-3 text-sm font-bold text-[#3d4a3d] transition hover:border-[#006e2f] hover:text-[#006e2f]"
            >
              Booking
            </Link>
            <Link
              to="/admin/bookings/calendar"
              className="rounded-lg border border-[#bccbb9] bg-white px-5 py-3 text-sm font-bold text-[#3d4a3d] transition hover:border-[#006e2f] hover:text-[#006e2f]"
            >
              Lịch đặt sân
            </Link>
            <Link
              to="/admin/revenue"
              className="rounded-lg border border-[#bccbb9] bg-white px-5 py-3 text-sm font-bold text-[#3d4a3d] transition hover:border-[#006e2f] hover:text-[#006e2f]"
            >
              Doanh thu
            </Link>
            <Link
              to="/admin/users"
              className="rounded-lg border border-[#bccbb9] bg-white px-5 py-3 text-sm font-bold text-[#3d4a3d] transition hover:border-[#006e2f] hover:text-[#006e2f]"
            >
              User
            </Link>
          </div>
        </header>

        <section className="mt-8 rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
          <div className="grid gap-4 md:grid-cols-[1fr_1fr_220px]">
            <label>
              <span className="text-sm font-semibold text-[#3d4a3d]">
                Từ ngày
              </span>
              <input
                type="date"
                value={fromDate}
                onChange={(event) => handleFromDateChange(event.target.value)}
                className="mt-2 w-full rounded-lg border border-[#bccbb9] bg-white px-4 py-3 text-sm outline-none transition focus:border-[#006e2f] focus:ring-2 focus:ring-[#006e2f]/15"
              />
            </label>
            <label>
              <span className="text-sm font-semibold text-[#3d4a3d]">
                Đến ngày
              </span>
              <input
                type="date"
                value={toDate}
                onChange={(event) => handleToDateChange(event.target.value)}
                className="mt-2 w-full rounded-lg border border-[#bccbb9] bg-white px-4 py-3 text-sm outline-none transition focus:border-[#006e2f] focus:ring-2 focus:ring-[#006e2f]/15"
              />
            </label>
            <label>
              <span className="text-sm font-semibold text-[#3d4a3d]">
                Nhóm theo
              </span>
              <select
                value={period}
                onChange={(event) =>
                  handlePeriodChange(event.target.value as RevenuePeriod)
                }
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

        <section className="mt-6 grid gap-4 md:grid-cols-3">
          <MetricCard
            label="Tổng doanh thu"
            value={dashboard ? formatCurrency(dashboard.totalRevenue) : '--'}
            hint={
              dashboard
                ? `${formatDate(dashboard.fromDate)} - ${formatDate(dashboard.toDate)}`
                : 'Đang tải dữ liệu'
            }
          />
          <MetricCard
            label="Booking hoàn tất"
            value={dashboard ? dashboard.completedBookingCount.toString() : '--'}
            hint="Chỉ tính booking Completed"
          />
          <MetricCard
            label="Giá trị trung bình"
            value={
              dashboard ? formatCurrency(dashboard.averageBookingValue) : '--'
            }
            hint="Doanh thu / số booking hoàn tất"
          />
        </section>

        <section className="mt-6 grid gap-6 lg:grid-cols-[1.7fr_1fr]">
          <div className="rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
            <div className="flex flex-col gap-1 sm:flex-row sm:items-end sm:justify-between">
              <div>
                <h2 className="text-xl font-bold">Doanh thu theo {periodLabel.toLowerCase()}</h2>
                <p className="text-sm text-[#3d4a3d]">
                  Các mốc không có booking vẫn hiển thị để biểu đồ ổn định.
                </p>
              </div>
              {isLoading ? (
                <span className="text-sm font-semibold text-[#006e2f]">
                  Đang tải...
                </span>
              ) : null}
            </div>

            <div className="mt-6 flex h-[320px] items-end gap-3 overflow-x-auto border-b border-l border-[#e0e3e5] px-2 pb-3">
              {dashboard?.revenuePoints.length ? (
                dashboard.revenuePoints.map((point) => (
                  <div
                    key={`${point.fromDate}-${point.toDate}`}
                    className="flex min-w-[72px] flex-1 flex-col items-center justify-end gap-2"
                  >
                    <div className="text-xs font-semibold text-[#3d4a3d]">
                      {formatCurrency(point.revenue)}
                    </div>
                    <div
                      className="w-full rounded-t-lg bg-[#006e2f] transition-all"
                      style={{
                        height:
                          maxRevenue > 0
                            ? `${Math.max((point.revenue / maxRevenue) * 220, 8)}px`
                            : '8px',
                        opacity: point.revenue > 0 ? 1 : 0.25,
                      }}
                      title={`${point.label}: ${formatCurrency(point.revenue)}`}
                    />
                    <div className="line-clamp-2 min-h-8 text-center text-xs font-medium text-[#545f73]">
                      {point.label}
                    </div>
                  </div>
                ))
              ) : (
                <div className="flex h-full w-full items-center justify-center text-sm font-medium text-[#545f73]">
                  Chưa có dữ liệu doanh thu trong khoảng ngày này.
                </div>
              )}
            </div>
          </div>

          <div className="rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
            <h2 className="text-xl font-bold">Chi tiết</h2>
            <div className="mt-4 space-y-3">
              {dashboard?.revenuePoints.length ? (
                dashboard.revenuePoints.slice(0, 10).map((point) => (
                  <div
                    key={`${point.label}-${point.fromDate}`}
                    className="rounded-lg border border-[#e0e3e5] p-4"
                  >
                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="font-bold">{point.label}</p>
                        <p className="mt-1 text-xs text-[#545f73]">
                          {formatDate(point.fromDate)} - {formatDate(point.toDate)}
                        </p>
                      </div>
                      <span className="rounded-full bg-[#22c55e]/20 px-3 py-1 text-xs font-bold text-[#006e2f]">
                        {point.completedBookingCount} booking
                      </span>
                    </div>
                    <p className="mt-3 text-lg font-bold text-[#006e2f]">
                      {formatCurrency(point.revenue)}
                    </p>
                  </div>
                ))
              ) : (
                <p className="rounded-lg border border-dashed border-[#bccbb9] p-4 text-sm text-[#545f73]">
                  Không có điểm doanh thu để hiển thị.
                </p>
              )}
            </div>
          </div>
        </section>
      </div>
    </main>
  )
}

function MetricCard({
  label,
  value,
  hint,
}: {
  label: string
  value: string
  hint: string
}) {
  return (
    <article className="rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
      <p className="text-sm font-semibold text-[#3d4a3d]">{label}</p>
      <p className="mt-3 text-3xl font-bold tracking-[-0.02em] text-[#191c1e]">
        {value}
      </p>
      <p className="mt-2 text-sm text-[#545f73]">{hint}</p>
    </article>
  )
}
