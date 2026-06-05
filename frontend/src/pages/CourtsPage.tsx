import { type FormEvent, useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'

import badmintonImage from '../assets/courts/court-badminton.png'
import basketballImage from '../assets/courts/court-basketball.png'
import footballImage from '../assets/courts/court-football.png'
import swimmingImage from '../assets/courts/court-swimming.png'
import tennisImage from '../assets/courts/court-tennis.png'
import volleyballImage from '../assets/courts/court-volleyball.png'
import { getCourts, type Court } from '../lib/courtsApi'

const sportOptions = ['Tất cả', 'Football', 'Tennis', 'Basketball', 'Badminton']
const statusOptions = ['Tất cả', 'Đang mở', 'Tạm đóng', 'Bảo trì']
const priceOptions = ['Mọi mức giá', 'Dưới 200k', '200k - 300k', 'Trên 300k']

const imageBySport = [
  { keyword: 'bóng đá', image: footballImage },
  { keyword: 'football', image: footballImage },
  { keyword: 'tennis', image: tennisImage },
  { keyword: 'bóng rổ', image: basketballImage },
  { keyword: 'basketball', image: basketballImage },
  { keyword: 'cầu lông', image: badmintonImage },
  { keyword: 'badminton', image: badmintonImage },
  { keyword: 'bóng chuyền', image: volleyballImage },
  { keyword: 'volleyball', image: volleyballImage },
  { keyword: 'bơi', image: swimmingImage },
  { keyword: 'swimming', image: swimmingImage },
]

function getCourtImage(court: Court) {
  const courtType = court.courtType.toLowerCase()
  const match = imageBySport.find((item) => courtType.includes(item.keyword))

  return match?.image ?? footballImage
}

function getStatusLabel(status: Court['status']) {
  if (status === 1) {
    return 'Đang mở'
  }

  if (status === 3) {
    return 'Bảo trì'
  }

  return 'Tạm đóng'
}

function getEstimatedPrice(court: Court, index: number) {
  const courtType = court.courtType.toLowerCase()

  if (courtType.includes('tennis')) {
    return 280
  }

  if (courtType.includes('bóng rổ') || courtType.includes('basketball')) {
    return 250
  }

  if (courtType.includes('cầu lông') || courtType.includes('badminton')) {
    return 180
  }

  return [300, 280, 250, 220, 180, 160][index % 6]
}

function matchesPriceFilter(price: number, selectedPrice: string) {
  if (selectedPrice === 'Dưới 200k') {
    return price < 200
  }

  if (selectedPrice === '200k - 300k') {
    return price >= 200 && price <= 300
  }

  if (selectedPrice === 'Trên 300k') {
    return price > 300
  }

  return true
}

export function CourtsPage() {
  const [keywordInput, setKeywordInput] = useState('')
  const [keyword, setKeyword] = useState('')
  const [selectedSport, setSelectedSport] = useState('Tất cả')
  const [selectedVenue, setSelectedVenue] = useState('Tất cả')
  const [selectedStatus, setSelectedStatus] = useState('Tất cả')
  const [selectedPrice, setSelectedPrice] = useState(priceOptions[0])
  const [courts, setCourts] = useState<Court[]>([])
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState('')

  async function loadCourts(searchKeyword = keyword) {
    setIsLoading(true)
    setError('')

    try {
      const response = await getCourts({
        keyword: searchKeyword.trim() || undefined,
        status: 'Active',
      })
      setCourts(response)
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Không tải được danh sách sân từ API.',
      )
      setCourts([])
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void loadCourts('')
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  const venueOptions = useMemo(() => {
    const venues = Array.from(new Set(courts.map((court) => court.courtType))).sort()
    return ['Tất cả', ...venues]
  }, [courts])

  function handleSearch(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setKeyword(keywordInput)
    void loadCourts(keywordInput)
  }

  function handleResetFilters() {
    setKeywordInput('')
    setKeyword('')
    setSelectedSport('Tất cả')
    setSelectedVenue('Tất cả')
    setSelectedStatus('Tất cả')
    setSelectedPrice(priceOptions[0])
    void loadCourts('')
  }

  const displayedCourts = useMemo(() => {
    return courts.filter((court, index) => {
      const estimatedPrice = getEstimatedPrice(court, index)
      const matchesKeyword =
        !keyword.trim() ||
        `${court.name} ${court.courtType}`
          .toLowerCase()
          .includes(keyword.trim().toLowerCase())
      const matchesSport =
        selectedSport === 'Tất cả' || court.courtType === selectedSport
      const matchesVenue =
        selectedVenue === 'Tất cả' || court.courtType === selectedVenue
      const matchesStatus =
        selectedStatus === 'Tất cả' || getStatusLabel(court.status) === selectedStatus
      const matchesPrice = matchesPriceFilter(estimatedPrice, selectedPrice)

      return (
        matchesKeyword &&
        matchesSport &&
        matchesVenue &&
        matchesStatus &&
        matchesPrice
      )
    })
  }, [courts, keyword, selectedPrice, selectedSport, selectedStatus, selectedVenue])

  return (
    <div className="min-h-screen bg-[#f7f9fb] text-[#191c1e]">
      <section className="mx-auto max-w-[1280px] px-4 pb-12 pt-24 sm:px-6">
        <form
          onSubmit={handleSearch}
          className="rounded-xl border border-[#bccbb9] bg-white p-6 shadow-[0_1px_2px_rgba(0,0,0,0.05)]"
        >
          <div className="grid gap-4 lg:grid-cols-[2fr_1fr_1fr_1fr_1fr] lg:items-end">
            <FilterField label="Tìm kiếm sân">
              <div className="relative">
                <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[#3d4a3d]">
                  ⌕
                </span>
                <input
                  value={keywordInput}
                  onChange={(event) => setKeywordInput(event.target.value)}
                  placeholder="Nhập tên sân hoặc địa điểm..."
                  className="h-12 w-full rounded-lg bg-[#eceef0] px-10 text-base text-[#191c1e] outline-none transition placeholder:text-[#6b7280] focus:ring-2 focus:ring-[#006e2f]/30"
                />
              </div>
            </FilterField>

            <FilterField label="Môn thể thao">
              <SelectField
                value={selectedSport}
                options={sportOptions}
                onChange={setSelectedSport}
              />
            </FilterField>

            <FilterField label="Cụm sân">
              <SelectField
                value={selectedVenue}
                options={venueOptions}
                onChange={setSelectedVenue}
              />
            </FilterField>

            <FilterField label="Trạng thái">
              <SelectField
                value={selectedStatus}
                options={statusOptions}
                onChange={setSelectedStatus}
              />
            </FilterField>

            <FilterField label="Giá thuê">
              <SelectField
                value={selectedPrice}
                options={priceOptions}
                onChange={setSelectedPrice}
              />
            </FilterField>
          </div>

          <div className="mt-4 flex flex-wrap items-center gap-3">
            <button
              type="submit"
              className="rounded-lg bg-[#006e2f] px-5 py-3 text-sm font-semibold text-white transition hover:bg-[#005321]"
            >
              Áp dụng bộ lọc
            </button>
            <button
              type="button"
              onClick={handleResetFilters}
              className="rounded-lg border border-[#bccbb9] bg-white px-5 py-3 text-sm font-semibold text-[#3d4a3d] transition hover:bg-[#f2f4f6]"
            >
              Xóa bộ lọc
            </button>
            <span className="text-sm text-[#3d4a3d]">
              Hiển thị <strong>{displayedCourts.length}</strong> sân phù hợp
            </span>
          </div>
        </form>

        <div className="flex flex-col gap-4 pb-6 pt-10 sm:flex-row sm:items-end sm:justify-between">
          <div>
            <h1 className="text-3xl font-bold tracking-[-0.01em]">
              Danh sách sân thể thao
            </h1>
            <p className="mt-2 text-base text-[#3d4a3d]">
              Khám phá và đặt sân nhanh chóng
            </p>
          </div>

          <div className="flex gap-1 rounded-lg bg-white p-1 shadow-sm">
            <button className="rounded-md bg-[#eceef0] px-3 py-2 text-[#006e2f]">
              ▦
            </button>
            <button className="rounded-md px-3 py-2 text-[#3d4a3d]">☰</button>
          </div>
        </div>

        {error ? (
          <div className="mb-6 rounded-xl border border-[#ffdad6] bg-[#ffdad6] px-4 py-3 text-sm font-medium text-[#93000a]">
            {error}
          </div>
        ) : null}

        {isLoading ? (
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {Array.from({ length: 4 }).map((_, index) => (
              <div
                key={index}
                className="h-[340px] animate-pulse rounded-xl border border-[#bccbb9] bg-white shadow-[0_4px_12px_rgba(30,41,59,0.05)]"
              />
            ))}
          </div>
        ) : (
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
            {displayedCourts.map((court, index) => (
              <CourtCard key={court.id} court={court} index={index} />
            ))}
          </div>
        )}

        {!isLoading && displayedCourts.length === 0 ? (
          <div className="rounded-xl border border-[#bccbb9] bg-white p-10 text-center text-[#3d4a3d]">
            {error
              ? 'Chưa thể tải danh sách sân. Vui lòng thử lại sau.'
              : 'Không tìm thấy sân phù hợp với bộ lọc hiện tại.'}
          </div>
        ) : null}
      </section>
    </div>
  )
}

function FilterField({
  label,
  children,
}: {
  label: string
  children: React.ReactNode
}) {
  return (
    <label className="block">
      <span className="mb-1 block text-xs font-bold uppercase tracking-[0.05em] text-[#3d4a3d]">
        {label}
      </span>
      {children}
    </label>
  )
}

function SelectField({
  value,
  options,
  onChange,
}: {
  value: string
  options: string[]
  onChange: (value: string) => void
}) {
  return (
    <select
      value={value}
      onChange={(event) => onChange(event.target.value)}
      className="h-12 w-full rounded-lg bg-[#eceef0] px-4 text-base text-[#191c1e] outline-none transition focus:ring-2 focus:ring-[#006e2f]/30"
    >
      {options.map((option) => (
        <option key={option} value={option}>
          {option}
        </option>
      ))}
    </select>
  )
}

function CourtCard({ court, index }: { court: Court; index: number }) {
  const price = getEstimatedPrice(court, index)
  const isActive = court.status === 1

  return (
    <article className="overflow-hidden rounded-xl border border-[#bccbb9] bg-white shadow-[0_4px_12px_rgba(30,41,59,0.05)] transition hover:-translate-y-1 hover:shadow-[0_8px_20px_rgba(30,41,59,0.08)]">
      <div className="relative h-48 overflow-hidden">
        <Link to={`/courts/${court.id}`} className="block h-full">
          <img
            src={getCourtImage(court)}
            alt={court.name}
            className="h-full w-full object-cover"
          />
        </Link>
        <span
          className={[
            'absolute left-3 top-3 rounded-full px-3 py-1 text-xs font-bold backdrop-blur',
            isActive
              ? 'bg-[#006e2f]/90 text-white'
              : 'bg-[#ef9900] text-[#5c3800]',
          ].join(' ')}
        >
          {court.courtType || 'Thể thao'}
        </span>
        <button
          type="button"
          className="absolute right-3 top-3 flex h-8 w-8 items-center justify-center rounded-full bg-white/80 text-[#ba1a1a] shadow-sm backdrop-blur"
          aria-label="Yêu thích"
        >
          ♥
        </button>
      </div>

      <div className="p-6">
        <div className="flex items-start justify-between gap-3">
          <Link
            to={`/courts/${court.id}`}
            className="line-clamp-2 text-sm font-semibold tracking-[0.01em] hover:text-[#006e2f]"
          >
            {court.name}
          </Link>
          <div className="flex items-center gap-1 text-xs font-bold text-[#855300]">
            ★ <span>4.{8 - (index % 3)}</span>
          </div>
        </div>

        <p className="mt-3 line-clamp-1 text-sm text-[#3d4a3d]">
          {court.courtType || 'Sân thể thao'}
        </p>

        <p className="mt-3 line-clamp-2 min-h-10 text-sm text-[#3d4a3d]">
          {court.courtType || 'Sân thể thao đang sẵn sàng cho lịch đặt mới.'}
        </p>

        <div className="mt-6 flex items-center justify-between gap-4">
          <div>
            <div className="text-2xl font-semibold text-[#006e2f]">
              {price}k
            </div>
            <div className="text-sm text-[#3d4a3d]">/giờ</div>
          </div>
          {isActive ? (
            <Link
              to={`/courts/${court.id}`}
              className="rounded-lg bg-[#006e2f] px-4 py-2 text-sm font-semibold tracking-[0.01em] text-white transition hover:bg-[#005321]"
            >
              Đặt ngay
            </Link>
          ) : (
            <button
              type="button"
              disabled
              className="rounded-lg bg-[#bccbb9] px-4 py-2 text-sm font-semibold tracking-[0.01em] text-white"
            >
              {getStatusLabel(court.status)}
            </button>
          )}
        </div>
      </div>
    </article>
  )
}
