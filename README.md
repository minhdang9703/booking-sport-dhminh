# Booking Sport

Booking Sport là hệ thống đặt lịch và quản lý sân thể thao, gồm backend API bằng ASP.NET Core và frontend trong thư mục `frontend`.

## Tính Năng Chính

- Xác thực người dùng bằng JWT, phân quyền `Customer` và `Admin`.
- Người dùng xem danh sách sân, xem lịch trống theo ngày, đặt sân theo khung giờ và xem lịch sử đặt sân.
- Admin quản lý sân và khung giờ sân.
- Admin quản lý lịch đặt, lọc booking và xác nhận/hủy/hoàn tất booking.
- Admin xem dashboard doanh thu theo ngày, tuần hoặc tháng.

## Kiến Trúc Và Công Nghệ

- Backend: .NET 8, ASP.NET Core Web API.
- Database: PostgreSQL.
- ORM: Entity Framework Core, migrations trong `backend/BookingSport.Api/Migrations`.
- Auth: JWT Bearer token.
- Frontend: thư mục `frontend`.

## Yêu Cầu

- .NET SDK 8.
- PostgreSQL hoặc Docker/Docker Compose.
- EF Core CLI nếu cần chạy migration thủ công.

## Chạy Nhanh Local

Chuyển vào thư mục backend API:

```bash
cd backend/BookingSport.Api
```

Restore, build và chạy migration:

```bash
dotnet restore
dotnet build
dotnet ef database update
```

Chạy API:

```bash
dotnet run
```

Hoặc chạy hạ tầng bằng Docker Compose từ thư mục `backend`:

```bash
docker compose up -d
```

Tắt Docker Compose:

```bash
docker compose down
```

## Seed Dữ Liệu

Các script seed nằm trong `backend/scripts`:

- `seed-auth-users.sql` / `seed-auth-users.sh`: tạo user mẫu cho xác thực.
- `seed-current-data.sql` / `seed-current-data.sh`: tạo dữ liệu mẫu cho sân và lịch.

Trên Windows, có thể chạy file `.sql` bằng database client hoặc chạy `.sh` qua WSL/Git Bash.

## API MVP

Base URL local thường là `http://localhost:5000`, tùy cấu hình trong `launchSettings.json`.

Các nhóm API chính:

- Auth: `POST /api/auth/register`, `POST /api/auth/login`, `GET /api/auth/me`.
- Courts: `GET /api/courts`, `GET /api/courts/{id}`, `POST /api/courts`, `PUT /api/courts/{id}`, `DELETE /api/courts/{id}`.
- Court schedules: `GET /api/court-schedules`, `GET /api/court-schedules/{id}`, `POST /api/court-schedules`, `PUT /api/court-schedules/{id}`, `DELETE /api/court-schedules/{id}`.
- Availability: `GET /api/courts/{courtId}/available-schedules?date=YYYY-MM-DD`.
- Bookings: `POST /api/bookings`, `GET /api/bookings/my`, `GET /api/bookings/{id}`.
- Admin bookings: `GET /api/bookings`, `PUT /api/bookings/{id}/status`.
- Dashboard: `GET /api/dashboard/revenue?fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD&period=Day|Week|Month`.

Chi tiết request/response xem [API_DOCS.md](API_DOCS.md).

## Kiểm Thử

Chạy test nếu có test project:

```bash
dotnet test
```

## Cấu Trúc Chính

- `backend/BookingSport.Api/Controllers`: API controllers.
- `backend/BookingSport.Api/Services`: business logic.
- `backend/BookingSport.Api/DTOs`: request/response DTOs.
- `backend/BookingSport.Api/Entities`: EF Core entities.
- `backend/BookingSport.Api/Migrations`: EF Core migrations.

## Frontend React

Frontend nằm trong thư mục `frontend`, dùng Vite + React + TypeScript + Tailwind CSS.

Chạy lần đầu:

```bash
cd frontend
npm.cmd install
```

Chạy dev server:

```bash
npm.cmd run dev
```

Build kiểm tra:

```bash
npm.cmd run build
```

Config API local nằm trong `frontend/.env.example`:

```env
VITE_API_BASE_URL=http://localhost:5000
```

Trên PowerShell, nếu `npm` bị chặn do execution policy, dùng `npm.cmd`.
