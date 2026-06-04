import { type FormEvent, useState } from 'react'
import { useNavigate } from 'react-router-dom'

import { AuthShell } from '../components/AuthShell'
import { FormAlert } from '../components/FormAlert'
import { FormField } from '../components/FormField'
import { register, saveAuthSession } from '../lib/authApi'

export function RegisterPage() {
  const navigate = useNavigate()
  const [fullName, setFullName] = useState('')
  const [email, setEmail] = useState('')
  const [phoneNumber, setPhoneNumber] = useState('')
  const [password, setPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setSuccess('')

    if (password !== confirmPassword) {
      setError('Mật khẩu xác nhận không khớp.')
      return
    }

    setIsSubmitting(true)

    try {
      const response = await register({
        fullName,
        email,
        password,
        phoneNumber: phoneNumber || undefined,
      })

      saveAuthSession(response)
      setSuccess('Đăng ký thành công. Đang chuyển về trang chủ...')
      navigate('/')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Đăng ký thất bại.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthShell
      activeMode="register"
      eyebrow="Đăng ký"
      title="Tạo tài khoản mới"
      description="Tạo tài khoản khách hàng để đặt sân và theo dõi lịch đã giữ chỗ."
      switchText="Đã có tài khoản?"
      switchHref="/login"
      switchLabel="Đăng nhập"
    >
      <form className="space-y-5" onSubmit={handleSubmit}>
        {error ? <FormAlert tone="error" message={error} /> : null}
        {success ? <FormAlert tone="success" message={success} /> : null}

        <FormField
          label="Họ và tên"
          name="fullName"
          value={fullName}
          placeholder="Nguyễn Văn A"
          autoComplete="name"
          required
          onChange={setFullName}
        />

        <FormField
          label="Email"
          name="email"
          type="email"
          value={email}
          placeholder="user@example.com"
          autoComplete="email"
          required
          onChange={setEmail}
        />

        <FormField
          label="Số điện thoại"
          name="phoneNumber"
          type="tel"
          value={phoneNumber}
          placeholder="0900000000"
          autoComplete="tel"
          onChange={setPhoneNumber}
        />

        <div className="grid gap-5 sm:grid-cols-2">
          <FormField
            label="Mật khẩu"
            name="password"
            type="password"
            value={password}
            placeholder="Nhập mật khẩu"
            autoComplete="new-password"
            required
            onChange={setPassword}
          />

          <FormField
            label="Xác nhận mật khẩu"
            name="confirmPassword"
            type="password"
            value={confirmPassword}
            placeholder="Nhập lại mật khẩu"
            autoComplete="new-password"
            required
            onChange={setConfirmPassword}
          />
        </div>

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-lg bg-[#006e2f] px-5 py-3 text-base font-semibold text-white shadow-[0_4px_6px_-1px_rgba(0,0,0,0.1)] transition hover:bg-[#005321] disabled:cursor-not-allowed disabled:bg-[#6d7b6c]"
        >
          {isSubmitting ? 'Đang tạo tài khoản...' : 'Tạo tài khoản'}
        </button>
      </form>
    </AuthShell>
  )
}
