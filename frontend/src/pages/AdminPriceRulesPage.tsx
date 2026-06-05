import { type FormEvent, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'

import {
  createPriceRule,
  deletePriceRule,
  getPriceRules,
  updatePriceRule,
  type PriceRule,
  type PriceRuleCreateRequest,
} from '../lib/priceRulesApi'

type PriceRuleFormState = {
  name: string
  dayOfWeek: number
  startTime: string
  endTime: string
  hourlyPrice: string
  isEnabled: boolean
}

const dayOptions = [
  { value: 0, label: 'Chủ nhật' },
  { value: 1, label: 'Thứ hai' },
  { value: 2, label: 'Thứ ba' },
  { value: 3, label: 'Thứ tư' },
  { value: 4, label: 'Thứ năm' },
  { value: 5, label: 'Thứ sáu' },
  { value: 6, label: 'Thứ bảy' },
]

const emptyForm: PriceRuleFormState = {
  name: '',
  dayOfWeek: 1,
  startTime: '05:00',
  endTime: '23:00',
  hourlyPrice: '250000',
  isEnabled: true,
}

function toFormState(rule: PriceRule): PriceRuleFormState {
  return {
    name: rule.name,
    dayOfWeek: rule.dayOfWeek,
    startTime: rule.startTime.slice(0, 5),
    endTime: rule.endTime.slice(0, 5),
    hourlyPrice: rule.hourlyPrice.toString(),
    isEnabled: rule.isEnabled,
  }
}

function toRequest(form: PriceRuleFormState): PriceRuleCreateRequest {
  return {
    name: form.name,
    dayOfWeek: form.dayOfWeek,
    startTime: `${form.startTime}:00`,
    endTime: `${form.endTime}:00`,
    hourlyPrice: Number(form.hourlyPrice),
    isEnabled: form.isEnabled,
  }
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value)
}

function formatTime(value: string) {
  return value.slice(0, 5)
}

function getDayLabel(value: number) {
  return dayOptions.find((day) => day.value === value)?.label ?? `Ngày ${value}`
}

export function AdminPriceRulesPage() {
  const [rules, setRules] = useState<PriceRule[]>([])
  const [selectedDay, setSelectedDay] = useState<number | 'all'>('all')
  const [editingRule, setEditingRule] = useState<PriceRule | null>(null)
  const [deletingRule, setDeletingRule] = useState<PriceRule | null>(null)
  const [form, setForm] = useState<PriceRuleFormState>(emptyForm)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  useEffect(() => {
    void reloadRules()
  }, [])

  const filteredRules = useMemo(() => {
    return rules.filter((rule) => selectedDay === 'all' || rule.dayOfWeek === selectedDay)
  }, [rules, selectedDay])

  async function reloadRules() {
    setIsLoading(true)
    setError('')

    try {
      const response = await getPriceRules()
      setRules(response)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không tải được bảng giá.')
    } finally {
      setIsLoading(false)
    }
  }

  function openCreateForm() {
    setEditingRule(null)
    setForm(emptyForm)
    setIsFormOpen(true)
    setError('')
    setSuccess('')
  }

  function openEditForm(rule: PriceRule) {
    setEditingRule(rule)
    setForm(toFormState(rule))
    setIsFormOpen(true)
    setError('')
    setSuccess('')
  }

  function closeForm() {
    setEditingRule(null)
    setIsFormOpen(false)
    setForm(emptyForm)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setError('')
    setSuccess('')

    try {
      if (editingRule) {
        await updatePriceRule(editingRule.id, toRequest(form))
        setSuccess('Cập nhật khung giá thành công.')
      } else {
        await createPriceRule(toRequest(form))
        setSuccess('Tạo khung giá thành công.')
      }

      closeForm()
      await reloadRules()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Lưu khung giá thất bại.')
    } finally {
      setIsSaving(false)
    }
  }

  async function confirmDelete() {
    if (!deletingRule) {
      return
    }

    setIsSaving(true)
    setError('')
    setSuccess('')

    try {
      await deletePriceRule(deletingRule.id)
      setSuccess('Đã xóa khung giá.')
      setDeletingRule(null)
      await reloadRules()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Xóa khung giá thất bại.')
    } finally {
      setIsSaving(false)
    }
  }

  return (
    <main className="min-h-screen bg-[#f7f9fb] text-[#191c1e]">
      <div className="mx-auto max-w-[1100px] px-4 pb-16 pt-24 sm:px-6">
        <header className="flex flex-col gap-4 md:flex-row md:items-end md:justify-between">
          <div>
            <p className="text-sm font-bold uppercase tracking-[0.14em] text-[#006e2f]">
              Admin
            </p>
            <h1 className="mt-2 text-3xl font-bold tracking-[-0.01em]">
              Bảng giá đặt sân
            </h1>
            <p className="mt-2 text-[#3d4a3d]">
              Quản lý khung giờ, ngày áp dụng và đơn giá theo giờ.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <AdminLink to="/admin/dashboard">Dashboard</AdminLink>
            <AdminLink to="/admin/courts">Quản lý sân</AdminLink>
            <AdminLink to="/admin/bookings">Booking</AdminLink>
            <AdminLink to="/admin/revenue">Doanh thu</AdminLink>
            <button
              type="button"
              onClick={openCreateForm}
              className="rounded-lg bg-[#006e2f] px-5 py-3 text-sm font-bold text-white"
            >
              + Thêm khung giá
            </button>
          </div>
        </header>

        <section className="mt-6 rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
          <label className="block max-w-xs">
            <span className="text-sm font-semibold text-[#3d4a3d]">Lọc theo ngày</span>
            <select
              value={selectedDay}
              onChange={(event) =>
                setSelectedDay(
                  event.target.value === 'all' ? 'all' : Number(event.target.value),
                )
              }
              className="mt-2 h-12 w-full rounded-lg bg-[#eceef0] px-4 outline-none focus:ring-2 focus:ring-[#006e2f]/30"
            >
              <option value="all">Tất cả</option>
              {dayOptions.map((day) => (
                <option key={day.value} value={day.value}>
                  {day.label}
                </option>
              ))}
            </select>
          </label>
        </section>

        {error ? <Alert tone="error" message={error} /> : null}
        {success ? <Alert tone="success" message={success} /> : null}

        <section className="mt-6 overflow-hidden rounded-xl border border-[#bccbb9] bg-white shadow-sm">
          {isLoading ? (
            <div className="space-y-3 p-5">
              {Array.from({ length: 4 }).map((_, index) => (
                <div key={index} className="h-20 animate-pulse rounded-lg bg-[#eceef0]" />
              ))}
            </div>
          ) : filteredRules.length === 0 ? (
            <div className="p-10 text-center text-[#3d4a3d]">
              Chưa có khung giá phù hợp.
            </div>
          ) : (
            filteredRules.map((rule) => (
              <div
                key={rule.id}
                className="grid gap-4 border-b border-[#e0e3e5] px-5 py-5 last:border-b-0 lg:grid-cols-[1.2fr_1fr_1fr_1fr_150px] lg:items-center"
              >
                <div>
                  <p className="font-bold">{rule.name}</p>
                  <p className="mt-1 text-sm text-[#3d4a3d]">{getDayLabel(rule.dayOfWeek)}</p>
                </div>
                <p className="font-semibold">
                  {formatTime(rule.startTime)} - {formatTime(rule.endTime)}
                </p>
                <p className="font-bold text-[#006e2f]">
                  {formatCurrency(rule.hourlyPrice)}/giờ
                </p>
                <span
                  className={[
                    'w-fit rounded-full px-3 py-1 text-xs font-bold',
                    rule.isEnabled
                      ? 'bg-[#22c55e]/20 text-[#006e2f]'
                      : 'bg-[#e0e3e5] text-[#545f73]',
                  ].join(' ')}
                >
                  {rule.isEnabled ? 'Đang bật' : 'Đang tắt'}
                </span>
                <div className="flex gap-2 lg:justify-end">
                  <button
                    type="button"
                    onClick={() => openEditForm(rule)}
                    className="rounded-lg border border-[#bccbb9] px-3 py-2 text-sm font-semibold text-[#006e2f]"
                  >
                    Sửa
                  </button>
                  <button
                    type="button"
                    onClick={() => setDeletingRule(rule)}
                    className="rounded-lg border border-[#ba1a1a] px-3 py-2 text-sm font-semibold text-[#ba1a1a]"
                  >
                    Xóa
                  </button>
                </div>
              </div>
            ))
          )}
        </section>
      </div>

      {isFormOpen ? (
        <PriceRuleFormModal
          form={form}
          editingRule={editingRule}
          isSaving={isSaving}
          onClose={closeForm}
          onSubmit={handleSubmit}
          onChange={setForm}
        />
      ) : null}

      {deletingRule ? (
        <ConfirmDeleteModal
          rule={deletingRule}
          isSaving={isSaving}
          onCancel={() => setDeletingRule(null)}
          onConfirm={() => void confirmDelete()}
        />
      ) : null}
    </main>
  )
}

function PriceRuleFormModal({
  form,
  editingRule,
  isSaving,
  onClose,
  onSubmit,
  onChange,
}: {
  form: PriceRuleFormState
  editingRule: PriceRule | null
  isSaving: boolean
  onClose: () => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onChange: (form: PriceRuleFormState) => void
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <form onSubmit={onSubmit} className="w-full max-w-2xl rounded-xl bg-white p-6 shadow-xl">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-2xl font-bold">
              {editingRule ? 'Cập nhật khung giá' : 'Thêm khung giá'}
            </h2>
            <p className="mt-1 text-sm text-[#3d4a3d]">
              Khung giá enabled cùng ngày không được chồng lấn giờ.
            </p>
          </div>
          <button type="button" onClick={onClose} className="text-2xl">
            ×
          </button>
        </div>

        <div className="mt-6 grid gap-5 sm:grid-cols-2">
          <TextInput
            label="Tên rule"
            value={form.name}
            required
            onChange={(name) => onChange({ ...form, name })}
          />
          <label className="block">
            <span className="text-sm font-semibold text-[#3d4a3d]">Ngày áp dụng</span>
            <select
              value={form.dayOfWeek}
              onChange={(event) => onChange({ ...form, dayOfWeek: Number(event.target.value) })}
              className="mt-2 h-12 w-full rounded-lg border border-[#bccbb9] px-4 outline-none focus:ring-2 focus:ring-[#006e2f]/30"
            >
              {dayOptions.map((day) => (
                <option key={day.value} value={day.value}>
                  {day.label}
                </option>
              ))}
            </select>
          </label>
          <TextInput
            type="time"
            label="Giờ bắt đầu"
            value={form.startTime}
            required
            onChange={(startTime) => onChange({ ...form, startTime })}
          />
          <TextInput
            type="time"
            label="Giờ kết thúc"
            value={form.endTime}
            required
            onChange={(endTime) => onChange({ ...form, endTime })}
          />
          <TextInput
            type="number"
            label="Đơn giá theo giờ"
            value={form.hourlyPrice}
            required
            onChange={(hourlyPrice) => onChange({ ...form, hourlyPrice })}
          />
          <label className="flex items-center gap-3 rounded-lg bg-[#f7f9fb] px-4 py-3 sm:mt-7">
            <input
              type="checkbox"
              checked={form.isEnabled}
              onChange={(event) => onChange({ ...form, isEnabled: event.target.checked })}
              className="h-4 w-4 accent-[#006e2f]"
            />
            <span className="text-sm font-semibold text-[#3d4a3d]">Bật khung giá</span>
          </label>
        </div>

        <div className="mt-6 flex justify-end gap-3">
          <button type="button" onClick={onClose} className="rounded-lg border border-[#bccbb9] px-5 py-3 text-sm font-bold">
            Hủy
          </button>
          <button type="submit" disabled={isSaving} className="rounded-lg bg-[#006e2f] px-5 py-3 text-sm font-bold text-white disabled:bg-[#bccbb9]">
            {isSaving ? 'Đang lưu...' : 'Lưu khung giá'}
          </button>
        </div>
      </form>
    </div>
  )
}

function ConfirmDeleteModal({
  rule,
  isSaving,
  onCancel,
  onConfirm,
}: {
  rule: PriceRule
  isSaving: boolean
  onCancel: () => void
  onConfirm: () => void
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-md rounded-xl bg-white p-6 shadow-xl">
        <h2 className="text-xl font-bold">Xóa khung giá?</h2>
        <p className="mt-3 text-[#3d4a3d]">
          Khung giá <strong>{rule.name}</strong> sẽ bị xóa khỏi hệ thống.
        </p>
        <div className="mt-6 flex justify-end gap-3">
          <button type="button" onClick={onCancel} className="rounded-lg border border-[#bccbb9] px-5 py-3 text-sm font-bold">
            Hủy
          </button>
          <button type="button" disabled={isSaving} onClick={onConfirm} className="rounded-lg bg-[#ba1a1a] px-5 py-3 text-sm font-bold text-white disabled:bg-[#bccbb9]">
            {isSaving ? 'Đang xóa...' : 'Xóa'}
          </button>
        </div>
      </div>
    </div>
  )
}

function TextInput({
  label,
  value,
  type = 'text',
  required,
  onChange,
}: {
  label: string
  value: string
  type?: string
  required?: boolean
  onChange: (value: string) => void
}) {
  return (
    <label className="block">
      <span className="text-sm font-semibold text-[#3d4a3d]">{label}</span>
      <input
        type={type}
        value={value}
        required={required}
        onChange={(event) => onChange(event.target.value)}
        className="mt-2 h-12 w-full rounded-lg border border-[#bccbb9] px-4 outline-none focus:ring-2 focus:ring-[#006e2f]/30"
      />
    </label>
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

function Alert({ tone, message }: { tone: 'error' | 'success'; message: string }) {
  return (
    <div
      className={[
        'mt-6 rounded-xl border px-4 py-3 text-sm font-medium',
        tone === 'error'
          ? 'border-[#ffdad6] bg-[#ffdad6] text-[#93000a]'
          : 'border-[#d6f5df] bg-[#e9f9ef] text-[#005321]',
      ].join(' ')}
    >
      {message}
    </div>
  )
}
