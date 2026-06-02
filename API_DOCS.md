
# API Documentation — Booking Sport

Base URL (local): `http://localhost:5000` (kiểm tra `launchSettings.json` hoặc cấu hình môi trường)

## Xác thực

- Header: `Authorization: Bearer {token}`

### POST /api/auth/register
- Mô tả: Đăng ký người dùng mới.
- Request body (JSON):

```json
{
	"email": "user@example.com",
	"password": "P@ssw0rd",
	"fullName": "Nguyen Van A"
}
```

- Response: `201 Created` với thông tin `AuthResponse` chứa token và user.

### POST /api/auth/login
- Mô tả: Đăng nhập, trả về JWT.
- Request body (JSON):

```json
{
	"email": "user@example.com",
	"password": "P@ssw0rd"
}
```

- Response: `200 OK` — `AuthResponse` gồm `accessToken`, `expiresIn`, `user`.

### GET /api/auth/me
- Mô tả: Lấy thông tin người dùng hiện tại.
- Yêu cầu: header `Authorization`.
- Response: `200 OK` — `CurrentUserResponse`.

## Users

### GET /api/users
- Mô tả: Lấy danh sách người dùng (admin).
- Query: `page`, `pageSize`, `role` (tuỳ implement).

### GET /api/users/{id}
- Mô tả: Lấy chi tiết user theo `id`.

### PUT /api/users/{id}
- Mô tả: Cập nhật thông tin user (admin hoặc chính chủ).

## Courts (Sân)

### GET /api/courts
- Mô tả: Lấy danh sách sân, hỗ trợ lọc theo `venueId`, `sportId`, `status`.

### GET /api/courts/{id}
- Mô tả: Lấy chi tiết sân.

### POST /api/courts
- Mô tả: Tạo sân mới (admin).
- Body: `CourtCreateRequest` (tên, venueId, sportId, mô tả, trạng thái, giá...)

### PUT /api/courts/{id}
- Mô tả: Cập nhật sân (admin).

### DELETE /api/courts/{id}
- Mô tả: Xoá sân hoặc đánh dấu inactive (admin).

## Court Schedules / Availability

### GET /api/courts/{courtId}/schedules
- Mô tả: Lấy khung giờ có sẵn/đã đặt của một sân.
- Query: `date` (YYYY-MM-DD), `from`, `to`.

### POST /api/courts/{courtId}/schedules
- Mô tả: Tạo khung giờ cho sân (admin/venue manager).

## Bookings (Đặt lịch)

### POST /api/bookings
- Mô tả: Tạo đặt lịch mới.
- Yêu cầu: `Authorization`.
- Body (ví dụ `BookingCreateRequest`):

```json
{
	"courtId": 123,
	"date": "2026-06-10",
	"startTime": "18:00",
	"endTime": "19:00",
	"price": 200000,
	"customerNote": "Team tập luyện"
}
```

- Response: `201 Created` — `BookingResponse`.

### GET /api/bookings
- Mô tả: Lấy danh sách đặt lịch (của user hiện tại hoặc admin cho tất cả).
- Query: `status`, `dateFrom`, `dateTo`, `courtId`, `page`, `pageSize`.

### GET /api/bookings/{id}
- Mô tả: Lấy chi tiết đặt lịch.

### PUT /api/bookings/{id}/status
- Mô tả: Cập nhật trạng thái đặt (confirm, cancel, complete).
- Body (ví dụ `BookingUpdateStatusRequest`):

```json
{
	"status": "Cancelled",
	"note": "Khách hủy"
}
```

## Payments

### POST /api/payments
- Mô tả: Tạo yêu cầu thanh toán cho một booking.
- Body: gồm `bookingId`, `method`, `amount`, `metadata`.

### GET /api/payments/{id}
- Mô tả: Lấy trạng thái giao dịch.

## Dashboard / Reports

### GET /api/dashboard/summary
- Mô tả: Thống kê tổng quan (doanh thu, số booking, sân phổ biến) — admin.

### GET /api/dashboard/revenue?from=YYYY-MM-DD&to=YYYY-MM-DD
- Mô tả: Báo cáo doanh thu theo khoảng thời gian.

## Lỗi phổ biến

- `400 Bad Request` — dữ liệu đầu vào không hợp lệ.
- `401 Unauthorized` — thiếu hoặc token không hợp lệ.
- `403 Forbidden` — thiếu quyền truy cập.
- `404 Not Found` — resource không tồn tại.
- `409 Conflict` — xung đột (ví dụ trùng lặp booking).

## Ghi chú triển khai

- Các DTO chính và controller nằm tại `backend/BookingSport.Api/Controllers` và `backend/BookingSport.Api/DTOs`.
- Kiểm tra migration trong `backend/BookingSport.Api/Migrations` khi cần cập nhật schema.

---

Nếu bạn muốn, tôi có thể:
- Thêm ví dụ request/response đầy đủ cho mỗi endpoint.
- Sinh OpenAPI/Swagger summary hoặc tách thành từng phần cho frontend.
