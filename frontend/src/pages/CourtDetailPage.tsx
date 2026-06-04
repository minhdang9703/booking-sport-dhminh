import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'

import galleryOne from '../assets/court-detail/gallery-1.png'
import galleryTwo from '../assets/court-detail/gallery-2.png'
import galleryThree from '../assets/court-detail/gallery-3.png'
import galleryFour from '../assets/court-detail/gallery-4.png'
import galleryMain from '../assets/court-detail/gallery-main.png'
import mapImage from '../assets/court-detail/map.png'
import {
  getAvailableSchedules,
  getCourtById,
  type AvailableSchedule,
  type Court,
} from '../lib/courtsApi'

const demoCourt: Court = {
  id: 'football-demo',
  venueId: 'demo',
  venueName: 'Phường 25, Bình Thạnh, TP. HCM',
  sportId: 'demo',
  sportName: 'Sân bóng đá',
  name: 'Sân Vận Động Bình Thạnh - Sân 7 Người',
  status: 1,
  description:
    'Sân cỏ nhân tạo đạt chuẩn, phù hợp các trận đấu phong trào và bán chuyên. Hệ thống thoát nước hiện đại giúp mặt sân ổn định trong nhiều điều kiện thời tiết.',
  createdAt: new Date().toISOString(),
}

const facilities = [
  { title: 'Gửi xe', description: 'Miễn phí và rộng rãi', icon: 'P' },
  { title: 'Nước uống', description: 'Nước lọc miễn phí', icon: 'W' },
  { title: 'Đèn LED', description: 'Độ sáng cao', icon: 'L' },
  { title: 'Phòng thay đồ', description: 'Sạch sẽ, riêng tư', icon: 'R' },
  { title: 'Áo bib', description: 'Cho thuê theo trận', icon: 'B' },
  { title: 'Wifi', description: 'Tốc độ cao', icon: 'F' },
]

const galleryImages = [galleryOne, galleryTwo, galleryThree, galleryFour]

function getToday() {
  return new Date().toISOString().slice(0, 10)
}

function formatTime(value: string) {
  return value.slice(0, 5)
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('vi-VN').format(value)
}

function getMinPrice(schedules: AvailableSchedule[]) {
  if (schedules.length === 0) {
    return 300000
  }

  return Math.min(...schedules.map((schedule) => schedule.price))
}

export function CourtDetailPage() {
  const navigate = useNavigate()
  const { courtId } = useParams()
  const [court, setCourt] = useState<Court | null>(null)
  const [selectedDate, setSelectedDate] = useState(getToday())
  const [schedules, setSchedules] = useState<AvailableSchedule[]>([])
  const [selectedScheduleId, setSelectedScheduleId] = useState('')
  const [isLoadingCourt, setIsLoadingCourt] = useState(true)
  const [isLoadingSchedules, setIsLoadingSchedules] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let isMounted = true

    if (!courtId) {
      Promise.resolve().then(() => {
        if (isMounted) {
          setCourt(demoCourt)
          setIsLoadingCourt(false)
        }
      })

      return
    }

    getCourtById(courtId)
      .then((response) => {
        if (isMounted) {
          setCourt(response)
          setError('')
        }
      })
      .catch((err: unknown) => {
        if (isMounted) {
          setCourt({ ...demoCourt, id: courtId })
          setError(
            err instanceof Error
              ? err.message
              : 'Không tải được chi tiết sân từ API.',
          )
        }
      })
      .finally(() => {
        if (isMounted) {
          setIsLoadingCourt(false)
        }
      })

    return () => {
      isMounted = false
    }
  }, [courtId])

  useEffect(() => {
    let isMounted = true

    if (!courtId) {
      return
    }

    getAvailableSchedules(courtId, selectedDate)
      .then((response) => {
        if (isMounted) {
          setSelectedScheduleId('')
          setSchedules(response)
        }
      })
      .catch((err: unknown) => {
        if (isMounted) {
          setSelectedScheduleId('')
          setSchedules([])
          setError(
            err instanceof Error
              ? err.message
              : 'Không tải được lịch trống từ API.',
          )
        }
      })
      .finally(() => {
        if (isMounted) {
          setIsLoadingSchedules(false)
        }
      })

    return () => {
      isMounted = false
    }
  }, [courtId, selectedDate])

  const minPrice = useMemo(() => getMinPrice(schedules), [schedules])
  const selectedSchedule = schedules.find(
    (schedule) => schedule.scheduleId === selectedScheduleId,
  )

  function handleBookNow() {
    if (!selectedSchedule || detail.status !== 1) {
      return
    }

    sessionStorage.setItem(
      'bookingSport.checkout',
      JSON.stringify({
        court: detail,
        schedule: selectedSchedule,
      }),
    )
    navigate('/checkout')
  }

  function handleDateChange(value: string) {
    setSelectedDate(value)
    setSelectedScheduleId('')
    setIsLoadingSchedules(true)
  }

  if (isLoadingCourt) {
    return (
      <main className="mx-auto min-h-screen max-w-[1280px] px-4 pt-24 sm:px-6">
        <div className="h-[640px] animate-pulse rounded-xl bg-white" />
      </main>
    )
  }

  const detail = court ?? demoCourt

  return (
    <main className="min-h-screen bg-[#f7f9fb] text-[#191c1e]">
      <div className="mx-auto max-w-[1280px] px-4 pb-12 pt-24 sm:px-6">
        <nav className="flex flex-wrap items-center gap-2 text-sm text-[#3d4a3d]">
          <Link to="/" className="hover:text-[#006e2f]">
            Trang chủ
          </Link>
          <span>/</span>
          <Link to="/courts" className="hover:text-[#006e2f]">
            {detail.sportName || 'Sân thể thao'}
          </Link>
          <span>/</span>
          <span className="font-semibold text-[#006e2f]">{detail.name}</span>
        </nav>

        <section className="mt-6 flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
          <div>
            <h1 className="max-w-4xl text-3xl font-bold leading-tight tracking-[-0.01em] sm:text-4xl">
              {detail.name}
            </h1>
            <div className="mt-3 flex flex-wrap gap-4 text-[#3d4a3d]">
              <span className="font-bold text-[#855300]">★ 4.8 (120 đánh giá)</span>
              <span>📍 {detail.venueName || 'Địa điểm đang cập nhật'}</span>
            </div>
          </div>

          <div className="flex gap-2">
            <button className="rounded-lg border border-[#bccbb9] bg-white px-4 py-2 text-sm font-semibold">
              Chia sẻ
            </button>
            <button className="rounded-lg border border-[#bccbb9] bg-white px-4 py-2 text-sm font-semibold">
              Lưu
            </button>
          </div>
        </section>

        {error ? (
          <div className="mt-6 rounded-xl border border-[#ffdad6] bg-[#ffdad6] px-4 py-3 text-sm font-medium text-[#93000a]">
            {error}. Đang hiển thị dữ liệu mẫu để kiểm tra giao diện.
          </div>
        ) : null}

        <section className="mt-8 grid h-auto gap-4 overflow-hidden rounded-xl lg:h-[500px] lg:grid-cols-2">
          <div className="relative min-h-[280px] overflow-hidden lg:min-h-full">
            <img src={galleryMain} alt={detail.name} className="h-full w-full object-cover" />
            <div className="absolute inset-0 bg-black/10" />
          </div>
          <div className="grid gap-4 sm:grid-cols-2">
            {galleryImages.map((image, index) => (
              <div key={image} className="relative min-h-40 overflow-hidden">
                <img
                  src={image}
                  alt={`${detail.name} ${index + 1}`}
                  className="h-full w-full object-cover"
                />
                {index === galleryImages.length - 1 ? (
                  <button className="absolute inset-0 flex items-center justify-center bg-black/50 text-base font-bold text-white">
                    Xem tất cả ảnh
                  </button>
                ) : null}
              </div>
            ))}
          </div>
        </section>

        <section className="mt-10 grid gap-8 lg:grid-cols-[1fr_390px]">
          <div className="space-y-8">
            <InfoSection title="Chi tiết sân">
              <p className="text-lg leading-8 text-[#3d4a3d]">
                {detail.description ||
                  'Sân thể thao được thiết kế tối ưu cho các trận đấu phong trào và bán chuyên, với mặt sân chất lượng cao, khu vực chờ rộng rãi và hệ thống hỗ trợ đặt lịch nhanh.'}
              </p>
            </InfoSection>

            <InfoSection title="Tiện ích cơ sở">
              <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                {facilities.map((facility) => (
                  <div
                    key={facility.title}
                    className="flex items-center gap-3 rounded-xl border border-[#bccbb9] bg-[#f2f4f6] p-4"
                  >
                    <div className="flex h-10 w-10 items-center justify-center rounded-full bg-[#22c55e]/20 font-bold text-[#006e2f]">
                      {facility.icon}
                    </div>
                    <div>
                      <p className="font-bold">{facility.title}</p>
                      <p className="text-xs text-[#3d4a3d]">{facility.description}</p>
                    </div>
                  </div>
                ))}
              </div>
            </InfoSection>

            <InfoSection title="Vị trí sân">
              <div className="overflow-hidden rounded-xl border border-[#bccbb9] bg-white shadow-sm">
                <img src={mapImage} alt="Bản đồ vị trí sân" className="h-80 w-full object-cover" />
              </div>
            </InfoSection>
          </div>

          <aside className="h-fit rounded-xl bg-white p-6 shadow-[0_20px_25px_-5px_rgba(0,0,0,0.1),0_8px_10px_-6px_rgba(0,0,0,0.1)] lg:sticky lg:top-24">
            <div className="flex items-start justify-between">
              <div>
                <p className="text-xs font-bold uppercase tracking-[0.15em] text-[#3d4a3d]">
                  Giá từ
                </p>
                <div className="mt-1 flex items-baseline gap-1">
                  <span className="text-3xl font-bold text-[#006e2f]">
                    {formatCurrency(minPrice)}
                  </span>
                  <span className="text-sm text-[#3d4a3d]">VND/giờ</span>
                </div>
              </div>
              <span className="rounded-full bg-[#d5e0f8] px-3 py-1 text-xs font-bold text-[#586377]">
                {detail.status === 1 ? 'Đang mở' : 'Tạm đóng'}
              </span>
            </div>

            <div className="mt-6">
              <label className="text-sm font-bold text-[#191c1e]" htmlFor="booking-date">
                Ngày đặt sân
              </label>
              <input
                id="booking-date"
                type="date"
                min={getToday()}
                value={selectedDate}
                onChange={(event) => handleDateChange(event.target.value)}
                className="mt-2 h-12 w-full rounded-lg bg-[#f7f9fb] px-4 outline-none ring-1 ring-[#bccbb9] focus:ring-2 focus:ring-[#006e2f]/30"
              />
            </div>

            <div className="mt-6 space-y-3">
              <div className="flex items-center justify-between">
                <p className="text-sm font-bold text-[#191c1e]">Khung giờ trống</p>
                {isLoadingSchedules ? (
                  <span className="text-xs text-[#3d4a3d]">Đang tải...</span>
                ) : null}
              </div>
              {schedules.length > 0 ? (
                schedules.map((schedule) => (
                  <button
                    key={schedule.scheduleId}
                    type="button"
                    onClick={() => setSelectedScheduleId(schedule.scheduleId)}
                    className={[
                      'flex w-full items-center justify-between rounded-lg p-3 text-left transition',
                      selectedScheduleId === schedule.scheduleId
                        ? 'bg-[#006e2f] text-white'
                        : 'bg-[#f7f9fb] text-[#191c1e] hover:bg-[#eceef0]',
                    ].join(' ')}
                  >
                    <span className="text-sm font-semibold">
                      {formatTime(schedule.startTime)} - {formatTime(schedule.endTime)}
                    </span>
                    <span className="text-sm font-bold">
                      {formatCurrency(schedule.price)}
                    </span>
                  </button>
                ))
              ) : (
                <div className="rounded-lg border border-dashed border-[#bccbb9] bg-[#f7f9fb] p-4 text-sm text-[#545f73]">
                  Không có lịch trống cho ngày đã chọn.
                </div>
              )}
            </div>

            <button
              type="button"
              onClick={handleBookNow}
              disabled={!selectedScheduleId || detail.status !== 1}
              className="mt-6 w-full rounded-lg bg-[#006e2f] py-4 text-lg font-bold text-white shadow-[0_10px_15px_-3px_rgba(0,110,47,0.2)] transition hover:bg-[#005321] disabled:cursor-not-allowed disabled:bg-[#bccbb9]"
            >
              Đặt sân ngay
            </button>
            <Link
              to={`/courts/${courtId ?? detail.id}/availability`}
              className="mt-3 flex w-full items-center justify-center rounded-lg border border-[#006e2f] py-3 text-sm font-bold text-[#006e2f] transition hover:bg-[#006e2f]/10"
            >
              Xem lịch trống theo ngày
            </Link>
            <p className="mt-4 text-center text-xs text-[#3d4a3d]">
              Chưa thanh toán ở bước này. Bạn sẽ xác nhận chi tiết ở màn hình tiếp theo.
            </p>
          </aside>
        </section>
      </div>
    </main>
  )
}

function InfoSection({
  title,
  children,
}: {
  title: string
  children: React.ReactNode
}) {
  return (
    <section className="space-y-4">
      <h2 className="text-2xl font-semibold text-[#191c1e]">{title}</h2>
      {children}
    </section>
  )
}
