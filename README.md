# Booking Sport MVP

Booking Sport là ứng dụng đặt sân thể thao gồm:
- Backend API bằng ASP.NET Core
- Frontend SPA bằng React + Vite
- Database PostgreSQL

MVP hiện tại đã hỗ trợ luồng đặt sân cơ bản cho người dùng và các màn quản trị cốt lõi cho admin.

## Tính năng MVP

### User
- Xem danh sách sân
- Tìm kiếm và lọc sân theo từ khóa, môn thể thao, cụm sân, trạng thái, mức giá ước tính
- Xem chi tiết sân
- Xem lịch trống theo ngày
- Chọn khung giờ và đặt sân
- Xem lịch sử đặt sân

### Admin
- Đăng nhập vào khu vực quản trị
- Quản lý sân: tạo, cập nhật, xóa mềm
- Quản lý booking: lọc theo ngày, trạng thái, từ khóa
- Xác nhận, hủy, hoàn tất booking
- Quản lý lịch đặt theo calendar
- Xem dashboard doanh thu theo ngày, tuần, tháng

## Công nghệ

- Backend: .NET 8, ASP.NET Core Web API
- Frontend: React, TypeScript, Vite, React Router, Tailwind CSS
- Database: PostgreSQL
- ORM: Entity Framework Core
- Auth: JWT Bearer
- E2E: Playwright

## Cấu trúc chính

- `backend/BookingSport.Api`: ASP.NET Core API
- `backend/BookingSport.Api/Controllers`: API controllers
- `backend/BookingSport.Api/Services`: business logic
- `backend/BookingSport.Api/DTOs`: request/response DTOs
- `backend/BookingSport.Api/Entities`: EF Core entities
- `backend/BookingSport.Api/Migrations`: EF Core migrations
- `backend/scripts`: script seed dữ liệu
- `frontend`: ứng dụng React
- `frontend/tests`: Playwright e2e tests

## Yêu cầu môi trường

- .NET SDK 8
- Node.js 20+
- PostgreSQL

## Chạy local

### 1. Backend

Di chuyển vào thư mục API:

```bash
cd backend/BookingSport.Api
```

Restore, build, chạy migration:

```bash
dotnet restore
dotnet build
dotnet ef database update
```

Chạy API:

```bash
dotnet run
```

`launchSettings.json` hiện khai báo API ở:
- `http://localhost:5259`
- `https://localhost:7131`

Trong môi trường dev hiện tại của repo, frontend đang được cấu hình gọi:
- `http://localhost:5029`

Nếu bạn chạy backend ở cổng khác, hãy cập nhật lại biến môi trường frontend cho khớp.

### 2. Frontend

Di chuyển vào thư mục frontend:

```bash
cd frontend
```

Cài dependencies:

```bash
npm.cmd install
```

Tạo file env nếu cần:

```bash
copy .env.example .env
```

Chạy dev server:

```bash
npm.cmd run dev
```

Build kiểm tra:

```bash
npm.cmd run build
```

Frontend mặc định chạy ở:
- `http://localhost:5173`

## Cấu hình môi trường

### Backend

Connection string local hiện nằm trong:
- `backend/BookingSport.Api/appsettings.Development.json`

Ví dụ:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=booking_sport;Username=postgres;Password=root123"
  }
}
```

### Frontend

File env mẫu:
- `frontend/.env.example`

File env local hiện tại:
- `frontend/.env`

Ví dụ đang dùng:

```env
VITE_API_BASE_URL=http://localhost:5029
```

## Seed account dùng cho dev/e2e

Tài khoản hiện được dùng để test local:

- Customer: `user01 / user123`
- Admin: `admin / admin123`

Script seed auth nằm tại:
- `backend/scripts/seed-auth-users.sql`

Lưu ý: dữ liệu DB local hiện có thể đã được cập nhật trực tiếp để khớp với 2 tài khoản trên.

## API chính của MVP

### Auth
- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me`

### Courts
- `GET /api/courts`
- `GET /api/courts/{id}`
- `GET /api/courts/{courtId}/available-schedules?date=YYYY-MM-DD`
- `POST /api/courts`
- `PUT /api/courts/{id}`
- `DELETE /api/courts/{id}`

### Court schedules
- `GET /api/court-schedules`
- `GET /api/court-schedules/{id}`
- `POST /api/court-schedules`
- `PUT /api/court-schedules/{id}`
- `DELETE /api/court-schedules/{id}`

### Bookings
- `POST /api/bookings`
- `GET /api/bookings/my`
- `GET /api/bookings`
- `GET /api/bookings/{id}`
- `PUT /api/bookings/{id}/status`

### Dashboard
- `GET /api/dashboard/revenue?fromDate=YYYY-MM-DD&toDate=YYYY-MM-DD&period=Day|Week|Month`

Tham khảo thêm:
- `API_DOCS.md`

## E2E hiện có

Playwright đã được cấu hình trong frontend.

Chạy toàn bộ e2e:

```bash
cd frontend
npm.cmd run test:e2e
```

Chạy e2e có mở browser:

```bash
npm.cmd run test:e2e:headed
```

Các luồng e2e hiện đang được bao phủ:
- User vào chi tiết sân và chuyển sang checkout
- User đăng ký và tạo booking thành công
- User đăng nhập và search/filter sân
- User xem sân, xem lịch trống, đặt sân, xem lịch sử
- Admin quản lý sân
- Admin quản lý booking
- Admin cập nhật booking từ calendar
- Admin xem doanh thu theo ngày, tuần, tháng

## Ghi chú hiện tại của MVP

- Luồng booking thật đã bỏ fallback demo để tránh sang checkout với dữ liệu giả.
- Sau khi đặt sân thành công, user được redirect về trang chủ.
- Bộ lọc ở màn user hiện dùng dữ liệu thật từ API và client-side filtering cho phần giá ước tính.
- Một số tài liệu cũ trong repo có thể vẫn đang dùng cổng `5000`; môi trường dev hiện tại đang dùng `5029` cho API frontend.
