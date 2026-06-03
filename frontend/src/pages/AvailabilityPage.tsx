import { useEffect, useMemo, useState } from 'react'
import { Link, useNavigate, useParams } from 'react-router-dom'

import fieldCardImage from '../assets/availability/field-card.png'
import {
  getAvailableSchedules,
  getCourtById,
  type AvailableSchedule,
  type Court,
} from '../lib/courtsApi'

const dayLabels = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN']

const demoCourt: Court = {
  id: 'football-demo',
  venueId: 'demo',
  venueName: 'Quận 10, TP. Hồ Chí Minh',
  sportId: 'demo',
  sportName: 'Sân bóng đá',
  name: 'Sân vận động Thống Nhất',
  status: 1,
  description: 'Sân cỏ nhân tạo, đèn sáng, phù hợp đá 7 người.',
  createdAt: new Date().toISOString(),
}

function toDateInputValue(date: Date) {
  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')

  return `${year}-${month}-${day}`
}

function parseDateInput(value: string) {
  const [year, month, day] = value.split('-').map(Number)

  return new Date(year, month - 1, day)
}

function getToday() {
  return toDateInputValue(new Date())
}

function getMonthLabel(date: Date) {
  return new Intl.DateTimeFormat('vi-VN', {
    month: 'long',
    year: 'numeric',
  }).format(date)
}

function getStartOfCalendar(monthDate: Date) {
  const firstDay = new Date(monthDate.getFullYear(), monthDate.getMonth(), 1)
  const mondayBasedDay = (firstDay.getDay() + 6) % 7

  firstDay.setDate(firstDay.getDate() - mondayBasedDay)

  return firstDay
}

function formatTime(value: string) {
  return value.slice(0, 5)
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('vi-VN').format(value)
}

function makeDemoSchedules(date: string, court: Court): AvailableSchedule[] {
  return [
    ['06:00:00', '07:00:00', 300000],
    ['07:00:00', '08:00:00', 300000],
    ['17:00:00', '18:00:00', 420000],
    ['18:00:00', '19:00:00', 450000],
    ['19:00:00', '20:00:00', 450000],
    ['20:00:00', '21:00:00', 420000],
  ].map(([startTime, endTime, price], index) => ({
    scheduleId: `demo-slot-${index}`,
    courtId: court.id,
    courtName: court.name,
    date,
    dayOfWeek: parseDateInput(date).getDay(),
    startTime: String(startTime),
    endTime: String(endTime),
    price: Number(price),
    isAvailable: true,
  }))
}

function getPeriodLabel(startTime: string) {
  const hour = Number(startTime.slice(0, 2))

  if (hour < 12) {
    return 'Buổi sáng'
  }

  if (hour < 18) {
    return 'Buổi chiều'
  }

  return 'Buổi tối'
}

export function AvailabilityPage() {
  const { courtId } = useParams()
  const navigate = useNavigate()
  const [court, setCourt] = useState<Court | null>(null)
  const [selectedDate, setSelectedDate] = useState(getToday())
  const [monthCursor, setMonthCursor] = useState(() => parseDateInput(getToday()))
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
              : 'Không tải được thông tin sân từ API.',
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
    const activeCourt = court ?? demoCourt

    if (!courtId) {
      Promise.resolve().then(() => {
        if (isMounted) {
          setSchedules(makeDemoSchedules(selectedDate, activeCourt))
          setIsLoadingSchedules(false)
        }
      })

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
  }, [court, courtId, selectedDate])

  const selectedSchedule = schedules.find(
    (schedule) => schedule.scheduleId === selectedScheduleId,
  )

  const calendarDays = useMemo(() => {
    const start = getStartOfCalendar(monthCursor)

    return Array.from({ length: 42 }, (_, index) => {
      const date = new Date(start)
      date.setDate(start.getDate() + index)

      return date
    })
  }, [monthCursor])

  const groupedSchedules = useMemo(() => {
    return schedules.reduce<Record<string, AvailableSchedule[]>>((groups, schedule) => {
      const label = getPeriodLabel(schedule.startTime)
      groups[label] = groups[label] ?? []
      groups[label].push(schedule)

      return groups
    }, {})
  }, [schedules])

  function selectDate(date: Date) {
    setSelectedDate(toDateInputValue(date))
    setMonthCursor(new Date(date.getFullYear(), date.getMonth(), 1))
    setSelectedScheduleId('')
    setIsLoadingSchedules(true)
  }

  function changeMonth(direction: -1 | 1) {
    setMonthCursor(
      new Date(monthCursor.getFullYear(), monthCursor.getMonth() + direction, 1),
    )
  }

  function continueToCheckout() {
    if (!selectedSchedule) {
      return
    }

    const checkoutCourt = court ?? demoCourt

    sessionStorage.setItem(
      'bookingSport.checkout',
      JSON.stringify({
        court: checkoutCourt,
        schedule: selectedSchedule,
      }),
    )
    navigate('/checkout')
  }

  const detail = court ?? demoCourt

  return (
    <main className="min-h-screen bg-[#f7f9fb] text-[#191c1e]">
      <div className="mx-auto max-w-[1280px] px-4 pb-16 pt-24 sm:px-6">
        <div className="grid gap-8 lg:grid-cols-[320px_1fr]">
          <aside className="space-y-6">
            <section className="overflow-hidden rounded-xl bg-white shadow-sm">
              <img
                src={fieldCardImage}
                alt={detail.name}
                className="h-40 w-full object-cover"
              />
              <div className="space-y-2 p-4">
                {isLoadingCourt ? (
                  <div className="h-20 animate-pulse rounded-lg bg-[#eceef0]" />
                ) : (
                  <>
                    <h1 className="text-2xl font-bold leading-8">{detail.name}</h1>
                    <p className="text-sm text-[#3d4a3d]">📍 {detail.venueName}</p>
                  </>
                )}
              </div>
            </section>

            <section className="rounded-xl bg-white p-4 shadow-sm">
              <div className="flex items-center justify-between">
                <h2 className="text-sm font-bold capitalize tracking-[0.01em]">
                  {getMonthLabel(monthCursor)}
                </h2>
                <div className="flex gap-2">
                  <button
                    type="button"
                    onClick={() => changeMonth(-1)}
                    className="rounded-lg border border-[#bccbb9] px-2 py-1"
                    aria-label="Tháng trước"
                  >
                    ‹
                  </button>
                  <button
                    type="button"
                    onClick={() => changeMonth(1)}
                    className="rounded-lg border border-[#bccbb9] px-2 py-1"
                    aria-label="Tháng sau"
                  >
                    ›
                  </button>
                </div>
              </div>

              <div className="mt-4 grid grid-cols-7 gap-1 text-center">
                {dayLabels.map((label) => (
                  <div
                    key={label}
                    className="py-2 text-xs font-bold uppercase tracking-[0.05em] text-[#3d4a3d]"
                  >
                    {label}
                  </div>
                ))}
                {calendarDays.map((date) => {
                  const value = toDateInputValue(date)
                  const isCurrentMonth = date.getMonth() === monthCursor.getMonth()
                  const isSelected = value === selectedDate
                  const isToday = value === getToday()

                  return (
                    <button
                      key={value}
                      type="button"
                      onClick={() => selectDate(date)}
                      className={[
                        'h-10 rounded-lg text-xs font-bold transition',
                        isSelected
                          ? 'bg-[#006e2f] text-white'
                          : 'text-[#191c1e] hover:bg-[#eceef0]',
                        !isCurrentMonth ? 'opacity-30' : '',
                        isToday && !isSelected ? 'text-[#ba1a1a]' : '',
                      ].join(' ')}
                    >
                      {date.getDate()}
                    </button>
                  )
                })}
              </div>
            </section>

            <section className="rounded-xl bg-white p-4 shadow-sm">
              <h3 className="text-xs font-bold uppercase tracking-[0.12em] text-[#3d4a3d]">
                Trạng thái
              </h3>
              <div className="mt-4 space-y-3 text-sm">
                <LegendItem color="bg-[#006e2f]" label="Còn trống" />
                <LegendItem
                  color="bg-[#e0e3e5] bg-[linear-gradient(45deg,transparent_25%,rgba(0,0,0,0.08)_25%,rgba(0,0,0,0.08)_50%,transparent_50%)]"
                  label="Đã đặt"
                />
                <LegendItem color="bg-[#ef9900]" label="Đang chọn" />
              </div>
            </section>
          </aside>

          <section className="space-y-6">
            <div className="flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
              <div>
                <p className="text-base font-bold text-[#006e2f]">
                  {new Intl.DateTimeFormat('vi-VN', {
                    weekday: 'long',
                    day: '2-digit',
                    month: '2-digit',
                    year: 'numeric',
                  }).format(parseDateInput(selectedDate))}
                </p>
                <h2 className="mt-1 text-3xl font-bold tracking-[-0.01em]">
                  Lịch trống trong ngày
                </h2>
              </div>
              <div className="rounded-lg bg-[#e6e8ea] p-1">
                <button className="rounded-md bg-white px-4 py-1.5 text-sm font-bold text-[#006e2f] shadow-sm">
                  Theo giờ
                </button>
                <button className="px-4 py-1.5 text-sm font-semibold text-[#3d4a3d]">
                  Theo ca
                </button>
              </div>
            </div>

            {error ? (
              <div className="rounded-xl border border-[#ffdad6] bg-[#ffdad6] px-4 py-3 text-sm font-medium text-[#93000a]">
                {error}. Đang hiển thị dữ liệu mẫu để kiểm tra giao diện.
              </div>
            ) : null}

            <div className="space-y-6">
              {isLoadingSchedules ? (
                <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                  {Array.from({ length: 6 }).map((_, index) => (
                    <div
                      key={index}
                      className="h-24 animate-pulse rounded-xl bg-white shadow-sm"
                    />
                  ))}
                </div>
              ) : schedules.length === 0 ? (
                <div className="rounded-xl bg-white p-10 text-center shadow-sm">
                  <h3 className="text-lg font-bold">Không có lịch trống</h3>
                  <p className="mt-2 text-sm text-[#3d4a3d]">
                    Hãy chọn ngày khác trên lịch để xem các khung giờ còn trống.
                  </p>
                </div>
              ) : (
                ['Buổi sáng', 'Buổi chiều', 'Buổi tối'].map((period) => {
                  const periodSchedules = groupedSchedules[period] ?? []

                  if (periodSchedules.length === 0) {
                    return null
                  }

                  return (
                    <section key={period} className="space-y-3">
                      <div className="flex items-center gap-2 text-sm font-bold uppercase tracking-[0.08em] text-[#3d4a3d]">
                        <span className="h-2.5 w-2.5 rounded-full bg-[#006e2f]" />
                        {period}
                      </div>
                      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
                        {periodSchedules.map((schedule) => {
                          const isSelected =
                            selectedScheduleId === schedule.scheduleId

                          return (
                            <button
                              key={schedule.scheduleId}
                              type="button"
                              onClick={() => setSelectedScheduleId(schedule.scheduleId)}
                              className={[
                                'relative rounded-xl border p-4 text-left transition',
                                isSelected
                                  ? 'border-[#ef9900] bg-[#ef9900]/10 shadow-[0_0_0_4px_rgba(239,153,0,1)]'
                                  : 'border-[#bccbb9] bg-white hover:border-[#006e2f]',
                              ].join(' ')}
                            >
                              <p className="text-sm text-[#3d4a3d]">Khung giờ</p>
                              <p className="mt-1 text-lg font-semibold">
                                {formatTime(schedule.startTime)} -{' '}
                                {formatTime(schedule.endTime)}
                              </p>
                              <p className="mt-2 text-sm font-bold text-[#006e2f]">
                                {formatCurrency(schedule.price)} VND
                              </p>
                              {isSelected ? (
                                <span className="absolute -right-2 -top-2 rounded-full bg-[#855300] px-2 py-1 text-xs font-bold text-white">
                                  ✓
                                </span>
                              ) : null}
                            </button>
                          )
                        })}
                      </div>
                    </section>
                  )
                })
              )}
            </div>

            <div className="sticky bottom-0 rounded-xl border border-[#006e2f]/20 bg-[#22c55e]/10 p-6 shadow-sm backdrop-blur">
              <div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
                <div>
                  <p className="text-sm font-semibold text-[#006e2f]">
                    {selectedSchedule
                      ? `${formatTime(selectedSchedule.startTime)} - ${formatTime(
                          selectedSchedule.endTime,
                        )}`
                      : 'Chưa chọn khung giờ'}
                  </p>
                  <p className="mt-1 text-2xl font-bold text-[#191c1e]">
                    {selectedSchedule
                      ? `${formatCurrency(selectedSchedule.price)} VND`
                      : 'Chọn lịch trống để tiếp tục'}
                  </p>
                </div>
                <button
                  type="button"
                  disabled={!selectedSchedule}
                  onClick={continueToCheckout}
                  className="rounded-lg bg-[#006e2f] px-12 py-4 text-lg font-bold text-white shadow-[0_10px_15px_-3px_rgba(0,0,0,0.1)] transition hover:bg-[#005321] disabled:cursor-not-allowed disabled:bg-[#bccbb9]"
                >
                  Tiếp tục đặt sân
                </button>
              </div>
            </div>

            <Link
              to={`/courts/${courtId ?? demoCourt.id}`}
              className="inline-flex text-sm font-semibold text-[#006e2f] hover:text-[#004b1e]"
            >
              ← Quay lại chi tiết sân
            </Link>
          </section>
        </div>
      </div>
    </main>
  )
}

function LegendItem({ color, label }: { color: string; label: string }) {
  return (
    <div className="flex items-center gap-3">
      <span className={`h-4 w-4 rounded ${color}`} />
      <span>{label}</span>
    </div>
  )
}
