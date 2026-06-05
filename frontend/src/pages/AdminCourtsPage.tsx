import { type FormEvent, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'

import {
  createCourt,
  deleteCourt,
  getCourts,
  updateCourt,
  type Court,
  type CourtStatus,
} from '../lib/courtsApi'

type CourtFormState = {
  name: string
  courtType: string
  status: CourtStatus
}

const emptyForm: CourtFormState = {
  name: '',
  courtType: 'Sân 5',
  status: 1,
}

const statusOptions: Array<{ value: CourtStatus | 'all'; label: string }> = [
  { value: 'all', label: 'Tất cả trạng thái' },
  { value: 1, label: 'Đang hoạt động' },
  { value: 2, label: 'Tạm ngưng' },
  { value: 3, label: 'Bảo trì' },
]

function getStatusMeta(status: CourtStatus) {
  if (status === 1) {
    return {
      label: 'Đang hoạt động',
      className: 'bg-[#22c55e]/20 text-[#006e2f]',
    }
  }

  if (status === 3) {
    return {
      label: 'Bảo trì',
      className: 'bg-[#ef9900]/20 text-[#855300]',
    }
  }

  return {
    label: 'Tạm ngưng',
    className: 'bg-[#e0e3e5] text-[#545f73]',
  }
}

export function AdminCourtsPage() {
  const [courts, setCourts] = useState<Court[]>([])
  const [keyword, setKeyword] = useState('')
  const [statusFilter, setStatusFilter] = useState<CourtStatus | 'all'>('all')
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [editingCourt, setEditingCourt] = useState<Court | null>(null)
  const [deletingCourt, setDeletingCourt] = useState<Court | null>(null)
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [form, setForm] = useState<CourtFormState>(emptyForm)

  useEffect(() => {
    void reloadCourts()
  }, [])

  const filteredCourts = useMemo(() => {
    return courts.filter((court) => {
      const matchesKeyword =
        !keyword.trim() ||
        `${court.name} ${court.courtType}`
          .toLowerCase()
          .includes(keyword.trim().toLowerCase())
      const matchesStatus = statusFilter === 'all' || court.status === statusFilter

      return matchesKeyword && matchesStatus
    })
  }, [courts, keyword, statusFilter])

  const stats = useMemo(
    () => ({
      total: courts.length,
      active: courts.filter((court) => court.status === 1).length,
      maintenance: courts.filter((court) => court.status === 3).length,
      inactive: courts.filter((court) => court.status === 2).length,
    }),
    [courts],
  )

  async function reloadCourts() {
    setIsLoading(true)
    setError('')

    try {
      const response = await getCourts()
      setCourts(response)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không tải được danh sách sân.')
    } finally {
      setIsLoading(false)
    }
  }

  function openCreateForm() {
    setEditingCourt(null)
    setIsFormOpen(true)
    setForm(emptyForm)
    setSuccess('')
    setError('')
  }

  function openEditForm(court: Court) {
    setEditingCourt(court)
    setIsFormOpen(true)
    setForm({
      name: court.name,
      courtType: court.courtType,
      status: court.status,
    })
    setSuccess('')
    setError('')
  }

  function closeForm() {
    setEditingCourt(null)
    setIsFormOpen(false)
    setForm(emptyForm)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setError('')
    setSuccess('')

    try {
      if (editingCourt) {
        await updateCourt(editingCourt.id, form)
        setSuccess('Cập nhật sân thành công.')
      } else {
        await createCourt(form)
        setSuccess('Tạo sân mới thành công.')
      }

      closeForm()
      await reloadCourts()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Lưu sân thất bại.')
    } finally {
      setIsSaving(false)
    }
  }

  async function confirmDelete() {
    if (!deletingCourt) {
      return
    }

    setIsSaving(true)
    setError('')
    setSuccess('')

    try {
      await deleteCourt(deletingCourt.id)
      setSuccess('Đã xóa mềm sân.')
      setDeletingCourt(null)
      await reloadCourts()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Xóa sân thất bại.')
    } finally {
      setIsSaving(false)
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
            <h1 className="mt-2 text-3xl font-bold tracking-[-0.01em]">
              Quản lý sân
            </h1>
            <p className="mt-2 text-[#3d4a3d]">
              Tạo mới, cập nhật trạng thái và xóa mềm sân trong một địa điểm.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <AdminLink to="/admin/dashboard">Dashboard</AdminLink>
            <AdminLink to="/admin/bookings">Booking</AdminLink>
            <AdminLink to="/admin/bookings/calendar">Lịch đặt sân</AdminLink>
            <AdminLink to="/admin/price-rules">Bảng giá</AdminLink>
            <AdminLink to="/admin/revenue">Doanh thu</AdminLink>
            <AdminLink to="/admin/users">User</AdminLink>
            <button
              type="button"
              onClick={openCreateForm}
              className="rounded-lg bg-[#006e2f] px-5 py-3 text-sm font-bold text-white shadow-sm transition hover:bg-[#005321]"
            >
              + Thêm sân
            </button>
          </div>
        </header>

        <section className="mt-8 grid gap-4 md:grid-cols-4">
          <StatCard label="Tổng sân" value={stats.total} />
          <StatCard label="Đang hoạt động" value={stats.active} tone="green" />
          <StatCard label="Bảo trì" value={stats.maintenance} tone="orange" />
          <StatCard label="Tạm ngưng" value={stats.inactive} tone="gray" />
        </section>

        <section className="mt-6 rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
          <div className="grid gap-4 md:grid-cols-[1fr_240px_auto] md:items-end">
            <label>
              <span className="text-sm font-semibold text-[#3d4a3d]">
                Tìm kiếm sân
              </span>
              <input
                value={keyword}
                onChange={(event) => setKeyword(event.target.value)}
                placeholder="Nhập tên sân hoặc loại sân..."
                className="mt-2 h-12 w-full rounded-lg bg-[#eceef0] px-4 outline-none focus:ring-2 focus:ring-[#006e2f]/30"
              />
            </label>
            <label>
              <span className="text-sm font-semibold text-[#3d4a3d]">
                Trạng thái
              </span>
              <select
                value={statusFilter}
                onChange={(event) =>
                  setStatusFilter(
                    event.target.value === 'all'
                      ? 'all'
                      : (Number(event.target.value) as CourtStatus),
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
            <button
              type="button"
              onClick={() => void reloadCourts()}
              className="h-12 rounded-lg border border-[#bccbb9] px-5 text-sm font-bold text-[#006e2f]"
            >
              Làm mới
            </button>
          </div>
        </section>

        {error ? <Alert tone="error" message={error} /> : null}
        {success ? <Alert tone="success" message={success} /> : null}

        <section className="mt-6 overflow-hidden rounded-xl border border-[#bccbb9] bg-white shadow-sm">
          <div className="hidden grid-cols-[1.5fr_1fr_150px_180px] gap-4 border-b border-[#e0e3e5] bg-[#f2f4f6] px-5 py-3 text-xs font-bold uppercase tracking-[0.08em] text-[#3d4a3d] lg:grid">
            <span>Tên sân</span>
            <span>Loại sân</span>
            <span>Trạng thái</span>
            <span className="text-right">Thao tác</span>
          </div>

          {isLoading ? (
            <div className="space-y-3 p-5">
              {Array.from({ length: 4 }).map((_, index) => (
                <div key={index} className="h-20 animate-pulse rounded-lg bg-[#eceef0]" />
              ))}
            </div>
          ) : filteredCourts.length === 0 ? (
            <div className="p-10 text-center text-[#3d4a3d]">
              Không tìm thấy sân phù hợp.
            </div>
          ) : (
            filteredCourts.map((court) => (
              <CourtRow
                key={court.id}
                court={court}
                onEdit={openEditForm}
                onDelete={setDeletingCourt}
              />
            ))
          )}
        </section>
      </div>

      {isFormOpen ? (
        <CourtFormModal
          form={form}
          editingCourt={editingCourt}
          isSaving={isSaving}
          onClose={closeForm}
          onSubmit={handleSubmit}
          onChange={setForm}
        />
      ) : null}

      {deletingCourt ? (
        <ConfirmDeleteModal
          court={deletingCourt}
          isSaving={isSaving}
          onCancel={() => setDeletingCourt(null)}
          onConfirm={() => void confirmDelete()}
        />
      ) : null}
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

function StatCard({
  label,
  value,
  tone = 'default',
}: {
  label: string
  value: number
  tone?: 'default' | 'green' | 'orange' | 'gray'
}) {
  const toneClass = {
    default: 'text-[#191c1e]',
    green: 'text-[#006e2f]',
    orange: 'text-[#855300]',
    gray: 'text-[#545f73]',
  }[tone]

  return (
    <div className="rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
      <p className="text-sm font-semibold text-[#3d4a3d]">{label}</p>
      <p className={`mt-3 text-3xl font-bold ${toneClass}`}>{value}</p>
    </div>
  )
}

function CourtRow({
  court,
  onEdit,
  onDelete,
}: {
  court: Court
  onEdit: (court: Court) => void
  onDelete: (court: Court) => void
}) {
  const status = getStatusMeta(court.status)

  return (
    <div className="grid gap-4 border-b border-[#e0e3e5] px-5 py-5 last:border-b-0 lg:grid-cols-[1.5fr_1fr_150px_180px] lg:items-center">
      <p className="font-bold">{court.name}</p>
      <p className="text-sm font-semibold">{court.courtType}</p>
      <span
        className={`w-fit rounded-full px-3 py-1 text-xs font-bold ${status.className}`}
      >
        {status.label}
      </span>
      <div className="flex gap-2 lg:justify-end">
        <button
          type="button"
          onClick={() => onEdit(court)}
          className="rounded-lg border border-[#bccbb9] px-3 py-2 text-sm font-semibold text-[#006e2f]"
        >
          Sửa
        </button>
        <button
          type="button"
          onClick={() => onDelete(court)}
          className="rounded-lg border border-[#ba1a1a] px-3 py-2 text-sm font-semibold text-[#ba1a1a]"
        >
          Xóa
        </button>
      </div>
    </div>
  )
}

function CourtFormModal({
  form,
  editingCourt,
  isSaving,
  onClose,
  onSubmit,
  onChange,
}: {
  form: CourtFormState
  editingCourt: Court | null
  isSaving: boolean
  onClose: () => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
  onChange: (form: CourtFormState) => void
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <form
        onSubmit={onSubmit}
        className="w-full max-w-xl rounded-xl bg-white p-6 shadow-xl"
      >
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-2xl font-bold">
              {editingCourt ? 'Cập nhật sân' : 'Thêm sân mới'}
            </h2>
            <p className="mt-1 text-sm text-[#3d4a3d]">
              Quản lý tên sân, loại sân và trạng thái.
            </p>
          </div>
          <button type="button" onClick={onClose} className="text-2xl">
            ×
          </button>
        </div>

        <div className="mt-6 grid gap-5 sm:grid-cols-2">
          <TextInput
            label="Tên sân"
            value={form.name}
            required
            onChange={(name) => onChange({ ...form, name })}
          />
          <TextInput
            label="Loại sân"
            value={form.courtType}
            required
            onChange={(courtType) => onChange({ ...form, courtType })}
          />
          <label className="block sm:col-span-2">
            <span className="text-sm font-semibold text-[#3d4a3d]">Trạng thái</span>
            <select
              value={form.status}
              onChange={(event) =>
                onChange({
                  ...form,
                  status: Number(event.target.value) as CourtStatus,
                })
              }
              className="mt-2 h-12 w-full rounded-lg border border-[#bccbb9] px-4 outline-none focus:ring-2 focus:ring-[#006e2f]/30"
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

        <div className="mt-6 flex justify-end gap-3">
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg border border-[#bccbb9] px-5 py-3 text-sm font-bold"
          >
            Hủy
          </button>
          <button
            type="submit"
            disabled={isSaving}
            className="rounded-lg bg-[#006e2f] px-5 py-3 text-sm font-bold text-white disabled:bg-[#bccbb9]"
          >
            {isSaving ? 'Đang lưu...' : 'Lưu sân'}
          </button>
        </div>
      </form>
    </div>
  )
}

function ConfirmDeleteModal({
  court,
  isSaving,
  onCancel,
  onConfirm,
}: {
  court: Court
  isSaving: boolean
  onCancel: () => void
  onConfirm: () => void
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-md rounded-xl bg-white p-6 shadow-xl">
        <h2 className="text-xl font-bold">Xóa sân?</h2>
        <p className="mt-3 text-[#3d4a3d]">
          Sân <strong>{court.name}</strong> sẽ được xóa mềm và không còn xuất hiện
          trong danh sách public.
        </p>
        <div className="mt-6 flex justify-end gap-3">
          <button
            type="button"
            onClick={onCancel}
            className="rounded-lg border border-[#bccbb9] px-5 py-3 text-sm font-bold"
          >
            Hủy
          </button>
          <button
            type="button"
            disabled={isSaving}
            onClick={onConfirm}
            className="rounded-lg bg-[#ba1a1a] px-5 py-3 text-sm font-bold text-white disabled:bg-[#bccbb9]"
          >
            {isSaving ? 'Đang xóa...' : 'Xóa sân'}
          </button>
        </div>
      </div>
    </div>
  )
}

function TextInput({
  label,
  value,
  required,
  onChange,
}: {
  label: string
  value: string
  required?: boolean
  onChange: (value: string) => void
}) {
  return (
    <label className="block">
      <span className="text-sm font-semibold text-[#3d4a3d]">{label}</span>
      <input
        value={value}
        required={required}
        onChange={(event) => onChange(event.target.value)}
        className="mt-2 h-12 w-full rounded-lg border border-[#bccbb9] px-4 outline-none focus:ring-2 focus:ring-[#006e2f]/30"
      />
    </label>
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
