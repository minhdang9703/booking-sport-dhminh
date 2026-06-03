import type { ReactNode } from 'react'
import { Link } from 'react-router-dom'

import fieldImage from '../assets/auth/field.png'
import logoImage from '../assets/auth/logo.png'

type AuthShellProps = {
  activeMode: 'login' | 'register'
  eyebrow: string
  title: string
  description: string
  switchText: string
  switchHref: string
  switchLabel: string
  children: ReactNode
}

export function AuthShell({
  activeMode,
  eyebrow,
  title,
  description,
  switchText,
  switchHref,
  switchLabel,
  children,
}: AuthShellProps) {
  return (
    <div className="grid min-h-screen bg-[#f7f9fb] lg:grid-cols-2">
      <section className="relative hidden min-h-screen overflow-hidden bg-[#006e2f] lg:flex lg:items-center">
        <img
          src={fieldImage}
          alt=""
          className="absolute inset-0 h-full w-full object-cover"
        />
        <div className="absolute inset-0 bg-gradient-to-t from-black/80 via-black/25 to-transparent" />

        <div className="relative z-10 px-16 text-white">
          <img src={logoImage} alt="AthletiBook" className="h-16 w-16" />
          <h2 className="mt-8 max-w-md text-5xl font-extrabold leading-[1.16] tracking-[-0.02em]">
            Nâng Tầm Trải Nghiệm Thể Thao
          </h2>
          <p className="mt-6 max-w-xl text-lg leading-8 text-white/90">
            Nền tảng quản lý và đặt sân vận động hàng đầu. Tiết kiệm thời
            gian, tối ưu hóa hiệu suất và kết nối cộng đồng yêu thể thao của
            bạn.
          </p>

          <div className="mt-10 flex gap-10">
            <div>
              <p className="text-2xl font-semibold text-[#6bff8f]">100+</p>
              <p className="mt-1 text-xs font-bold uppercase tracking-[0.18em] text-white/70">
                Sân vận động
              </p>
            </div>
            <div className="border-l border-white/20 pl-10">
              <p className="text-2xl font-semibold text-[#6bff8f]">10k+</p>
              <p className="mt-1 text-xs font-bold uppercase tracking-[0.18em] text-white/70">
                Người dùng
              </p>
            </div>
          </div>
        </div>
      </section>

      <section className="flex min-h-screen items-center justify-center bg-[radial-gradient(circle_at_top_left,rgba(226,232,240,0.9),rgba(247,249,251,0)_32%)] px-4 py-10 sm:px-8 lg:px-16">
        <div className="w-full max-w-md">
          <div className="rounded-xl bg-white p-6 shadow-[0_1px_2px_rgba(0,0,0,0.05)] sm:p-10">
            <div className="mb-8 grid grid-cols-2 rounded-lg bg-[#eceef0] p-1">
              <Link
                to="/login"
                className={[
                  'rounded-md px-4 py-2 text-center text-sm font-semibold tracking-[0.01em] transition',
                  activeMode === 'login'
                    ? 'bg-white text-[#006e2f] shadow-sm'
                    : 'text-[#3d4a3d] hover:text-[#191c1e]',
                ].join(' ')}
              >
                Đăng Nhập
              </Link>
              <Link
                to="/register"
                className={[
                  'rounded-md px-4 py-2 text-center text-sm font-semibold tracking-[0.01em] transition',
                  activeMode === 'register'
                    ? 'bg-white text-[#006e2f] shadow-sm'
                    : 'text-[#3d4a3d] hover:text-[#191c1e]',
                ].join(' ')}
              >
                Đăng Ký
              </Link>
            </div>

            <div className="mb-8">
              <p className="text-sm font-bold uppercase tracking-[0.18em] text-[#006e2f]">
                {eyebrow}
              </p>
              <h3 className="mt-2 text-3xl font-bold tracking-[-0.01em] text-[#191c1e]">
                {title}
              </h3>
              <p className="mt-2 text-base leading-6 text-[#3d4a3d]">
                {description}
              </p>
              <p className="mt-3 text-sm leading-6 text-[#3d4a3d]">
                {switchText}{' '}
                <Link
                  to={switchHref}
                  className="font-bold text-[#006e2f] hover:text-[#004b1e]"
                >
                  {switchLabel}
                </Link>
              </p>
            </div>

            {children}

            <div className="mt-8">
              <div className="relative flex items-center justify-center">
                <div className="absolute inset-x-0 top-1/2 border-t border-[#bccbb9]" />
                <span className="relative bg-white px-4 text-xs font-bold uppercase tracking-[0.12em] text-[#3d4a3d]">
                  Hoặc tiếp tục với
                </span>
              </div>
              <div className="mt-6 grid grid-cols-2 gap-3">
                <button
                  type="button"
                  className="rounded-lg border border-[#bccbb9] px-4 py-2.5 text-sm font-medium text-[#191c1e] transition hover:bg-[#f2f4f6]"
                >
                  Google
                </button>
                <button
                  type="button"
                  className="rounded-lg border border-[#bccbb9] px-4 py-2.5 text-sm font-medium text-[#191c1e] transition hover:bg-[#f2f4f6]"
                >
                  Facebook
                </button>
              </div>
            </div>
          </div>

          <div className="mt-8 flex justify-center gap-6 text-xs font-bold text-[#3d4a3d]">
            <span>Tiếng Việt</span>
            <a href="mailto:support@example.com" className="hover:text-[#006e2f]">
              Hỗ trợ
            </a>
            <Link to="/" className="hover:text-[#006e2f]">
              Bảo mật
            </Link>
          </div>
        </div>
      </section>
    </div>
  )
}
