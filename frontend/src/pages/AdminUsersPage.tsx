import { type FormEvent, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'

import {
  createUser,
  deleteUser,
  getUsers,
  updateUser,
  type UserResponse,
  type UserRole,
} from '../lib/usersApi'

type UserFormState = {
  fullName: string
  email: string
  password: string
  phoneNumber: string
  role: UserRole
}

const emptyForm: UserFormState = {
  fullName: '',
  email: '',
  password: '',
  phoneNumber: '',
  role: 1,
}

const roleOptions: Array<{ value: UserRole | 'all'; label: string }> = [
  { value: 'all', label: 'Tất cả vai trò' },
  { value: 1, label: 'Customer' },
  { value: 2, label: 'Owner' },
  { value: 3, label: 'Admin' },
]

function getRoleMeta(role: UserRole) {
  if (role === 3) {
    return {
      label: 'Admin',
      className: 'bg-[#ef9900]/20 text-[#855300]',
    }
  }

  if (role === 2) {
    return {
      label: 'Owner',
      className: 'bg-[#d5e0f8]/50 text-[#545f73]',
    }
  }

  return {
    label: 'Customer',
    className: 'bg-[#22c55e]/20 text-[#006e2f]',
  }
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  }).format(new Date(value))
}

export function AdminUsersPage() {
  const [users, setUsers] = useState<UserResponse[]>([])
  const [keyword, setKeyword] = useState('')
  const [roleFilter, setRoleFilter] = useState<UserRole | 'all'>('all')
  const [isLoading, setIsLoading] = useState(true)
  const [isSaving, setIsSaving] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [isFormOpen, setIsFormOpen] = useState(false)
  const [editingUser, setEditingUser] = useState<UserResponse | null>(null)
  const [deletingUser, setDeletingUser] = useState<UserResponse | null>(null)
  const [form, setForm] = useState<UserFormState>(emptyForm)

  useEffect(() => {
    let isMounted = true

    getUsers()
      .then((response) => {
        if (isMounted) {
          setUsers(response)
        }
      })
      .catch((err: unknown) => {
        if (isMounted) {
          setError(
            err instanceof Error
              ? err.message
              : 'Không tải được danh sách user.',
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

  const filteredUsers = useMemo(() => {
    return users.filter((user) => {
      const matchesKeyword =
        !keyword.trim() ||
        `${user.fullName} ${user.email} ${user.phoneNumber ?? ''}`
          .toLowerCase()
          .includes(keyword.trim().toLowerCase())
      const matchesRole = roleFilter === 'all' || user.role === roleFilter

      return matchesKeyword && matchesRole
    })
  }, [keyword, roleFilter, users])

  const stats = useMemo(
    () => ({
      total: users.length,
      customers: users.filter((user) => user.role === 1).length,
      owners: users.filter((user) => user.role === 2).length,
      admins: users.filter((user) => user.role === 3).length,
    }),
    [users],
  )

  async function reloadUsers() {
    setIsLoading(true)
    setError('')

    try {
      const response = await getUsers()
      setUsers(response)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không tải được danh sách user.')
    } finally {
      setIsLoading(false)
    }
  }

  function openCreateForm() {
    setEditingUser(null)
    setForm(emptyForm)
    setIsFormOpen(true)
    setError('')
    setSuccess('')
  }

  function openEditForm(user: UserResponse) {
    setEditingUser(user)
    setForm({
      fullName: user.fullName,
      email: user.email,
      password: '',
      phoneNumber: user.phoneNumber ?? '',
      role: user.role,
    })
    setIsFormOpen(true)
    setError('')
    setSuccess('')
  }

  function closeForm() {
    setEditingUser(null)
    setForm(emptyForm)
    setIsFormOpen(false)
  }

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setIsSaving(true)
    setError('')
    setSuccess('')

    try {
      if (editingUser) {
        await updateUser(editingUser.id, {
          fullName: form.fullName,
          phoneNumber: form.phoneNumber || undefined,
          role: form.role,
        })
        setSuccess('Cập nhật user thành công.')
      } else {
        await createUser({
          fullName: form.fullName,
          email: form.email,
          password: form.password,
          phoneNumber: form.phoneNumber || undefined,
          role: form.role,
        })
        setSuccess('Tạo user mới thành công.')
      }

      closeForm()
      await reloadUsers()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Lưu user thất bại.')
    } finally {
      setIsSaving(false)
    }
  }

  async function confirmDelete() {
    if (!deletingUser) {
      return
    }

    setIsSaving(true)
    setError('')
    setSuccess('')

    try {
      await deleteUser(deletingUser.id)
      setSuccess('Đã xóa mềm user.')
      setDeletingUser(null)
      await reloadUsers()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Xóa user thất bại.')
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
              Quản lý user
            </h1>
            <p className="mt-2 text-[#3d4a3d]">
              Theo dõi tài khoản, phân quyền và xóa mềm user trong hệ thống.
            </p>
          </div>
          <div className="flex flex-wrap gap-3">
            <AdminLink to="/admin/dashboard">Dashboard</AdminLink>
            <AdminLink to="/admin/bookings">Booking</AdminLink>
            <AdminLink to="/admin/bookings/calendar">Lịch đặt sân</AdminLink>
            <AdminLink to="/admin/revenue">Doanh thu</AdminLink>
            <AdminLink to="/admin/courts">Quản lý sân</AdminLink>
            <button
              type="button"
              onClick={openCreateForm}
              className="rounded-lg bg-[#006e2f] px-5 py-3 text-sm font-bold text-white shadow-sm transition hover:bg-[#005321]"
            >
              + Thêm user
            </button>
          </div>
        </header>

        <section className="mt-8 grid gap-4 md:grid-cols-4">
          <StatCard label="Tổng user" value={stats.total} />
          <StatCard label="Customer" value={stats.customers} tone="green" />
          <StatCard label="Owner" value={stats.owners} tone="gray" />
          <StatCard label="Admin" value={stats.admins} tone="orange" />
        </section>

        <section className="mt-6 rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
          <div className="grid gap-4 md:grid-cols-[1fr_240px] md:items-end">
            <label>
              <span className="text-sm font-semibold text-[#3d4a3d]">
                Tìm kiếm user
              </span>
              <input
                value={keyword}
                onChange={(event) => setKeyword(event.target.value)}
                placeholder="Tên, email hoặc số điện thoại..."
                className="mt-2 h-12 w-full rounded-lg bg-[#eceef0] px-4 outline-none focus:ring-2 focus:ring-[#006e2f]/30"
              />
            </label>
            <label>
              <span className="text-sm font-semibold text-[#3d4a3d]">
                Vai trò
              </span>
              <select
                value={roleFilter}
                onChange={(event) =>
                  setRoleFilter(
                    event.target.value === 'all'
                      ? 'all'
                      : (Number(event.target.value) as UserRole),
                  )
                }
                className="mt-2 h-12 w-full rounded-lg bg-[#eceef0] px-4 outline-none focus:ring-2 focus:ring-[#006e2f]/30"
              >
                {roleOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
          </div>
        </section>

        {error ? <Alert tone="error" message={error} /> : null}
        {success ? <Alert tone="success" message={success} /> : null}

        <section className="mt-6 rounded-xl border border-[#bccbb9] bg-white p-5 shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[900px] border-collapse">
              <thead>
                <tr className="border-b border-[#e0e3e5] text-left text-sm text-[#545f73]">
                  <th className="py-3 pr-4 font-semibold">User</th>
                  <th className="py-3 pr-4 font-semibold">Email</th>
                  <th className="py-3 pr-4 font-semibold">Số điện thoại</th>
                  <th className="py-3 pr-4 font-semibold">Vai trò</th>
                  <th className="py-3 pr-4 font-semibold">Ngày tạo</th>
                  <th className="py-3 pr-4 text-right font-semibold">Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {isLoading
                  ? Array.from({ length: 5 }).map((_, index) => (
                      <tr key={index} className="border-b border-[#eef1ef]">
                        <td colSpan={6} className="py-4">
                          <div className="h-10 animate-pulse rounded-lg bg-[#eceef0]" />
                        </td>
                      </tr>
                    ))
                  : filteredUsers.map((user) => (
                      <UserRow
                        key={user.id}
                        user={user}
                        onEdit={openEditForm}
                        onDelete={setDeletingUser}
                      />
                    ))}
              </tbody>
            </table>
          </div>

          {!isLoading && filteredUsers.length === 0 ? (
            <div className="rounded-lg border border-dashed border-[#bccbb9] p-8 text-center text-sm text-[#545f73]">
              Không có user phù hợp với bộ lọc hiện tại.
            </div>
          ) : null}
        </section>

        {isFormOpen ? (
          <UserFormModal
            form={form}
            editingUser={editingUser}
            isSaving={isSaving}
            onClose={closeForm}
            onChange={setForm}
            onSubmit={handleSubmit}
          />
        ) : null}

        {deletingUser ? (
          <ConfirmDeleteModal
            user={deletingUser}
            isSaving={isSaving}
            onCancel={() => setDeletingUser(null)}
            onConfirm={confirmDelete}
          />
        ) : null}
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

function StatCard({
  label,
  value,
  tone = 'default',
}: {
  label: string
  value: number
  tone?: 'default' | 'green' | 'orange' | 'gray'
}) {
  const toneClassName =
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
      <p className={['mt-3 text-3xl font-bold', toneClassName].join(' ')}>
        {value}
      </p>
    </article>
  )
}

function UserRow({
  user,
  onEdit,
  onDelete,
}: {
  user: UserResponse
  onEdit: (user: UserResponse) => void
  onDelete: (user: UserResponse) => void
}) {
  const role = getRoleMeta(user.role)

  return (
    <tr className="border-b border-[#eef1ef] text-sm last:border-0">
      <td className="py-4 pr-4">
        <div className="flex items-center gap-3">
          <div className="flex h-10 w-10 items-center justify-center rounded-full bg-[#006e2f] text-sm font-bold text-white">
            {user.fullName.slice(0, 1).toUpperCase()}
          </div>
          <div>
            <p className="font-bold">{user.fullName}</p>
            <p className="text-xs text-[#545f73]">
              #{user.id.slice(0, 8).toUpperCase()}
            </p>
          </div>
        </div>
      </td>
      <td className="py-4 pr-4 text-[#3d4a3d]">{user.email}</td>
      <td className="py-4 pr-4 text-[#3d4a3d]">
        {user.phoneNumber || 'Chưa cập nhật'}
      </td>
      <td className="py-4 pr-4">
        <span
          className={[
            'rounded-full px-3 py-1 text-xs font-bold',
            role.className,
          ].join(' ')}
        >
          {role.label}
        </span>
      </td>
      <td className="py-4 pr-4 text-[#3d4a3d]">{formatDate(user.createdAt)}</td>
      <td className="py-4 pr-4 text-right">
        <div className="flex justify-end gap-2">
          <button
            type="button"
            onClick={() => onEdit(user)}
            className="rounded-lg border border-[#bccbb9] px-3 py-2 text-xs font-bold text-[#3d4a3d] transition hover:border-[#006e2f] hover:text-[#006e2f]"
          >
            Sửa
          </button>
          <button
            type="button"
            onClick={() => onDelete(user)}
            className="rounded-lg border border-[#f5b4ab] px-3 py-2 text-xs font-bold text-[#ba1a1a] transition hover:bg-[#ffdad6]"
          >
            Xóa
          </button>
        </div>
      </td>
    </tr>
  )
}

function UserFormModal({
  form,
  editingUser,
  isSaving,
  onClose,
  onChange,
  onSubmit,
}: {
  form: UserFormState
  editingUser: UserResponse | null
  isSaving: boolean
  onClose: () => void
  onChange: (form: UserFormState) => void
  onSubmit: (event: FormEvent<HTMLFormElement>) => void
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <form
        onSubmit={onSubmit}
        className="w-full max-w-[680px] rounded-xl bg-white p-6 shadow-xl"
      >
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-2xl font-bold">
              {editingUser ? 'Cập nhật user' : 'Thêm user mới'}
            </h2>
            <p className="mt-1 text-sm text-[#545f73]">
              Admin có thể tạo user và cập nhật vai trò trong hệ thống.
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

        <div className="mt-6 grid gap-4 sm:grid-cols-2">
          <TextInput
            label="Họ tên"
            value={form.fullName}
            required
            onChange={(fullName) => onChange({ ...form, fullName })}
          />
          <label className="block">
            <span className="text-sm font-semibold text-[#3d4a3d]">Vai trò</span>
            <select
              value={form.role}
              onChange={(event) =>
                onChange({ ...form, role: Number(event.target.value) as UserRole })
              }
              className="mt-2 h-12 w-full rounded-lg border border-[#bccbb9] px-4 outline-none focus:ring-2 focus:ring-[#006e2f]/30"
            >
              {roleOptions
                .filter((option) => option.value !== 'all')
                .map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
            </select>
          </label>
          <TextInput
            label="Email"
            value={form.email}
            required={!editingUser}
            disabled={Boolean(editingUser)}
            onChange={(email) => onChange({ ...form, email })}
          />
          <TextInput
            label="Số điện thoại"
            value={form.phoneNumber}
            onChange={(phoneNumber) => onChange({ ...form, phoneNumber })}
          />
          {!editingUser ? (
            <TextInput
              label="Mật khẩu"
              type="password"
              value={form.password}
              required
              onChange={(password) => onChange({ ...form, password })}
            />
          ) : null}
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
            className="rounded-lg bg-[#006e2f] px-5 py-3 text-sm font-bold text-white disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSaving ? 'Đang lưu...' : 'Lưu user'}
          </button>
        </div>
      </form>
    </div>
  )
}

function ConfirmDeleteModal({
  user,
  isSaving,
  onCancel,
  onConfirm,
}: {
  user: UserResponse
  isSaving: boolean
  onCancel: () => void
  onConfirm: () => void
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 px-4">
      <div className="w-full max-w-[420px] rounded-xl bg-white p-6 shadow-xl">
        <h2 className="text-xl font-bold">Xóa user?</h2>
        <p className="mt-2 text-sm text-[#545f73]">
          User “{user.fullName}” sẽ bị xóa mềm và không còn đăng nhập được.
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
            className="rounded-lg bg-[#ba1a1a] px-5 py-3 text-sm font-bold text-white disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isSaving ? 'Đang xóa...' : 'Xóa user'}
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
  disabled,
  onChange,
}: {
  label: string
  value: string
  type?: string
  required?: boolean
  disabled?: boolean
  onChange: (value: string) => void
}) {
  return (
    <label className="block">
      <span className="text-sm font-semibold text-[#3d4a3d]">{label}</span>
      <input
        type={type}
        value={value}
        required={required}
        disabled={disabled}
        onChange={(event) => onChange(event.target.value)}
        className="mt-2 h-12 w-full rounded-lg border border-[#bccbb9] px-4 outline-none focus:ring-2 focus:ring-[#006e2f]/30 disabled:cursor-not-allowed disabled:bg-[#eceef0] disabled:text-[#545f73]"
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
