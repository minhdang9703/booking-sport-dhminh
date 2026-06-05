import { type FormEvent, useEffect, useMemo, useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'

import fieldPreviewImage from '../assets/checkout/field-preview.png'
import { getAuthSession } from '../lib/authApi'
import { createBooking, type PaymentType } from '../lib/bookingsApi'
import type { AvailableSchedule, Court } from '../lib/courtsApi'

type CheckoutState = {
  court: Court
  schedule: AvailableSchedule
}

const paymentMethods = [
  { id: 3 as PaymentType, label: 'Ví điện tử', icon: '◇' },
  { id: 2 as PaymentType, label: 'Chuyển khoản', icon: '▣' },
  { id: 1 as PaymentType, label: 'Tại sân', icon: '●' },
]

function readCheckoutState(): CheckoutState | null {
  const raw = sessionStorage.getItem('bookingSport.checkout')

  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as CheckoutState

    return parsed.court?.id &&
      parsed.schedule?.courtId &&
      parsed.schedule?.date &&
      parsed.schedule?.startTime &&
      parsed.schedule?.endTime
      ? parsed
      : null
  } catch {
    return null
  }
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('vi-VN').format(value)
}

function formatTime(value: string) {
  return value.slice(0, 5)
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
  const [startHour, startMinute] = schedule.startTime.split(':').map(Number)
  const [endHour, endMinute] = schedule.endTime.split(':').map(Number)
  const hours = (endHour * 60 + endMinute - (startHour * 60 + startMinute)) / 60

  return Math.max(hours, 0) * schedule.hourlyPrice
}

export function CheckoutPage() {
  const navigate = useNavigate()
  const checkoutState = useMemo(() => readCheckoutState(), [])
  const authSession = useMemo(() => getAuthSession(), [])
  const [fullName, setFullName] = useState(authSession?.user.fullName ?? '')
  const [phoneNumber, setPhoneNumber] = useState(authSession?.user.phoneNumber ?? '')
  const [note, setNote] = useState('')
  const [paymentMethod, setPaymentMethod] = useState<PaymentType>(paymentMethods[0].id)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  useEffect(() => {
    if (!success) {
      return
    }

    const redirectTimer = window.setTimeout(() => {
      navigate('/')
    }, 1800)

    return () => window.clearTimeout(redirectTimer)
  }, [navigate, success])

  if (!checkoutState) {
    sessionStorage.removeItem('bookingSport.checkout')

    return (
      <main className="min-h-screen bg-[#f7f9fb] px-4 pb-16 pt-24 text-[#191c1e]">
        <div className="mx-auto max-w-2xl rounded-xl bg-white p-8 text-center shadow-sm">
          <h1 className="text-2xl font-bold">Chưa có khung giờ hợp lệ</h1>
          <p className="mt-3 text-[#3d4a3d]">
            Hãy quay lại màn lịch trống và chọn một khung giờ từ dữ liệu thực tế trước khi thanh toán.
          </p>
          <Link
            to="/courts"
            className="mt-6 inline-flex rounded-lg bg-[#006e2f] px-5 py-3 font-bold text-white"
          >
            Xem danh sách sân
          </Link>
        </div>
      </main>
    )
  }

  const { court, schedule } = checkoutState
  const serviceFee = 0
  const totalPrice = calculateTotalPrice(schedule) + serviceFee

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setSuccess('')

    if (!authSession) {
      setError('Bạn cần đăng nhập trước khi đặt sân.')
      return
    }

    setIsSubmitting(true)

    try {
      await createBooking({
        courtId: schedule.courtId,
        bookingDate: schedule.date,
        startTime: schedule.startTime,
        endTime: schedule.endTime,
        paymentType: paymentMethod,
        note: note.trim() || undefined,
      })
      sessionStorage.removeItem('bookingSport.checkout')
      setSuccess('Đặt sân thành công. Booking đang ở trạng thái chờ xác nhận.')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Đặt sân thất bại.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="min-h-screen bg-[#f7f9fb] text-[#191c1e]">
      <div className="mx-auto max-w-[1280px] px-4 pb-16 pt-24 sm:px-6">
        <nav className="flex flex-wrap items-center gap-2 text-sm text-[#3d4a3d]">
          <Link to="/courts" className="hover:text-[#006e2f]">
            Sân bãi
          </Link>
          <span>/</span>
          <Link to={`/courts/${court.id}`} className="hover:text-[#006e2f]">
            {court.name}
          </Link>
          <span>/</span>
          <span className="font-bold text-[#006e2f]">Thanh toán</span>
        </nav>

        <div className="mt-8 grid gap-8 lg:grid-cols-[1fr_460px]">
          <section className="space-y-8">
            <ProgressStepper />

            <form className="space-y-8" onSubmit={handleSubmit}>
              <section className="rounded-xl border border-[#bccbb9] bg-white p-6 shadow-sm">
                <h1 className="text-2xl font-semibold">Thông tin người đặt</h1>
                <div className="mt-6 grid gap-5 sm:grid-cols-2">
                  <FormInput
                    label="Họ và tên"
                    value={fullName}
                    onChange={setFullName}
                    placeholder="Nguyễn Văn A"
                    required
                  />
                  <FormInput
                    label="Số điện thoại"
                    value={phoneNumber}
                    onChange={setPhoneNumber}
                    placeholder="0901 234 567"
                    required
                  />
                  <label className="block sm:col-span-2">
                    <span className="text-sm font-semibold text-[#545f73]">
                      Ghi chú (Tùy chọn)
                    </span>
                    <textarea
                      value={note}
                      onChange={(event) => setNote(event.target.value)}
                      placeholder="Yêu cầu thêm về dụng cụ hoặc dịch vụ..."
                      className="mt-2 min-h-32 w-full rounded-lg border border-[#bccbb9] bg-white px-4 py-3 outline-none focus:border-[#006e2f] focus:ring-2 focus:ring-[#006e2f]/15"
                    />
                  </label>
                </div>
              </section>

              <section className="rounded-xl border border-[#bccbb9] bg-white p-6 shadow-sm">
                <h2 className="text-2xl font-semibold">Phương thức thanh toán</h2>
                <div className="mt-6 grid gap-4 sm:grid-cols-3">
                  {paymentMethods.map((method) => {
                    const isSelected = paymentMethod === method.id

                    return (
                      <button
                        key={method.id}
                        type="button"
                        onClick={() => setPaymentMethod(method.id)}
                        className={[
                          'rounded-xl border-2 p-4 text-center transition',
                          isSelected
                            ? 'border-[#006e2f] bg-[#22c55e]/10'
                            : 'border-[#bccbb9] bg-[#f7f9fb] hover:border-[#006e2f]',
                        ].join(' ')}
                      >
                        <div className="text-2xl text-[#006e2f]">{method.icon}</div>
                        <div className="mt-3 text-sm font-bold">{method.label}</div>
                      </button>
                    )
                  })}
                </div>
              </section>

              {error ? (
                <div className="rounded-xl border border-[#ffdad6] bg-[#ffdad6] px-4 py-3 text-sm font-medium text-[#93000a]">
                  {error}
                </div>
              ) : null}
              {success ? (
                <div className="rounded-xl border border-[#d6f5df] bg-[#e9f9ef] px-4 py-3 text-sm font-medium text-[#005321]">
                  {success}
                </div>
              ) : null}

              <button
                type="submit"
                disabled={isSubmitting}
                className="rounded-lg bg-[#ef9900] px-8 py-4 text-lg font-bold text-[#5c3800] shadow-[0_4px_6px_-1px_rgba(0,0,0,0.1)] transition hover:bg-[#ffb95f] disabled:cursor-not-allowed disabled:bg-[#bccbb9]"
              >
                {isSubmitting ? 'Đang đặt sân...' : 'Thanh toán & đặt sân'}
              </button>
            </form>
          </section>

          <aside className="h-fit overflow-hidden rounded-xl border border-[#bccbb9] bg-white shadow-[0_10px_15px_-3px_rgba(0,0,0,0.1)] lg:sticky lg:top-24">
            <div className="relative h-48">
              <img
                src={fieldPreviewImage}
                alt={court.name}
                className="h-full w-full object-cover"
              />
              <span className="absolute bottom-4 left-4 rounded-full bg-[#006e2f] px-3 py-1 text-xs font-bold text-white shadow-sm">
                {court.courtType || 'Sân thể thao'}
              </span>
            </div>

            <div className="space-y-6 p-6">
              <div>
                <h2 className="text-2xl font-bold">{court.name}</h2>
                <p className="mt-2 text-sm text-[#3d4a3d]">{court.courtType}</p>
              </div>

              <div className="space-y-4 border-y border-[#bccbb9] py-5">
                <SummaryRow label="Ngày đặt" value={formatDate(schedule.date)} />
                <SummaryRow
                  label="Khung giờ"
                  value={`${formatTime(schedule.startTime)} - ${formatTime(
                    schedule.endTime,
                  )}`}
                />
                <SummaryRow label="Giá theo giờ" value={`${formatCurrency(schedule.hourlyPrice)} VND`} />
                <SummaryRow label="Phí dịch vụ" value={`${formatCurrency(serviceFee)} VND`} />
              </div>

              <div className="flex items-end justify-between">
                <span className="text-sm font-bold text-[#3d4a3d]">Tổng cộng</span>
                <span className="text-3xl font-bold text-[#006e2f]">
                  {formatCurrency(totalPrice)} VND
                </span>
              </div>

              <div className="grid grid-cols-3 gap-4 border-t border-[#bccbb9] pt-5 text-center text-xs font-bold text-[#3d4a3d]">
                <span>Bảo mật</span>
                <span>Uy tín</span>
                <span>Hỗ trợ 24/7</span>
              </div>
            </div>
          </aside>
        </div>
      </div>
    </main>
  )
}

function ProgressStepper() {
  const steps = ['Chọn giờ', 'Thông tin', 'Hoàn tất']

  return (
    <div className="flex max-w-md items-center justify-between">
      {steps.map((step, index) => (
        <div key={step} className="flex flex-1 items-center last:flex-none">
          <div className="flex flex-col items-center gap-2">
            <span
              className={[
                'flex h-8 w-8 items-center justify-center rounded-full text-xs font-bold',
                index < 2
                  ? 'bg-[#006e2f] text-white'
                  : 'bg-[#e0e3e5] text-[#3d4a3d]',
              ].join(' ')}
            >
              {index + 1}
            </span>
            <span className="text-xs font-bold uppercase tracking-[0.05em] text-[#006e2f]">
              {step}
            </span>
          </div>
          {index < steps.length - 1 ? (
            <div
              className={[
                'mx-4 h-0.5 flex-1',
                index === 0 ? 'bg-[#006e2f]' : 'bg-[#bccbb9]',
              ].join(' ')}
            />
          ) : null}
        </div>
      ))}
    </div>
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
      <span className="text-sm font-semibold text-[#545f73]">{label}</span>
      <input
        value={value}
        required={required}
        placeholder={placeholder}
        onChange={(event) => onChange(event.target.value)}
        className="mt-2 h-12 w-full rounded-lg border border-[#bccbb9] bg-white px-4 outline-none focus:border-[#006e2f] focus:ring-2 focus:ring-[#006e2f]/15"
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
