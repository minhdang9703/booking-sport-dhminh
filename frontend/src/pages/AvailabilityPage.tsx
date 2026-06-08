import { type FormEvent, useEffect, useMemo, useState } from 'react'
import { Link, useParams } from 'react-router-dom'

import fieldCardImage from '../assets/availability/field-card.png'
import { getAuthSession } from '../lib/authApi'
import { subscribeToCourtBookings, type BookingRealtimeEvent } from '../lib/bookingRealtime'
import { createBooking, type BookingResponse, type PaymentType } from '../lib/bookingsApi'
import {
  getAvailableSchedules,
  getCourtById,
  getScheduleKey,
  type AvailableSchedule,
  type Court,
} from '../lib/courtsApi'

const weekDayLabels = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN']
const slotMinutes = 30
const dayStartMinutes = 5 * 60
const dayEndMinutes = 24 * 60 + 2 * 60

type SlotCell = AvailableSchedule & {
  slotKey: string
}

type CheckoutModalState = 'closed' | 'checkout' | 'success'

const paymentMethods = [
  { id: 3 as PaymentType, label: 'Ví điện tử', icon: '◇' },
  { id: 2 as PaymentType, label: 'Chuyển khoản', icon: '▣' },
  { id: 1 as PaymentType, label: 'Tại sân', icon: '●' },
]

function toDateInputValue(date: Date) {
  const year = date.getFullYear()
  const month = `${date.getMonth() + 1}`.padStart(2, '0')
  const day = `${date.getDate()}`.padStart(2, '0')

  return `${year}-${month}-${day}`
}

function getToday() {
  return toDateInputValue(new Date())
}

function getWeekStart(date: Date) {
  const mondayBasedDay = (date.getDay() + 6) % 7
  const start = new Date(date)
  start.setDate(date.getDate() - mondayBasedDay)
  start.setHours(0, 0, 0, 0)

  return start
}

function addDays(date: Date, days: number) {
  const nextDate = new Date(date)
  nextDate.setDate(date.getDate() + days)

  return nextDate
}

function addMinutes(value: string, minutes: number) {
  const total = timeToMinutes(value) + minutes
  const hour = Math.floor(total / 60) % 24
  const minute = total % 60

  return `${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}:00`
}

function timeToMinutes(value: string) {
  const [hour, minute] = value.split(':').map(Number)

  return hour * 60 + minute
}

function formatTime(value: string) {
  return value.slice(0, 5)
}

function formatMonthLabel(date: Date) {
  return new Intl.DateTimeFormat('vi-VN', {
    month: 'long',
    year: 'numeric',
  }).format(date)
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('vi-VN').format(value)
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('vi-VN', {
    weekday: 'long',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(value))
}

function calculateTotalPrice(schedule: AvailableSchedule) {
  const start = timeToMinutes(schedule.startTime)
  let end = timeToMinutes(schedule.endTime)

  if (end <= start) {
    end += 24 * 60
  }

  return Math.max((end - start) / 60, 0) * schedule.hourlyPrice
}

function buildTimeRows() {
  const rows: string[] = []

  for (let minutes = dayStartMinutes; minutes <= dayEndMinutes - slotMinutes; minutes += slotMinutes) {
    const hour = Math.floor(minutes / 60) % 24
    const minute = minutes % 60
    rows.push(`${hour.toString().padStart(2, '0')}:${minute.toString().padStart(2, '0')}`)
  }

  return rows
}

function isRangeCoveringSlot(schedule: AvailableSchedule, rowTime: string) {
  const slotStart = timeToMinutes(`${rowTime}:00`)
  const slotEnd = slotStart + slotMinutes
  const start = timeToMinutes(schedule.startTime)
  const end = timeToMinutes(schedule.endTime)

  return start <= slotStart && end >= slotEnd
}

function isBlockingBookingEvent(event: BookingRealtimeEvent) {
  return event.status === 1 || event.status === 2
}

function isSameSlot(slot: SlotCell, event: BookingRealtimeEvent) {
  return (
    slot.courtId === event.courtId &&
    slot.date === event.bookingDate &&
    timeToMinutes(slot.startTime) < timeToMinutes(event.endTime) &&
    timeToMinutes(slot.endTime) > timeToMinutes(event.startTime)
  )
}

function buildSlotCell(schedule: AvailableSchedule, rowTime: string): SlotCell {
  const startTime = `${rowTime}:00`
  const endTime = addMinutes(startTime, slotMinutes)
  const slot: AvailableSchedule = {
    ...schedule,
    startTime,
    endTime,
  }

  return {
    ...slot,
    slotKey: getScheduleKey(slot),
  }
}

export function AvailabilityPage() {
  const { courtId } = useParams()
  const authSession = useMemo(() => getAuthSession(), [])
  const [court, setCourt] = useState<Court | null>(null)
  const [weekStart, setWeekStart] = useState(() => getWeekStart(new Date()))
  const [schedulesByDate, setSchedulesByDate] = useState<Record<string, AvailableSchedule[]>>({})
  const [selectedSlot, setSelectedSlot] = useState<SlotCell | null>(null)
  const [checkoutModalState, setCheckoutModalState] = useState<CheckoutModalState>('closed')
  const [fullName, setFullName] = useState(authSession?.user.fullName ?? '')
  const [phoneNumber, setPhoneNumber] = useState(authSession?.user.phoneNumber ?? '')
  const [note, setNote] = useState('')
  const [paymentMethod, setPaymentMethod] = useState<PaymentType>(paymentMethods[0].id)
  const [createdBooking, setCreatedBooking] = useState<BookingResponse | null>(null)
  const [checkoutError, setCheckoutError] = useState('')
  const [isSubmittingBooking, setIsSubmittingBooking] = useState(false)
  const [isLoadingCourt, setIsLoadingCourt] = useState(true)
  const [isLoadingSchedules, setIsLoadingSchedules] = useState(true)
  const [error, setError] = useState('')
  const [availabilityRefreshKey, setAvailabilityRefreshKey] = useState(0)

  const weekDays = useMemo(
    () => Array.from({ length: 7 }, (_, index) => addDays(weekStart, index)),
    [weekStart],
  )
  const timeRows = useMemo(() => buildTimeRows(), [])
  const weekEnd = weekDays[6]

  useEffect(() => {
    let isMounted = true

    if (!courtId) {
      setError('Không tìm thấy sân hợp lệ.')
      setIsLoadingCourt(false)
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
          setCourt(null)
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

    if (!courtId || !court) {
      setSchedulesByDate({})
      setIsLoadingSchedules(false)
      return
    }

    setIsLoadingSchedules(true)
    setSelectedSlot(null)

    Promise.all(
      weekDays.map(async (date) => {
        const dateKey = toDateInputValue(date)
        const schedules = await getAvailableSchedules(courtId, dateKey)

        return [dateKey, schedules] as const
      }),
    )
      .then((entries) => {
        if (isMounted) {
          setSchedulesByDate(Object.fromEntries(entries))
          setError('')
        }
      })
      .catch((err: unknown) => {
        if (isMounted) {
          setSchedulesByDate({})
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
  }, [availabilityRefreshKey, court, courtId, weekDays])

  useEffect(() => {
    if (!courtId) {
      return undefined
    }

    return subscribeToCourtBookings(courtId, {
      onAvailabilityChanged: (event) => {
        if (
          event.courtId !== courtId ||
          event.userId === authSession?.user.id ||
          !isBlockingBookingEvent(event)
        ) {
          return
        }

        setAvailabilityRefreshKey((current) => current + 1)
        setSelectedSlot((current) => {
          if (!current || !isSameSlot(current, event)) {
            return current
          }

          setCheckoutModalState('closed')
          setCheckoutError('Khung giờ này vừa có người khác đặt. Vui lòng chọn khung giờ khác.')
          return null
        })
      },
      onError: () => {
        setError('Không thể kết nối realtime booking. Lịch vẫn có thể được tải lại thủ công.')
      },
    })
  }, [authSession?.user.id, courtId])

  function getCellSlot(dateKey: string, rowTime: string) {
    const schedules = schedulesByDate[dateKey] ?? []
    const schedule = schedules.find((item) => isRangeCoveringSlot(item, rowTime))

    return schedule ? buildSlotCell(schedule, rowTime) : null
  }

  function changeWeek(direction: -1 | 1) {
    setWeekStart((current) => addDays(current, direction * 7))
  }

  function goToday() {
    setWeekStart(getWeekStart(new Date()))
  }

  function continueToCheckout() {
    if (!selectedSlot || !court) {
      return
    }

    setCheckoutError('')
    setCreatedBooking(null)
    setCheckoutModalState('checkout')
  }

  function closeCheckoutModal() {
    if (isSubmittingBooking) {
      return
    }

    setCheckoutModalState('closed')
    setCheckoutError('')
  }

  async function submitCheckout(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()

    if (!selectedSlot) {
      setCheckoutError('Vui lòng chọn khung giờ trước khi đặt sân.')
      return
    }

    if (!authSession) {
      setCheckoutError('Bạn cần đăng nhập trước khi đặt sân.')
      return
    }

    if (!fullName.trim() || !phoneNumber.trim()) {
      setCheckoutError('Vui lòng nhập họ tên và số điện thoại.')
      return
    }

    setCheckoutError('')
    setIsSubmittingBooking(true)

    try {
      const booking = await createBooking({
        courtId: selectedSlot.courtId,
        bookingDate: selectedSlot.date,
        startTime: selectedSlot.startTime,
        endTime: selectedSlot.endTime,
        paymentType: paymentMethod,
        note: note.trim() || undefined,
      })

      setCreatedBooking(booking)
      setCheckoutModalState('success')
      setSelectedSlot(null)
      setNote('')
      setAvailabilityRefreshKey((current) => current + 1)
    } catch (err) {
      setCheckoutError(err instanceof Error ? err.message : 'Đặt sân thất bại.')
    } finally {
      setIsSubmittingBooking(false)
    }
  }

  if (isLoadingCourt) {
    return (
      <main className="min-h-screen bg-[#f7f9fb] px-4 pb-16 pt-24 sm:px-6">
        <div className="mx-auto max-w-[1280px]">
          <div className="h-[640px] animate-pulse rounded-xl bg-white" />
        </div>
      </main>
    )
  }

  if (!court) {
    return (
      <main className="min-h-screen bg-[#f7f9fb] px-4 pb-16 pt-24 text-[#191c1e] sm:px-6">
        <div className="mx-auto max-w-2xl rounded-xl bg-white p-8 text-center shadow-sm">
          <h1 className="text-2xl font-bold">Không thể mở lịch đặt sân</h1>
          <p className="mt-3 text-[#3d4a3d]">
            {error || 'Sân không tồn tại hoặc dữ liệu chưa sẵn sàng.'}
          </p>
          <Link
            to="/courts"
            className="mt-6 inline-flex rounded-lg bg-[#006e2f] px-5 py-3 font-bold text-white"
          >
            Quay lại danh sách sân
          </Link>
        </div>
      </main>
    )
  }

  return (
    <main className="min-h-screen bg-[#f7f9fb] text-[#191c1e]">
      <div className="mx-auto max-w-[1280px] px-4 pb-12 pt-24 sm:px-6">
        <div className="flex flex-col gap-8 lg:flex-row lg:items-start">
          <aside className="w-full shrink-0 space-y-6 lg:w-72">
            <section className="overflow-hidden rounded-xl border border-[#bccbb9] bg-white shadow-sm">
              <img
                src={fieldCardImage}
                alt={court.name}
                className="h-32 w-full object-cover"
              />
              <div className="space-y-1 p-3">
                <h1 className="text-base font-bold">{court.name}</h1>
                <p className="text-xs text-[#3d4a3d]">{court.courtType}</p>
              </div>
            </section>

            <section className="rounded-xl border border-[#bccbb9] bg-white p-4 shadow-sm">
              <h2 className="text-base font-bold uppercase tracking-[0.05em] text-[#3d4a3d]">
                Chú giải
              </h2>
              <div className="mt-4 space-y-3 text-base">
                <LegendItem markerClassName="border border-[#bccbb9] bg-white" label="Còn trống" />
                <LegendItem markerClassName="h-1 rounded bg-[#006e2f]/20 ring-1 ring-[#006e2f]/30" label={court.courtType || 'Sân'} />
                <LegendItem markerClassName="bg-[#22c55e] ring-1 ring-[#006e2f]" label="Đang chọn" />
              </div>
            </section>

            <button
              type="button"
              onClick={continueToCheckout}
              disabled={!selectedSlot}
              className="flex w-full items-center justify-center gap-2 rounded-lg bg-[#006e2f] py-3 text-base font-bold text-white shadow-[0_4px_6px_-1px_rgba(0,0,0,0.1)] transition hover:bg-[#005321] disabled:cursor-not-allowed disabled:bg-[#bccbb9]"
            >
              Tiếp tục <span aria-hidden="true">→</span>
            </button>

            {selectedSlot ? (
              <section className="rounded-xl border border-[#bccbb9] bg-white p-4 text-sm shadow-sm">
                <p className="font-bold text-[#006e2f]">Đang chọn</p>
                <p className="mt-2">
                  {formatTime(selectedSlot.startTime)} - {formatTime(selectedSlot.endTime)}
                </p>
                <p className="mt-1 text-[#3d4a3d]">
                  {selectedSlot.date} · {formatCurrency(selectedSlot.hourlyPrice)} VND/giờ
                </p>
              </section>
            ) : null}
          </aside>

          <section className="min-w-0 flex-1">
            <div className="mb-4 rounded-xl border border-[#bccbb9] bg-white p-3 shadow-sm">
              <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
                <div className="flex flex-wrap items-center gap-4">
                  <h2 className="text-base font-bold capitalize">
                    {formatMonthLabel(weekStart)}
                  </h2>
                  <div className="rounded-lg bg-[#eceef0] p-1">
                    <button className="rounded-md bg-white px-4 py-1 text-base font-bold shadow-sm">
                      Tuần
                    </button>
                    <button className="px-4 py-1 text-base font-medium text-[#3d4a3d]">
                      Tháng
                    </button>
                  </div>
                  <p className="text-sm text-[#3d4a3d]">
                    {toDateInputValue(weekStart)} - {toDateInputValue(weekEnd)}
                  </p>
                </div>

                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={() => changeWeek(-1)}
                    className="flex h-10 w-10 items-center justify-center rounded-lg border border-[#bccbb9] text-xl font-bold"
                    aria-label="Tuần trước"
                  >
                    ‹
                  </button>
                  <button
                    type="button"
                    onClick={goToday}
                    className="h-10 rounded-lg border border-[#bccbb9] px-4 text-base font-bold"
                  >
                    Hôm nay
                  </button>
                  <button
                    type="button"
                    onClick={() => changeWeek(1)}
                    className="flex h-10 w-10 items-center justify-center rounded-lg border border-[#bccbb9] text-xl font-bold"
                    aria-label="Tuần sau"
                  >
                    ›
                  </button>
                </div>
              </div>
            </div>

            {error ? (
              <div className="mb-4 rounded-xl border border-[#ffdad6] bg-[#ffdad6] px-4 py-3 text-sm font-medium text-[#93000a]">
                {error}
              </div>
            ) : null}

            <div className="overflow-x-auto rounded-xl bg-white shadow-sm">
              <div className="min-w-[880px]">
                <div className="grid grid-cols-[60px_repeat(7,minmax(0,1fr))] border-l-2 border-t-2 border-[#6d7b6c]">
                  <div className="h-12 border-b-2 border-r border-[#6d7b6c] bg-[#f2f4f6]" />
                  {weekDays.map((date, index) => {
                    const dateKey = toDateInputValue(date)
                    const isToday = dateKey === getToday()

                    return (
                      <div
                        key={dateKey}
                        className="flex h-12 flex-col items-center justify-center border-b-2 border-r border-[#6d7b6c] bg-[#f2f4f6]"
                      >
                        <span className="text-[10px] font-bold uppercase text-[#3d4a3d]">
                          {weekDayLabels[index]}
                        </span>
                        <span
                          className={[
                            'text-base font-bold',
                            isToday ? 'text-[#ba1a1a]' : 'text-[#191c1e]',
                          ].join(' ')}
                        >
                          {date.getDate().toString().padStart(2, '0')}
                        </span>
                      </div>
                    )
                  })}

                  {timeRows.map((rowTime) => (
                    <TimeRow
                      key={rowTime}
                      rowTime={rowTime}
                      weekDays={weekDays}
                      selectedSlot={selectedSlot}
                      isLoading={isLoadingSchedules}
                      getCellSlot={getCellSlot}
                      onSelect={setSelectedSlot}
                    />
                  ))}
                </div>
              </div>
            </div>
          </section>
        </div>
      </div>
      {checkoutModalState === 'checkout' && selectedSlot ? (
        <CheckoutModal
          court={court}
          schedule={selectedSlot}
          fullName={fullName}
          phoneNumber={phoneNumber}
          note={note}
          paymentMethod={paymentMethod}
          error={checkoutError}
          isSubmitting={isSubmittingBooking}
          onClose={closeCheckoutModal}
          onFullNameChange={setFullName}
          onPhoneNumberChange={setPhoneNumber}
          onNoteChange={setNote}
          onPaymentMethodChange={setPaymentMethod}
          onSubmit={submitCheckout}
        />
      ) : null}
      {checkoutModalState === 'success' && createdBooking ? (
        <SuccessModal
          booking={createdBooking}
          onClose={closeCheckoutModal}
        />
      ) : null}
    </main>
  )
}

function CheckoutModal({
  court,
  schedule,
  fullName,
  phoneNumber,
  note,
  paymentMethod,
  error,
  isSubmitting,
  onClose,
  onFullNameChange,
  onPhoneNumberChange,
  onNoteChange,
  onPaymentMethodChange,
  onSubmit,
}: {
  court: Court
  schedule: AvailableSchedule
  fullName: string
  phoneNumber: string
  note: string
  paymentMethod: PaymentType
  error: string
  isSubmitting: boolean
  onClose: () => void
  onFullNameChange: (value: string) => void
  onPhoneNumberChange: (value: string) => void
  onNoteChange: (value: string) => void
  onPaymentMethodChange: (value: PaymentType) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
}) {
  const serviceFee = 0
  const totalPrice = calculateTotalPrice(schedule) + serviceFee

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-center overflow-y-auto bg-black/45 px-4 py-8 backdrop-blur-sm">
      <div className="w-full max-w-[1152px] overflow-hidden rounded-xl bg-white shadow-[0_20px_40px_rgba(0,0,0,0.22)]">
        <div className="flex h-16 items-center justify-between border-b border-[#e0e3e5] px-6">
          <h2 className="text-xl font-bold text-[#191c1e]">Đặt sân</h2>
          <button
            type="button"
            onClick={onClose}
            disabled={isSubmitting}
            className="flex h-8 w-8 items-center justify-center rounded-lg text-xl font-bold text-[#3d4a3d] transition hover:bg-[#eceef0] disabled:cursor-not-allowed"
            aria-label="Đóng modal đặt sân"
          >
            x
          </button>
        </div>

        <form className="px-6 pb-6 pt-6" onSubmit={onSubmit}>
          <ModalStepper />

          <div className="mt-8 grid gap-8 lg:grid-cols-[1fr_441px]">
            <div className="space-y-6">
              <section className="rounded-xl border border-[#e0e3e5] bg-white p-6">
                <h3 className="flex items-center gap-2 text-xl font-bold">
                  <span aria-hidden="true">▦</span>
                  Thông tin đặt sân
                </h3>
                <div className="mt-6 grid gap-5 sm:grid-cols-2">
                  <ReadonlyField label="Ngày đặt" value={formatDate(schedule.date)} />
                  <ReadonlyField label="Loại sân" value={court.courtType || 'Sân'} />
                  <ReadonlyField label="Giờ bắt đầu" value={formatTime(schedule.startTime)} />
                  <ReadonlyField label="Giờ kết thúc" value={formatTime(schedule.endTime)} />
                </div>
              </section>

              <section className="rounded-xl border border-[#e0e3e5] bg-white p-6">
                <h3 className="flex items-center gap-2 text-xl font-bold">
                  <span aria-hidden="true">◎</span>
                  Thông tin người đặt
                </h3>
                <div className="mt-6 grid gap-5 sm:grid-cols-2">
                  <FormInput
                    label="Họ và tên"
                    value={fullName}
                    onChange={onFullNameChange}
                    placeholder="Nguyễn Văn A"
                    required
                  />
                  <FormInput
                    label="Số điện thoại"
                    value={phoneNumber}
                    onChange={onPhoneNumberChange}
                    placeholder="0901 234 567"
                    required
                  />
                  <label className="block sm:col-span-2">
                    <span className="text-base font-semibold text-[#191c1e]">
                      Ghi chú (Tùy chọn)
                    </span>
                    <textarea
                      value={note}
                      onChange={(event) => onNoteChange(event.target.value)}
                      placeholder="Yêu cầu thêm về dụng cụ hoặc dịch vụ..."
                      className="mt-2 min-h-24 w-full resize-none rounded-lg border border-[#bccbb9] bg-white px-4 py-3 outline-none transition focus:border-[#006e2f] focus:ring-2 focus:ring-[#006e2f]/15"
                    />
                  </label>
                </div>
              </section>
            </div>

            <aside className="h-fit overflow-hidden rounded-xl border border-[#bccbb9] bg-white shadow-[0_10px_15px_-3px_rgba(0,0,0,0.1)]">
              <div className="border-b border-[#e0e3e5] bg-[#f7f9fb] px-4 py-4">
                <h3 className="text-lg font-bold">Tổng thanh toán</h3>
              </div>
              <div className="space-y-6 p-6">
                <div className="space-y-4">
                  <SummaryRow label="Sân" value={court.name} />
                  <SummaryRow label="Ngày" value={formatDate(schedule.date)} />
                  <SummaryRow
                    label="Khung giờ"
                    value={`${formatTime(schedule.startTime)} - ${formatTime(schedule.endTime)}`}
                  />
                  <SummaryRow
                    label="Giá theo giờ"
                    value={`${formatCurrency(schedule.hourlyPrice)} VND`}
                  />
                </div>

                <div className="space-y-3 border-y border-[#e0e3e5] py-5">
                  <p className="text-base font-bold">Phương thức thanh toán</p>
                  <div className="grid gap-3">
                    {paymentMethods.map((method) => {
                      const isSelected = paymentMethod === method.id

                      return (
                        <button
                          key={method.id}
                          type="button"
                          onClick={() => onPaymentMethodChange(method.id)}
                          className={[
                            'flex items-center justify-between rounded-lg border px-4 py-3 text-left transition',
                            isSelected
                              ? 'border-[#006e2f] bg-[#e9f9ef] text-[#005321]'
                              : 'border-[#bccbb9] bg-white hover:border-[#006e2f]',
                          ].join(' ')}
                        >
                          <span className="flex items-center gap-3 font-bold">
                            <span aria-hidden="true">{method.icon}</span>
                            {method.label}
                          </span>
                          <span
                            className={[
                              'h-4 w-4 rounded-full border',
                              isSelected ? 'border-[#006e2f] bg-[#006e2f]' : 'border-[#bccbb9]',
                            ].join(' ')}
                          />
                        </button>
                      )
                    })}
                  </div>
                </div>

                <div className="space-y-4">
                  <SummaryRow label="Phí dịch vụ" value={`${formatCurrency(serviceFee)} VND`} />
                  <div className="flex items-end justify-between gap-4 border-t border-[#e0e3e5] pt-4">
                    <span className="text-base font-bold text-[#3d4a3d]">Tổng cộng</span>
                    <span className="text-2xl font-bold text-[#006e2f]">
                      {formatCurrency(totalPrice)} VND
                    </span>
                  </div>
                </div>

                {error ? (
                  <div className="rounded-lg border border-[#ffdad6] bg-[#ffdad6] px-4 py-3 text-sm font-medium text-[#93000a]">
                    {error}
                  </div>
                ) : null}

                <button
                  type="submit"
                  disabled={isSubmitting}
                  className="flex h-14 w-full items-center justify-center gap-2 rounded-lg bg-[#006e2f] text-base font-bold text-white shadow-[0_4px_6px_-1px_rgba(0,0,0,0.1)] transition hover:bg-[#005321] disabled:cursor-not-allowed disabled:bg-[#bccbb9]"
                >
                  {isSubmitting ? 'Đang đặt sân...' : 'Xác nhận đặt sân'}
                  <span aria-hidden="true">→</span>
                </button>

                <p className="text-center text-xs leading-5 text-[#3d4a3d]">
                  Booking sẽ được tạo ở trạng thái chờ xác nhận.
                </p>
              </div>
            </aside>
          </div>
        </form>
      </div>
    </div>
  )
}

function SuccessModal({
  booking,
  onClose,
}: {
  booking: BookingResponse
  onClose: () => void
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/45 px-4 py-8 backdrop-blur-sm">
      <div className="w-full max-w-[560px] rounded-xl bg-white p-8 text-center shadow-[0_20px_40px_rgba(0,0,0,0.22)]">
        <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-full bg-[#e9f9ef] text-3xl font-bold text-[#006e2f]">
          ✓
        </div>
        <h2 className="mt-5 text-2xl font-bold text-[#191c1e]">
          Đặt sân thành công
        </h2>
        <p className="mt-2 text-sm leading-6 text-[#3d4a3d]">
          Booking của bạn đã được ghi nhận và đang chờ xác nhận từ sân.
        </p>

        <div className="mt-6 rounded-xl border border-[#e0e3e5] bg-[#f7f9fb] p-5 text-left">
          <SummaryRow label="Sân" value={booking.courtName} />
          <div className="mt-4">
            <SummaryRow label="Ngày" value={formatDate(booking.bookingDate)} />
          </div>
          <div className="mt-4">
            <SummaryRow
              label="Khung giờ"
              value={`${formatTime(booking.startTime)} - ${formatTime(booking.endTime)}`}
            />
          </div>
          <div className="mt-4">
            <SummaryRow
              label="Tổng tiền"
              value={`${formatCurrency(booking.totalPrice)} VND`}
            />
          </div>
        </div>

        <div className="mt-6 flex flex-col gap-3 sm:flex-row">
          <button
            type="button"
            onClick={onClose}
            className="h-12 flex-1 rounded-lg border border-[#bccbb9] font-bold text-[#191c1e] transition hover:bg-[#eceef0]"
          >
            Đóng
          </button>
          <Link
            to="/bookings"
            className="flex h-12 flex-1 items-center justify-center rounded-lg bg-[#006e2f] font-bold text-white transition hover:bg-[#005321]"
          >
            Xem lịch sử
          </Link>
        </div>
      </div>
    </div>
  )
}

function ModalStepper() {
  const steps = ['Chọn giờ', 'Thông tin', 'Hoàn tất']

  return (
    <div className="mx-auto flex max-w-[672px] items-center justify-between">
      {steps.map((step, index) => (
        <div key={step} className="flex flex-1 items-start last:flex-none">
          <div className="flex flex-col items-center gap-2">
            <span
              className={[
                'flex h-10 w-10 items-center justify-center rounded-full text-sm font-bold',
                index < 2 ? 'bg-[#006e2f] text-white' : 'bg-[#e0e3e5] text-[#3d4a3d]',
              ].join(' ')}
            >
              {index + 1}
            </span>
            <span className="text-sm font-bold text-[#006e2f]">{step}</span>
          </div>
          {index < steps.length - 1 ? (
            <div
              className={[
                'mx-4 mt-5 h-0.5 flex-1',
                index === 0 ? 'bg-[#006e2f]' : 'bg-[#bccbb9]',
              ].join(' ')}
            />
          ) : null}
        </div>
      ))}
    </div>
  )
}

function ReadonlyField({ label, value }: { label: string; value: string }) {
  return (
    <label className="block">
      <span className="text-base font-semibold text-[#191c1e]">{label}</span>
      <input
        value={value}
        readOnly
        className="mt-2 h-12 w-full rounded-lg border border-[#bccbb9] bg-[#f7f9fb] px-4 text-[#191c1e] outline-none"
      />
    </label>
  )
}

function FormInput({
  label,
  value,
  placeholder,
  required,
  onChange,
}: {
  label: string
  value: string
  placeholder: string
  required?: boolean
  onChange: (value: string) => void
}) {
  return (
    <label className="block">
      <span className="text-base font-semibold text-[#191c1e]">{label}</span>
      <input
        value={value}
        required={required}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
        className="mt-2 h-12 w-full rounded-lg border border-[#bccbb9] bg-white px-4 outline-none transition focus:border-[#006e2f] focus:ring-2 focus:ring-[#006e2f]/15"
      />
    </label>
  )
}

function SummaryRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-start justify-between gap-4 text-sm">
      <span className="text-[#3d4a3d]">{label}</span>
      <span className="text-right font-bold text-[#191c1e]">{value}</span>
    </div>
  )
}

function TimeRow({
  rowTime,
  weekDays,
  selectedSlot,
  isLoading,
  getCellSlot,
  onSelect,
}: {
  rowTime: string
  weekDays: Date[]
  selectedSlot: SlotCell | null
  isLoading: boolean
  getCellSlot: (dateKey: string, rowTime: string) => SlotCell | null
  onSelect: (slot: SlotCell) => void
}) {
  return (
    <>
      <div className="flex h-6 items-center justify-center border-b border-r border-[#6d7b6c] bg-[#f7f9fb] text-[11px] text-[#191c1e]">
        {rowTime}
      </div>
      {weekDays.map((date) => {
        const dateKey = toDateInputValue(date)
        const slot = getCellSlot(dateKey, rowTime)
        const isSelected = slot?.slotKey === selectedSlot?.slotKey

        return (
          <button
            key={`${dateKey}-${rowTime}`}
            type="button"
            disabled={!slot || isLoading}
            onClick={() => slot && onSelect(slot)}
            className={[
              'relative h-6 border-b border-r border-[#bccbb9] text-[10px] transition',
              slot ? 'bg-white hover:bg-[#e9f9ef]' : 'bg-[#f7f9fb]',
              isSelected ? 'bg-[#22c55e] text-white ring-2 ring-inset ring-[#006e2f]' : '',
              isLoading ? 'animate-pulse cursor-wait bg-[#eceef0]' : '',
            ].join(' ')}
            title={
              slot
                ? `${dateKey} ${formatTime(slot.startTime)} - ${formatTime(slot.endTime)}`
                : undefined
            }
          >
            {isSelected ? <span className="font-bold">✓</span> : null}
          </button>
        )
      })}
    </>
  )
}

function LegendItem({
  markerClassName,
  label,
}: {
  markerClassName: string
  label: string
}) {
  return (
    <div className="flex items-center gap-3">
      <span className={`block h-4 w-4 rounded ${markerClassName}`} />
      <span>{label}</span>
    </div>
  )
}
