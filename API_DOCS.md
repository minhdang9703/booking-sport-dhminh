# API Documentation - Booking Sport

Base URL local: `http://localhost:5000` hoặc theo `backend/BookingSport.Api/Properties/launchSettings.json`.

Các endpoint yêu cầu đăng nhập dùng header:

```http
Authorization: Bearer {accessToken}
```

Admin endpoint yêu cầu JWT có role `Admin`.

Ghi chú enum: query/body có thể dùng tên enum như `Confirmed`, `Wednesday`, `Week` nếu model binding nhận được; response hiện trả enum dạng số vì JSON config chưa bật string enum converter.

Mapping enum thường dùng:

- `BookingStatus`: `Pending = 1`, `Confirmed = 2`, `Cancelled = 3`, `Completed = 4`.
- `RevenuePeriod`: `Day = 1`, `Week = 2`, `Month = 3`.
- `DayOfWeek`: `Sunday = 0`, `Monday = 1`, `Tuesday = 2`, `Wednesday = 3`, `Thursday = 4`, `Friday = 5`, `Saturday = 6`.

## Auth

### POST /api/auth/register

Đăng ký user mới.

Body:

```json
{
  "email": "user@example.com",
  "password": "P@ssw0rd",
  "fullName": "Nguyen Van A"
}
```

Response: `200 OK` với `AuthResponse` gồm token và thông tin user.

### POST /api/auth/login

Đăng nhập và nhận JWT.

Body:

```json
{
  "email": "user@example.com",
  "password": "P@ssw0rd"
}
```

Response: `200 OK` với `AuthResponse`.

### GET /api/auth/me

Lấy thông tin user hiện tại.

Yêu cầu: đăng nhập.

Response: `200 OK` với `CurrentUserResponse`.

## Courts

### GET /api/courts

Lấy danh sách sân.

Query:

- `venueId`: lọc theo cụm sân.
- `sportId`: lọc theo môn thể thao.
- `status`: lọc theo trạng thái sân.
- `keyword`: tìm theo tên sân.

Response: `200 OK` với danh sách `CourtResponse`.

### GET /api/courts/{id}

Lấy chi tiết sân.

Response: `200 OK` với `CourtResponse`, hoặc `404 Not Found`.

### GET /api/courts/{courtId}/available-schedules?date=YYYY-MM-DD

Lấy các khung giờ còn trống của một sân theo ngày.

Response: `200 OK` với danh sách:

```json
[
  {
    "scheduleId": "00000000-0000-0000-0000-000000000000",
    "courtId": "00000000-0000-0000-0000-000000000000",
    "courtName": "Sân 1",
    "date": "2026-06-10",
    "dayOfWeek": 3,
    "startTime": "18:00:00",
    "endTime": "19:00:00",
    "price": 200000,
    "isAvailable": true
  }
]
```

### POST /api/courts

Tạo sân mới.

Yêu cầu: admin.

Body:

```json
{
  "venueId": "00000000-0000-0000-0000-000000000000",
  "sportId": "00000000-0000-0000-0000-000000000000",
  "name": "Sân 1",
  "status": "Active",
  "description": "Sân cỏ nhân tạo"
}
```

Response: `201 Created` với `CourtResponse`.

### PUT /api/courts/{id}

Cập nhật sân.

Yêu cầu: admin.

Body:

```json
{
  "name": "Sân 1",
  "status": "Active",
  "description": "Sân cỏ nhân tạo"
}
```

Response: `200 OK` với `CourtResponse`.

### DELETE /api/courts/{id}

Xóa mềm sân.

Yêu cầu: admin.

Response: `204 No Content`.

## Court Schedules

### GET /api/court-schedules

Lấy danh sách khung giờ sân.

Query:

- `courtId`: lọc theo sân.
- `dayOfWeek`: lọc theo ngày trong tuần, ví dụ `Monday`.
- `isAvailable`: lọc khung giờ đang bật/tắt.

Response: `200 OK` với danh sách `CourtScheduleResponse`.

### GET /api/court-schedules/{id}

Lấy chi tiết khung giờ sân.

Response: `200 OK` với `CourtScheduleResponse`, hoặc `404 Not Found`.

### POST /api/court-schedules

Tạo khung giờ sân.

Yêu cầu: admin.

Body:

```json
{
  "courtId": "00000000-0000-0000-0000-000000000000",
  "dayOfWeek": "Wednesday",
  "startTime": "18:00:00",
  "endTime": "19:00:00",
  "price": 200000,
  "isAvailable": true
}
```

Response: `201 Created` với `CourtScheduleResponse`.

### PUT /api/court-schedules/{id}

Cập nhật khung giờ sân.

Yêu cầu: admin.

Body:

```json
{
  "dayOfWeek": "Wednesday",
  "startTime": "18:00:00",
  "endTime": "19:00:00",
  "price": 220000,
  "isAvailable": true
}
```

Response: `200 OK` với `CourtScheduleResponse`.

### DELETE /api/court-schedules/{id}

Xóa mềm khung giờ sân.

Yêu cầu: admin.

Response: `204 No Content`.

## Bookings

### POST /api/bookings

Tạo booking mới cho user hiện tại.

Yêu cầu: đăng nhập.

Body:

```json
{
  "courtScheduleId": "00000000-0000-0000-0000-000000000000",
  "bookingDate": "2026-06-10",
  "note": "Team tập luyện"
}
```

Response: `201 Created` với `BookingResponse`.

Ghi chú:

- `bookingDate` phải khớp `dayOfWeek` của `courtScheduleId`.
- Booking trùng khung giờ đang `Pending` hoặc `Confirmed` sẽ trả `409 Conflict`.
- Booking mới có trạng thái mặc định `Pending`.

### GET /api/bookings/my

Lấy lịch sử booking của user hiện tại.

Yêu cầu: đăng nhập.

Response: `200 OK` với danh sách `BookingResponse`.

### GET /api/bookings/{id}

Lấy chi tiết booking.

Yêu cầu: đăng nhập.

Quyền truy cập:

- User chỉ xem được booking của chính mình.
- Admin xem được mọi booking.

Response: `200 OK` với `BookingResponse`, hoặc `404 Not Found`.

### GET /api/bookings

Lấy danh sách booking để admin quản lý.

Yêu cầu: admin.

Query:

- `fromDate`: lọc từ ngày booking.
- `toDate`: lọc đến ngày booking.
- `courtId`: lọc theo sân.
- `status`: lọc theo trạng thái `Pending`, `Confirmed`, `Cancelled`, `Completed`.

Response: `200 OK` với danh sách `BookingResponse`.

### PUT /api/bookings/{id}/status

Cập nhật trạng thái booking.

Yêu cầu: admin.

Body:

```json
{
  "status": "Confirmed"
}
```

Các status hỗ trợ:

- `Pending`
- `Confirmed`
- `Cancelled`
- `Completed`

Response: `200 OK` với `BookingResponse`.

## Dashboard / Revenue

### GET /api/dashboard/revenue

Xem dashboard doanh thu theo khoảng thời gian.

Yêu cầu: admin.

Query:

- `fromDate`: ngày bắt đầu, mặc định là 30 ngày trước ngày hiện tại.
- `toDate`: ngày kết thúc, mặc định là ngày hiện tại.
- `period`: cách gom nhóm doanh thu, nhận `Day`, `Week` hoặc `Month`. Mặc định là `Day`.

Ví dụ:

```http
GET /api/dashboard/revenue?fromDate=2026-06-01&toDate=2026-06-30&period=Week
```

Response: `200 OK` với `RevenueDashboardResponse`:

```json
{
  "fromDate": "2026-06-01",
  "toDate": "2026-06-30",
  "totalRevenue": 1200000,
  "completedBookingCount": 6,
  "averageBookingValue": 200000,
  "period": 2,
  "revenuePoints": [
    {
      "label": "2026-W23",
      "fromDate": "2026-06-01",
      "toDate": "2026-06-07",
      "revenue": 400000,
      "completedBookingCount": 2
    }
  ],
  "dailyRevenue": [
    {
      "date": "2026-06-01",
      "revenue": 200000,
      "completedBookingCount": 1
    }
  ]
}
```

Doanh thu chỉ tính booking có trạng thái `Completed`.

## Health Check

### GET /health

Kiểm tra API đang chạy.

Response:

```json
{
  "status": "ok"
}
```

## Mã Lỗi Phổ Biến

- `400 Bad Request`: dữ liệu đầu vào không hợp lệ.
- `401 Unauthorized`: thiếu token hoặc token không hợp lệ.
- `403 Forbidden`: không đủ quyền truy cập.
- `404 Not Found`: resource không tồn tại.
- `409 Conflict`: xung đột dữ liệu, ví dụ trùng booking hoặc trùng khung giờ.

## Ghi Chú

- Controllers nằm trong `backend/BookingSport.Api/Controllers`.
- DTOs nằm trong `backend/BookingSport.Api/DTOs`.
- Business logic nằm trong `backend/BookingSport.Api/Services`.
- Schema database được quản lý bằng EF Core migrations trong `backend/BookingSport.Api/Migrations`.
