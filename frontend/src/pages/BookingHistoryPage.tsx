import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'

import basketballImage from '../assets/bookings/booking-basketball.png'
import footballImage from '../assets/bookings/booking-football.png'
import footballAltImage from '../assets/bookings/booking-football-alt.png'
import tennisImage from '../assets/bookings/booking-tennis.png'
import { getAccessToken } from '../lib/authApi'
import { getMyBookings, type BookingResponse } from '../lib/bookingsApi'

type BookingFilter = 'all' | 'upcoming' | 'completed' | 'cancelled'

const filters: Array<{ value: BookingFilter; label: string }> = [
  { value: 'all', label: 'Tất cả' },
  { value: 'upcoming', label: 'Sắp tới' },
  { value: 'completed', label: 'Đã hoàn thành' },
  { value: 'cancelled', label: 'Đã hủy' },
]

const demoBookings: BookingResponse[] = [
  {
    id: 'ab-9821',
    userId: 'demo',
    userName: 'Nguyễn Văn A',
    courtScheduleId: 'demo',
    courtName: 'Sân Bóng Đá Mini A1 - Victory Arena',
    bookingDate: '2026-12-25',
    status: 2,
    totalPrice: 450000,
    note: '18:00 - 19:30',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'ab-8742',
    userId: 'demo',
    userName: 'Nguyễn Văn A',
    courtScheduleId: 'demo',
    courtName: 'Sân Tennis Cao Cấp - Central Park',
    bookingDate: '2026-12-15',
    status: 4,
    totalPrice: 300000,
    note: '08:00 - 10:00',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'ab-9905',
    userId: 'demo',
    userName: 'Nguyễn Văn A',
    courtScheduleId: 'demo',
    courtName: 'Nhà Thi Đấu Đa Năng - Sky Center',
    bookingDate: '2026-12-18',
    status: 1,
    totalPrice: 220000,
    note: '19:00 - 20:00',
    createdAt: new Date().toISOString(),
  },
  {
    id: 'ab-7651',
    userId: 'demo',
    userName: 'Nguyễn Văn A',
    courtScheduleId: 'demo',
    courtName: 'Sân Bóng Đá Mini B2 - Victory Arena',
    bookingDate: '2026-12-10',
    status: 3,
    totalPrice: 450000,
    note: '17:00 - 18:30',
    createdAt: new Date().toISOString(),
  },
]

function formatCurrency(value: number) {
  return `${new Intl.NumberFormat('vi-VN').format(value)}đ`
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: 'long',
    year: 'numeric',
  }).format(new Date(value))
}

function getShortId(id: string) {
  return `#${id.slice(0, 8).toUpperCase()}`
}

function getStatusMeta(status: number) {
  if (status === 4) {
    return {
      label: 'Đã hoàn thành',
      filter: 'completed' as BookingFilter,
      className: 'bg-[#d5e0f8]/30 text-[#545f73]',
    }
  }

  if (status === 3) {
    return {
      label: 'Đã hủy',
      filter: 'cancelled' as BookingFilter,
      className: 'bg-[#ffdad6] text-[#ba1a1a]',
    }
  }

  if (status === 1) {
    return {
      label: 'Chờ thanh toán',
      filter: 'upcoming' as BookingFilter,
      className: 'bg-[#ef9900]/20 text-[#855300]',
    }
  }

  return {
    label: 'Sắp tới',
    filter: 'upcoming' as BookingFilter,
    className: 'bg-[#22c55e]/20 text-[#006e2f]',
  }
}

function getBookingImage(booking: BookingResponse, index: number) {
  const name = booking.courtName.toLowerCase()

  if (name.includes('tennis')) {
    return tennisImage
  }

  if (name.includes('thi đấu') || name.includes('đa năng') || name.includes('bóng rổ')) {
    return basketballImage
  }

  return index % 2 === 0 ? footballImage : footballAltImage
}

function getTimeLabel(booking: BookingResponse) {
  return booking.note?.match(/\d{2}:\d{2}\s*-\s*\d{2}:\d{2}/)?.[0] ?? 'Đang cập nhật'
}

export function BookingHistoryPage() {
  const [activeFilter, setActiveFilter] = useState<BookingFilter>('all')
  const [bookings, setBookings] = useState<BookingResponse[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let isMounted = true

    if (!getAccessToken()) {
      Promise.resolve().then(() => {
        if (isMounted) {
          setBookings(demoBookings)
          setError('Bạn chưa đăng nhập. Đang hiển thị dữ liệu mẫu.')
          setIsLoading(false)
        }
      })

      return
    }

    getMyBookings()
      .then((response) => {
        if (isMounted) {
          setBookings(response)
          setError('')
        }
      })
      .catch((err: unknown) => {
        if (isMounted) {
          setBookings(demoBookings)
          setError(
            err instanceof Error
              ? `${err.message}. Đang hiển thị dữ liệu mẫu.`
              : 'Không tải được lịch sử đặt sân. Đang hiển thị dữ liệu mẫu.',
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
  }, [])

  const displayedBookings = useMemo(() => {
    if (activeFilter === 'all') {
      return bookings
    }

    return bookings.filter(
      (booking) => getStatusMeta(booking.status).filter === activeFilter,
    )
  }, [activeFilter, bookings])

  return (
    <main className="min-h-screen bg-[#f7f9fb] text-[#191c1e]">
      <div className="mx-auto max-w-[1280px] px-4 pb-16 pt-24 sm:px-6">
        <header>
          <h1 className="text-3xl font-bold tracking-[-0.01em]">
            Lịch sử đặt sân
          </h1>
          <p className="mt-2 text-base text-[#3d4a3d]">
            Quản lý và theo dõi tất cả các lượt đặt chỗ của bạn.
          </p>
        </header>

        <div className="mt-8 flex gap-4 overflow-x-auto pb-2">
          {filters.map((filter) => (
            <button
              key={filter.value}
              type="button"
              onClick={() => setActiveFilter(filter.value)}
              className={[
                'whitespace-nowrap rounded-full px-6 py-2 text-sm font-semibold transition',
                activeFilter === filter.value
                  ? 'bg-[#006e2f] text-white shadow-sm'
                  : 'bg-[#e6e8ea] text-[#3d4a3d] hover:bg-[#e0e3e5]',
              ].join(' ')}
            >
              {filter.label}
            </button>
          ))}
        </div>

        {error ? (
          <div className="mt-6 rounded-xl border border-[#ffdad6] bg-[#ffdad6] px-4 py-3 text-sm font-medium text-[#93000a]">
            {error}
          </div>
        ) : null}

        <section className="mt-6 space-y-6">
          {isLoading ? (
            Array.from({ length: 3 }).map((_, index) => (
              <div
                key={index}
                className="h-44 animate-pulse rounded-xl bg-white shadow-sm"
              />
            ))
          ) : displayedBookings.length === 0 ? (
            <div className="rounded-xl border border-[#bccbb9] bg-white p-10 text-center shadow-sm">
              <h2 className="text-xl font-bold">Chưa có lịch đặt phù hợp</h2>
              <p className="mt-2 text-[#3d4a3d]">
                Thử đổi bộ lọc hoặc đặt sân mới để bắt đầu.
              </p>
              <Link
                to="/courts"
                className="mt-6 inline-flex rounded-lg bg-[#006e2f] px-5 py-3 font-semibold text-white"
              >
                Xem sân trống
              </Link>
            </div>
          ) : (
            displayedBookings.map((booking, index) => (
              <BookingCard key={booking.id} booking={booking} index={index} />
            ))
          )}
        </section>
      </div>
    </main>
  )
}

function BookingCard({
  booking,
  index,
}: {
  booking: BookingResponse
  index: number
}) {
  const status = getStatusMeta(booking.status)

  return (
    <article className="grid gap-5 rounded-xl border border-[#bccbb9] bg-white p-5 shadow-[0_4px_12px_rgba(30,41,59,0.05)] md:grid-cols-[128px_1fr_auto] md:items-center md:p-6">
      <img
        src={getBookingImage(booking, index)}
        alt={booking.courtName}
        className="h-32 w-full rounded-lg object-cover md:w-32"
      />

      <div className="space-y-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <span
            className={`rounded-full px-3 py-1 text-xs font-bold tracking-[0.05em] ${status.className}`}
          >
            {status.label}
          </span>
          <span className="text-sm text-[#3d4a3d]">ID: {getShortId(booking.id)}</span>
        </div>

        <h2 className="text-2xl font-semibold leading-8">{booking.courtName}</h2>

        <div className="flex flex-wrap gap-5 text-sm text-[#3d4a3d]">
          <span>📅 {formatDate(booking.bookingDate)}</span>
          <span>⏱ {getTimeLabel(booking)}</span>
          <span className="font-bold text-[#191c1e]">
            💳 {formatCurrency(booking.totalPrice)}
          </span>
        </div>
      </div>

      <div className="flex flex-col gap-3 md:w-40">
        {booking.status === 4 ? (
          <>
            <button className="rounded-lg border border-[#006e2f] px-4 py-2 text-sm font-semibold text-[#006e2f]">
              Xem hóa đơn
            </button>
            <Link
              to="/courts"
              className="rounded-lg bg-[#e6e8ea] px-4 py-2 text-center text-sm font-semibold text-[#3d4a3d]"
            >
              Đặt lại
            </Link>
          </>
        ) : booking.status === 3 ? (
          <button className="rounded-lg border border-[#6d7b6c] px-4 py-2 text-sm font-semibold text-[#3d4a3d]">
            Xem lý do hủy
          </button>
        ) : (
          <>
            <button className="rounded-lg bg-[#006e2f] px-4 py-2 text-sm font-semibold text-white">
              Xem vé đặt
            </button>
            <button className="rounded-lg border border-[#ba1a1a] px-4 py-2 text-sm font-semibold text-[#ba1a1a]">
              Hủy đặt sân
            </button>
          </>
        )}
      </div>
    </article>
  )
}
