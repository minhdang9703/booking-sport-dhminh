
# Booking Sport

Dự án "Booking Sport" là một hệ thống đặt lịch và quản lý sân thể thao, gồm backend API bằng ASP.NET Core và một frontend (thư mục `frontend`).

## Tính năng chính

- Quản lý người dùng, vai trò và xác thực.
- Quản lý sân (courts) và khung giờ (schedules).
- Tạo, huỷ và theo dõi lịch đặt.
- Quản lý thanh toán và trạng thái đơn đặt.
- Bảng điều khiển thống kê (dashboard).

## Kiến trúc & Công nghệ

- Backend: .NET 8 (ASP.NET Core Web API)
- ORM: Entity Framework Core (migrations có trong `backend/BookingSport.Api/Migrations`)
- Cơ sở dữ liệu: sử dụng migration và script SQL trong thư mục `scripts/` để tạo/seed dữ liệu
- Frontend: thư mục `frontend` (ứng dụng client — xem README riêng nếu có)

## Yêu cầu

- .NET SDK 8
- Docker & Docker Compose (tùy chọn)
- PostgreSQL hoặc DB tương thích (theo cấu hình trong `appsettings.*`)

> Ghi chú: trên Windows, các script `.sh` trong `scripts/` có thể chạy trong WSL hoặc bằng Bash.

## Chạy nhanh (Local)

1. Mở terminal, chuyển vào thư mục backend:

```bash
cd backend/BookingSport.Api
```

2. Cài dependencies và build:

```bash
dotnet restore
dotnet build
```

3. Áp migration và cập nhật database:

```bash
dotnet ef database update
```

4. Chạy ứng dụng API:

```bash
dotnet run
```

Hoặc sử dụng Docker Compose (từ thư mục `backend`):

```bash
docker compose up -d
```

## Seed dữ liệu

Các script seed nằm trong thư mục `scripts/`:

- `seed-auth-users.sql` / `seed-auth-users.sh` — tạo người dùng mẫu cho xác thực.
- `seed-current-data.sql` / `seed-current-data.sh` — dữ liệu mẫu hiện trạng sân, lịch.

Trên Windows có thể chạy các file `.sql` trực tiếp bằng client DB hoặc chạy `.sh` qua WSL.

## API

Controllers chính nằm trong `backend/BookingSport.Api/Controllers` (ví dụ: `AuthController`, `BookingsController`, `CourtsController`).

Base URL khi chạy local thường là `http://localhost:5000` (hoặc theo cấu hình `launchSettings.json`).

## Thử nghiệm

Chạy test project (nếu có):

```bash
dotnet test
```

## Đóng góp

## License
