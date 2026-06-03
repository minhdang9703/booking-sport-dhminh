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
      setError('Mat khau xac nhan khong khop.')
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
      setSuccess('Dang ky thanh cong. Dang chuyen ve trang chu...')
      navigate('/')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Dang ky that bai.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <AuthShell
      activeMode="register"
      eyebrow="Dang ky"
      title="Tao tai khoan moi"
      description="Tao tai khoan customer de dat san va theo doi lich da giu cho."
      switchText="Da co tai khoan?"
      switchHref="/login"
      switchLabel="Dang nhap"
    >
      <form className="space-y-5" onSubmit={handleSubmit}>
        {error ? <FormAlert tone="error" message={error} /> : null}
        {success ? <FormAlert tone="success" message={success} /> : null}

        <FormField
          label="Ho va ten"
          name="fullName"
          value={fullName}
          placeholder="Nguyen Van A"
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
          label="So dien thoai"
          name="phoneNumber"
          type="tel"
          value={phoneNumber}
          placeholder="0900000000"
          autoComplete="tel"
          onChange={setPhoneNumber}
        />

        <div className="grid gap-5 sm:grid-cols-2">
          <FormField
            label="Mat khau"
            name="password"
            type="password"
            value={password}
            placeholder="Nhap mat khau"
            autoComplete="new-password"
            required
            onChange={setPassword}
          />

          <FormField
            label="Xac nhan mat khau"
            name="confirmPassword"
            type="password"
            value={confirmPassword}
            placeholder="Nhap lai mat khau"
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
          {isSubmitting ? 'Dang tao tai khoan...' : 'Tao tai khoan'}
        </button>
      </form>
    </AuthShell>
  )
}
