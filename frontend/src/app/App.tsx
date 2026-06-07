import { useEffect, useRef, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'

import avatarImage from '../assets/courts/avatar.png'
import logoImage from '../assets/courts/logo.png'
import {
  getAuthSession,
  isAdminUser,
  logout,
  refreshSession,
  subscribeAuthSession,
  type AuthResponse,
} from '../lib/authApi'

const publicNavItems = [
  { to: '/', label: 'Trang chủ' },
  { to: '/courts', label: 'Danh sách sân' },
  { to: '/bookings', label: 'Lịch sử đặt sân' },
]

export function App() {
  const location = useLocation()
  const navigate = useNavigate()
  const dropdownRef = useRef<HTMLDivElement>(null)
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false)
  const [authSession, setAuthSession] = useState<AuthResponse | null>(() =>
    getAuthSession(),
  )
  const isAuthPage =
    location.pathname === '/login' || location.pathname === '/register'

  useEffect(() => {
    function handlePointerDown(event: MouseEvent) {
      if (
        dropdownRef.current &&
        !dropdownRef.current.contains(event.target as Node)
      ) {
        setIsUserMenuOpen(false)
      }
    }

    document.addEventListener('mousedown', handlePointerDown)

    return () => document.removeEventListener('mousedown', handlePointerDown)
  }, [])

  useEffect(() => {
    const unsubscribe = subscribeAuthSession(setAuthSession)

    refreshSession().catch(() => {
      setAuthSession(null)
    })

    return unsubscribe
  }, [])

  if (isAuthPage) {
    return <Outlet />
  }

  const user = authSession?.user ?? null
  const navItems = isAdminUser(user)
    ? [...publicNavItems, { to: '/admin', label: 'Admin' }]
    : publicNavItems

  async function handleLogout() {
    const confirmed = window.confirm('Bạn có chắc chắn muốn đăng xuất?')

    if (!confirmed) {
      return
    }

    await logout()
    setIsUserMenuOpen(false)
    navigate('/login')
  }

  return (
    <div className="min-h-screen bg-[#f7f9fb] text-[#191c1e]">
      <header className="fixed inset-x-0 top-0 z-30 border-b border-[#e0e3e5] bg-[#f7f9fb]/95 backdrop-blur">
        <div className="mx-auto flex h-16 max-w-[1280px] items-center justify-between px-4 sm:px-6">
          <NavLink to="/" className="flex items-center gap-2">
            <img src={logoImage} alt="Booking Sport" className="h-10 w-10" />
            <span className="text-2xl font-bold text-[#191c1e]">
              AthletiBook
            </span>
          </NavLink>

          <nav className="hidden items-center gap-10 md:flex">
            {navItems.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                className={({ isActive }) =>
                  [
                    'border-b-2 px-0 py-1 text-sm font-medium tracking-[0.01em] transition',
                    isActive
                      ? 'border-[#006e2f] text-[#006e2f]'
                      : 'border-transparent text-[#3d4a3d] hover:text-[#191c1e]',
                  ].join(' ')
                }
              >
                {item.label}
              </NavLink>
            ))}
          </nav>

          <div className="flex items-center gap-4">
            {user ? (
              <>
                <button
                  type="button"
                  className="hidden text-[#3d4a3d] hover:text-[#006e2f] sm:block"
                  aria-label="Thông báo"
                >
                  🔔
                </button>
                <div ref={dropdownRef} className="relative">
                  <button
                    type="button"
                    onClick={() => setIsUserMenuOpen((current) => !current)}
                    className="flex items-center gap-2 rounded-full border border-transparent px-1 py-1 transition hover:border-[#bccbb9] hover:bg-white"
                    aria-expanded={isUserMenuOpen}
                    aria-haspopup="menu"
                  >
                    <img
                      src={avatarImage}
                      alt="User avatar"
                      className="h-8 w-8 rounded-full border border-[#bccbb9]"
                    />
                    <span className="hidden max-w-[140px] truncate text-sm font-semibold text-[#3d4a3d] sm:block">
                      {user.fullName}
                    </span>
                  </button>

                  {isUserMenuOpen ? (
                    <div
                      role="menu"
                      className="absolute right-0 mt-3 w-[240px] rounded-xl border border-[#bccbb9] bg-white p-3 shadow-xl"
                    >
                      <div className="border-b border-[#e0e3e5] pb-3">
                        <p className="truncate text-sm font-bold">
                          {user.fullName}
                        </p>
                        <p className="mt-1 truncate text-xs text-[#545f73]">
                          {user.email}
                        </p>
                      </div>
                      <NavLink
                        to="/bookings"
                        className="mt-3 block rounded-lg px-3 py-2 text-sm font-semibold text-[#3d4a3d] hover:bg-[#f7f9fb]"
                      >
                        Lịch sử đặt sân
                      </NavLink>
                      {isAdminUser(user) ? (
                        <NavLink
                          to="/admin"
                          className="block rounded-lg px-3 py-2 text-sm font-semibold text-[#3d4a3d] hover:bg-[#f7f9fb]"
                        >
                          Trang admin
                        </NavLink>
                      ) : null}
                      <button
                        type="button"
                        onClick={handleLogout}
                        className="mt-2 w-full rounded-lg bg-[#ffdad6] px-3 py-2 text-left text-sm font-bold text-[#ba1a1a] hover:bg-[#f5b4ab]"
                      >
                        Đăng xuất
                      </button>
                    </div>
                  ) : null}
                </div>
              </>
            ) : (
              <NavLink
                to="/login"
                className="rounded-lg bg-[#006e2f] px-5 py-2.5 text-sm font-bold text-white shadow-sm transition hover:bg-[#005321]"
              >
                Đăng nhập
              </NavLink>
            )}
          </div>
        </div>
      </header>
      <main>
        <Outlet />
      </main>
    </div>
  )
}
