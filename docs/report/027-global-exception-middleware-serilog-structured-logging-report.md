# Report: Global Exception Middleware + Serilog Structured Logging

## Thời gian triển khai

- Ngày triển khai: 2026-06-08
- Phạm vi: backend logging, exception handling, API request logging

## Nội dung đã triển khai

1. Serilog structured logging
   - Thêm Serilog vào backend.
   - Log ra console.
   - Log ra rolling file theo cấu hình folder path.
   - Enrich log với:
     - `Application`
     - environment name
     - machine name
     - thread id
     - log context

2. Config log folder path
   - Thêm cấu hình:
     - `Logging:File:Enabled`
     - `Logging:File:FolderPath`
     - `Logging:File:FileNamePattern`
     - `Logging:File:RetainedFileCountLimit`
   - Mặc định:
     - folder `logs`
     - file pattern `booking-sport-api-.log`
     - retained file count `14`
   - Relative path được resolve theo content root.
   - Folder log tự được tạo nếu chưa tồn tại.
   - `.gitignore` hiện đã ignore `logs/` và `*.log`.

3. Global exception middleware
   - Thêm `GlobalExceptionMiddleware`.
   - Bắt unexpected exception.
   - Log exception kèm request context.
   - Trả JSON error response thống nhất:
     - `status`
     - `title`
     - `message`
     - `traceId`
     - `timestamp`
     - `detail` trong Development/IntegrationTests.

4. API logging middleware
   - Thêm `ApiLoggingMiddleware`.
   - Log mỗi API request:
     - method
     - path
     - query string sanitized
     - status code
     - elapsed milliseconds
     - trace id
     - user id/role nếu có
   - Log sanitized request body cho write methods:
     - `POST`
     - `PUT`
     - `PATCH`
     - `DELETE`
   - Body logging giới hạn mặc định 16 KB.
   - Excluded auth paths:
     - `/api/auth/login`
     - `/api/auth/register`
     - `/api/auth/refresh`
     - `/api/auth/logout`

5. Log sanitizer
   - Thêm `LogSanitizer`.
   - Mask JSON fields và query string fields nhạy cảm:
     - `password`
     - `currentPassword`
     - `newPassword`
     - `confirmPassword`
     - `accessToken`
     - `refreshToken`
     - `token`
     - `authorization`
     - `cookie`
     - `smtpPassword`
     - `secret`
     - `clientSecret`

6. Business event logs
   - Bổ sung log khi booking được tạo.
   - Bổ sung log khi booking status được cập nhật.

## Files chính đã thay đổi

- `backend/BookingSport.Api/Program.cs`
- `backend/BookingSport.Api/BookingSport.Api.csproj`
- `backend/BookingSport.Api/appsettings.json`
- `backend/BookingSport.Api/DTOs/Errors/ErrorResponse.cs`
- `backend/BookingSport.Api/Logging/*`
- `backend/BookingSport.Api/Middleware/GlobalExceptionMiddleware.cs`
- `backend/BookingSport.Api/Middleware/ApiLoggingMiddleware.cs`
- `backend/BookingSport.Api/Services/Bookings/BookingService.cs`
- `backend/BookingSport.Api.Tests/Integration/GlobalExceptionMiddlewareIntegrationTests.cs`
- `backend/BookingSport.Api.Tests/Logging/LogSanitizerTests.cs`

## Tests đã thêm

1. `LogSanitizerTests`
   - JSON sensitive fields bị mask.
   - Query string sensitive fields bị mask.

2. `GlobalExceptionMiddlewareIntegrationTests`
   - `/test/throw` trả HTTP 500.
   - Response content type JSON.
   - Body có `status`, `title`, `message`, `traceId`, `timestamp`.
   - Body không chứa stack trace.

## Kết quả kiểm tra

Đã chạy:

```powershell
dotnet build backend\BookingSport.sln
dotnet test backend\BookingSport.sln
```

Kết quả:

- Build: pass.
- Tests: pass `84/84`.

## Lưu ý

- `appsettings.Development.json` là file ignored theo `.gitignore`; cấu hình logging local đã được cập nhật trong workspace nhưng không nên commit nếu chứa secret SMTP.
- API logging không log response body mặc định để tránh lộ dữ liệu và tránh log file Excel/binary.
- Hangfire job exceptions vẫn do Hangfire retry/log riêng; global exception middleware xử lý request pipeline.
- Frontend không thay đổi trong scope 027 nên không chạy E2E cho scope này.
