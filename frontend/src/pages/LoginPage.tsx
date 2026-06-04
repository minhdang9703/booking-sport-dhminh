import { type FormEvent, useState } from 'react'
import { useNavigate } from 'react-router-dom'

import { AuthShell } from '../components/AuthShell'
import { FormAlert } from '../components/FormAlert'
import { FormField } from '../components/FormField'
import { isAdminUser, login, saveAuthSession } from '../lib/authApi'

export function LoginPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError('')
    setIsSubmitting(true)

    try {
      const response = await login({ email, password })
      saveAuthSession(response)
      navigate(isAdminUser(response.user) ? '/admin' : '/')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Đăng nhập thất bại.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthShell
      activeMode="login"
      eyebrow="Đăng nhập"
      title="Chào mừng trở lại"
      description="Vui lòng nhập thông tin để truy cập hệ thống."
      switchText="Chưa có tài khoản?"
      switchHref="/register"
      switchLabel="Đăng ký ngay"
    >
      <form className="space-y-5" onSubmit={handleSubmit}>
        {error ? <FormAlert tone="error" message={error} /> : null}

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
          label="Mật khẩu"
          name="password"
          type="password"
          value={password}
          placeholder="Nhập mật khẩu"
          autoComplete="current-password"
          required
          onChange={setPassword}
        />

        <div className="flex items-center justify-between gap-4">
          <label className="flex items-center gap-2 text-sm text-[#3d4a3d]">
            <input
              type="checkbox"
              className="h-4 w-4 rounded border-[#bccbb9] text-[#006e2f] focus:ring-[#006e2f]"
            />
            Ghi nhớ đăng nhập
          </label>
          <button
            type="button"
            className="text-xs font-bold text-[#006e2f] hover:text-[#004b1e]"
          >
            Quên mật khẩu?
          </button>
        </div>

        <button
          type="submit"
          disabled={isSubmitting}
          className="w-full rounded-lg bg-[#006e2f] px-5 py-3 text-base font-semibold text-white shadow-[0_4px_6px_-1px_rgba(0,0,0,0.1)] transition hover:bg-[#005321] disabled:cursor-not-allowed disabled:bg-[#6d7b6c]"
        >
          {isSubmitting ? 'Đang đăng nhập...' : 'Đăng nhập'}
        </button>
      </form>
    </AuthShell>
  )
}
