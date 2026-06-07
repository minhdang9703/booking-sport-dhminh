# Booking Sport MVP

Booking Sport là ứng dụng đặt sân thể thao gồm:

- Backend API bằng ASP.NET Core .NET 8.
- Frontend SPA bằng React + TypeScript + Vite.
- Database PostgreSQL.

MVP hiện tại hỗ trợ luồng đặt sân cho khách hàng và các màn quản trị cốt lõi cho admin: quản lý sân, người dùng, khung giá, booking, calendar và doanh thu.

## Tính Năng Hiện Có

### Người Dùng

- Đăng ký và đăng nhập bằng access token ngắn hạn + HttpOnly refresh cookie.
- Xem trang chủ và danh sách sân.
- Tìm kiếm/lọc sân theo từ khóa, loại sân và trạng thái.
- Xem chi tiết sân.
- Xem lịch trống của sân theo ngày.
- Chọn slot trống và checkout để tạo booking.
- Xem lịch sử booking cá nhân.
- Cập nhật thời gian tồn tại access token/phiên của chính tài khoản trong giới hạn hệ thống.

### Admin

- Đăng nhập khu vực quản trị.
- Xem dashboard tổng quan.
- Quản lý người dùng.
- Quản lý sân: tạo, cập nhật, xóa mềm.
- Quản lý khung giá (`PriceRule`): tạo, cập nhật, xóa, bật/tắt, chống trùng giờ giữa các rule đang bật.
- Quản lý booking: lọc danh sách, xem chi tiết, cập nhật trạng thái.
- Xem booking calendar.
- Xem dashboard doanh thu theo ngày, tuần, tháng.
- Cập nhật thời gian tồn tại access token/phiên mặc định toàn hệ thống.

## Công Nghệ

- Backend: .NET 8, ASP.NET Core Web API.
- ORM/Database: Entity Framework Core, PostgreSQL.
- Auth: JWT Bearer access token ngắn hạn, HttpOnly refresh cookie, role-based authorization.
- Frontend: React, TypeScript, Vite, React Router, Tailwind CSS.
- Backend tests: xUnit, FluentAssertions, SQLite in-memory, WebApplicationFactory, local PostgreSQL integration database, Testcontainers fallback.
- Frontend E2E: Playwright.

## Cấu Trúc Project

- `backend/BookingSport.Api`: ASP.NET Core API.
- `backend/BookingSport.Api/Controllers`: API controllers.
- `backend/BookingSport.Api/Services`: business logic.
- `backend/BookingSport.Api/DTOs`: request/response DTOs.
- `backend/BookingSport.Api/Entities`: EF Core entities.
- `backend/BookingSport.Api/Migrations`: EF Core migrations.
- `backend/BookingSport.Api.Tests`: unit tests và integration tests backend.
- `backend/scripts`: script/tool seed dữ liệu.
- `frontend`: ứng dụng React.
- `frontend/tests`: Playwright E2E tests.
- `docs/plan`: tài liệu phân tích trước triển khai.
- `docs/todo`: todo list cần duyệt.
- `docs/report`: report sau triển khai.

## Yêu Cầu Môi Trường

- .NET SDK 8 hoặc mới hơn.
- Node.js 20 hoặc mới hơn.
- PostgreSQL local.
- Docker Desktop/Docker Engine là tùy chọn, chỉ dùng làm fallback cho integration tests nếu không dùng local PostgreSQL.

## Chạy Local

### Backend

```bash
cd backend/BookingSport.Api
dotnet restore
dotnet build
dotnet ef database update
dotnet run
```

Theo `launchSettings.json`, API chạy ở:

- `http://localhost:5259`
- `https://localhost:7131`

Swagger được mở ở `/swagger` khi chạy môi trường Development.

### Frontend

```bash
cd frontend
npm.cmd install
npm.cmd run dev
```

Frontend mặc định chạy ở:

- `http://localhost:5173`

Build frontend:

```bash
npm.cmd run build
```

## Cấu Hình Môi Trường

### Backend

Config local nằm ở:

- `backend/BookingSport.Api/appsettings.Development.json`

Connection string hiện tại:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=booking_sport;Username=postgres;Password=root123"
  }
}
```

JWT, refresh cookie/session và CORS cũng được cấu hình trong `appsettings.Development.json`.

### Frontend

File env local:

- `frontend/.env`

Giá trị hiện dùng:

```env
VITE_API_BASE_URL=http://localhost:5259
```

File mẫu:

- `frontend/.env.example`

## Dữ Liệu Mẫu

Script seed/sample data nằm trong:

- `backend/scripts`

Tài khoản thường dùng cho dev/E2E gần đây:

- Admin: `admin@test.local / Test@123456`
- Customer: `customer@test.local / Test@123456`

Dữ liệu local có thể thay đổi theo từng lần seed/test. Kiểm tra script trong `backend/scripts` nếu cần dựng lại dữ liệu mẫu.

## API Chính

Các endpoint cần đăng nhập dùng header:

```http
Authorization: Bearer {accessToken}
```

Access token được cấp lại bằng HttpOnly cookie `bookingSport.refresh` qua `POST /api/auth/refresh`. Frontend cần gửi request với credentials/cookie enabled.

Admin endpoints yêu cầu role `Admin`.

### Auth

- `POST /api/auth/register`
- `POST /api/auth/login`
- `POST /api/auth/refresh`
- `POST /api/auth/logout`
- `GET /api/auth/me`
- `GET /api/auth/session-settings`
- `PUT /api/auth/session-settings`
- `GET /api/admin/auth-settings`
- `PUT /api/admin/auth-settings`

### Users

- `GET /api/users`
- `GET /api/users/{id}`
- `POST /api/users`
- `PUT /api/users/{id}`
- `DELETE /api/users/{id}`

### Courts

- `GET /api/courts`
- `GET /api/courts/{id}`
- `GET /api/courts/{courtId}/available-schedules?date=YYYY-MM-DD`
- `POST /api/courts`
- `PUT /api/courts/{id}`
- `DELETE /api/courts/{id}`

### Price Rules

- `GET /api/price-rules`
- `GET /api/price-rules/{id}`
- `POST /api/price-rules`
- `PUT /api/price-rules/{id}`
- `DELETE /api/price-rules/{id}`

### Bookings

- `POST /api/bookings`
- `GET /api/bookings/my`
- `GET /api/bookings`
- `GET /api/bookings/{id}`
- `PUT /api/bookings/{id}/status`

### Dashboard

- `GET /api/dashboard/revenue?fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD&period=Day|Week|Month`

### Health

- `GET /health`

Tham khảo thêm:

- `API_DOCS.md`

## Quy Tắc Nghiệp Vụ Chính

- Booking mới mặc định ở trạng thái `Pending`.
- Booking ở trạng thái `Pending` hoặc `Confirmed` sẽ chặn slot tương ứng.
- Booking ở trạng thái `Cancelled` hoặc `Completed` không chặn người dùng tạo booking mới cùng slot.
- Doanh thu dashboard chỉ tính booking có trạng thái `Completed`.
- Price rule đang bật không được overlap với price rule đang bật khác trong cùng ngày.
- Court bị xóa mềm hoặc không active sẽ không được dùng như sân khả dụng cho booking.

## Tests

### Backend

Chạy toàn bộ backend tests:

```bash
dotnet test backend/BookingSport.sln
```

Backend test project:

- `backend/BookingSport.Api.Tests`

Hiện có:

- 52 service unit tests dùng SQLite in-memory.
- 10 API integration flow tests dùng `WebApplicationFactory`.
- Tổng suite hiện tại: 62 tests pass, 0 failed, 0 skipped.

Integration tests ưu tiên local PostgreSQL test database:

```text
booking_sport_integration_tests
```

Factory đọc connection string từ `backend/BookingSport.Api/appsettings.Development.json`, đổi database từ `booking_sport` sang `booking_sport_integration_tests`, rồi reset/migrate database test trước mỗi flow. Database dev `booking_sport` không bị reset.

Nếu local PostgreSQL không dùng được, integration tests fallback sang PostgreSQL Testcontainers. Nếu cả local PostgreSQL và Docker đều không dùng được, integration tests sẽ skip có lý do rõ ràng.

### Frontend E2E

```bash
cd frontend
npm.cmd run test:e2e
```

Chạy E2E có mở browser:

```bash
npm.cmd run test:e2e:headed
```

Các luồng E2E chính:

- User login và filter danh sách sân.
- User xem lịch trống, chọn slot, checkout booking.
- User xem lịch sử booking.
- Admin CRUD court.
- Admin CRUD price rule.
- Admin update booking status.
- Admin xem booking calendar.
- Admin xem revenue dashboard.

## Ghi Chú

- Controllers giữ mỏng; business logic nằm trong Services.
- Request/response đi qua DTO, không expose entity trực tiếp.
- Database schema được quản lý bằng EF Core migrations.
- Các tài liệu phân tích/todo/report nằm trong `docs`.
