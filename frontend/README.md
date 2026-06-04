# Booking Sport Frontend

Ứng dụng frontend cho Booking Sport MVP, xây bằng React + Vite.

## Stack

- React
- TypeScript
- Vite
- React Router
- Tailwind CSS
- Playwright

## Chạy local

```bash
npm.cmd install
```

Tạo env local:

```bash
copy .env.example .env
```

Chạy dev server:

```bash
npm.cmd run dev
```

Build:

```bash
npm.cmd run build
```

Frontend mặc định chạy ở:
- `http://localhost:5173`

## Biến môi trường

Mẫu:

```env
VITE_API_BASE_URL=http://localhost:5000
```

Trong môi trường dev hiện tại của repo đang dùng:

```env
VITE_API_BASE_URL=http://localhost:5029
```

## Màn hình chính của MVP

- `/`
- `/login`
- `/register`
- `/courts/:courtId`
- `/courts/:courtId/availability`
- `/checkout`
- `/bookings`
- `/admin`
- `/admin/dashboard`
- `/admin/courts`
- `/admin/bookings`
- `/admin/bookings/calendar`
- `/admin/revenue`
- `/admin/users`

## E2E

Chạy toàn bộ:

```bash
npm.cmd run test:e2e
```

Chạy có mở browser:

```bash
npm.cmd run test:e2e:headed
```

Các file test hiện có:
- `tests/booking-flow.spec.ts`
- `tests/role-workflows.spec.ts`

## Lưu ý

- Trên Windows PowerShell, nên dùng `npm.cmd` để tránh lỗi execution policy với `npm.ps1`.
- Frontend hiện kỳ vọng backend đã chạy sẵn và database đã migrate xong.
