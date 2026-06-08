# Plan: Global Exception Middleware + Serilog Structured Logging

## Bối cảnh

Backend hiện đang dùng logging mặc định của ASP.NET Core và một số nơi có `ILogger`:

- `AdminController` log khi admin cập nhật auth settings và export booking report.
- `HangfireBookingConfirmationEmailQueue` log khi enqueue email job.
- `BookingEmailJob` log khi gửi email thành công hoặc bỏ qua booking không tồn tại.

Tuy nhiên hệ thống chưa có:

- Global exception middleware để bắt lỗi không mong muốn.
- Response lỗi thống nhất dạng JSON/Problem Details.
- Serilog structured logging.
- Request logging có correlation id/request id.
- Cấu hình sink/output rõ ràng cho development/production.

Theo quy chuẩn repo, controller nên mỏng, business logic nằm ở service, unexpected errors nên được xử lý ở middleware toàn cục thay vì try/catch rải rác trong controller.

## Mục tiêu thay đổi

1. Thêm global exception middleware
   - Bắt toàn bộ exception chưa được handle.
   - Log lỗi unexpected một lần tại middleware.
   - Trả response JSON thống nhất, không lộ stack trace trong production.
   - Giữ nguyên các lỗi business validation hiện tại đang trả `BadRequest`, `Conflict`, `NotFound`.

2. Thêm Serilog structured logging
   - Dùng Serilog thay logging provider mặc định.
   - Log ra console ở development.
   - Log ra rolling file theo folder path cấu hình được.
   - Enrich log với context cơ bản:
     - `Application`
     - `Environment`
     - `RequestId` hoặc `CorrelationId`
     - request path/method/status/duration qua request logging.
   - Cho phép cấu hình thư mục log:
     - `Logging:File:Enabled`
     - `Logging:File:FolderPath`
     - `Logging:File:FileNamePattern`
     - `Logging:File:RetainedFileCountLimit`

3. Bổ sung API logging có kiểm soát
   - Log mỗi API request ở mức metadata:
     - HTTP method
     - path
     - query string đã mask nếu có field nhạy cảm
     - status code
     - elapsed milliseconds
     - user id nếu đã đăng nhập
     - role nếu có
     - client IP nếu cần
   - Log request body cho các API thay đổi dữ liệu quan trọng ở mức có kiểm soát:
     - `POST /api/bookings`
     - `PUT /api/bookings/{id}/status`
     - admin CRUD courts/price rules/users nếu có
     - auth settings/session settings update
     - admin export report filters
   - Không log response body mặc định để tránh lộ dữ liệu và làm log quá lớn.
   - Không log secrets/sensitive data:
     - password
     - access token
     - refresh cookie
     - authorization header
     - email SMTP password
     - cookie header
   - Với exception, log thêm context:
     - request path/method
     - trace id
     - user id/role nếu có
     - route values
     - sanitized request body nếu có thể đọc được

4. Chuẩn hóa response lỗi unexpected
   - Dạng đề xuất:
     - `status`
     - `title`
     - `message`
     - `traceId`
     - `timestamp`
   - Development có thể thêm `detail` nếu cần.
   - Production không trả stack trace.

5. Bổ sung tests
   - Integration test endpoint throw exception trả HTTP 500 JSON thống nhất.
   - Test response không lộ stack trace.
   - Test business errors hiện tại không bị middleware làm thay đổi.

## Phạm vi đề xuất

### Backend packages

Thêm package:

- `Serilog.AspNetCore`
- `Serilog.Settings.Configuration`
- `Serilog.Enrichers.Environment`
- `Serilog.Enrichers.Thread`
- `Serilog.Sinks.Console`
- `Serilog.Sinks.File` nếu chọn rolling file

### Middleware

Tạo:

- `backend/BookingSport.Api/Middleware/GlobalExceptionMiddleware.cs`
- `backend/BookingSport.Api/Middleware/ApiLoggingMiddleware.cs` nếu chọn log request body có kiểm soát
- Có thể thêm DTO:
  - `backend/BookingSport.Api/DTOs/Errors/ErrorResponse.cs`

Middleware flow:

1. Gọi `next(context)`.
2. Nếu có exception:
   - Log `LogError(exception, ...)`.
   - Set status `500`.
   - Set content type `application/json`.
   - Trả error response có `traceId`.

Vị trí đăng ký:

- Sau `UseHttpsRedirection`.
- Trước `UseCors`, `UseAuthentication`, `MapControllers`.

### API logging middleware

Nếu cần log data cho từng API, tạo `ApiLoggingMiddleware` riêng thay vì nhồi vào exception middleware.

Nhiệm vụ:

1. Tạo scope log với:
   - `TraceId`
   - `UserId`
   - `UserRole`
   - `RequestPath`
   - `RequestMethod`
2. Đọc request body có giới hạn dung lượng, ví dụ tối đa 16 KB.
3. Chỉ log body với method:
   - `POST`
   - `PUT`
   - `PATCH`
   - `DELETE`
4. Sanitize body trước khi log.
5. Bỏ qua hoặc mask các path nhạy cảm:
   - `/api/auth/login`
   - `/api/auth/register`
   - `/api/auth/refresh`
   - `/api/auth/logout`
6. Không log file/binary response, ví dụ Excel export body.

Field cần mask:

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
- `Email:Password`

### Serilog setup

Cập nhật `Program.cs`:

- Configure `Log.Logger`.
- `builder.Host.UseSerilog(...)`.
- `app.UseSerilogRequestLogging(...)`.
- Đọc cấu hình log folder path.
- Tạo folder log nếu chưa tồn tại.
- Nếu `Logging:File:Enabled = true`, thêm file sink path từ config.

Cấu hình trong `appsettings.json` và `appsettings.Development.json`:

- `Serilog:Using`
- `Serilog:MinimumLevel`
- `Serilog:Enrich`
- `Serilog:WriteTo`
- `Logging:File`

Ví dụ development:

- Console sink.
- Minimum `Information`.
- Override `Microsoft.AspNetCore` là `Warning`.
- Override `Microsoft.EntityFrameworkCore` là `Warning`.
- File sink:
  - folder mặc định: `logs`
  - file pattern: `booking-sport-api-.log`
  - rolling daily
  - retained file count: 14

Ví dụ config đề xuất:

```json
"Logging": {
  "File": {
    "Enabled": true,
    "FolderPath": "logs",
    "FileNamePattern": "booking-sport-api-.log",
    "RetainedFileCountLimit": 14
  }
}
```

Path xử lý:

- Nếu `FolderPath` là relative path, resolve theo application content root.
- Nếu `FolderPath` là absolute path, dùng trực tiếp.
- Tự tạo thư mục nếu chưa tồn tại.
- Không commit log files.

### Logging business events

Giữ nguyên các event đã có:

- Admin update auth settings.
- Admin export booking report.
- Enqueue booking confirmation email job.
- Booking confirmation email sent/skipped.

Có thể bổ sung sau nếu cần:

- Booking created.
- Booking status updated.
- Excel export failed.
- SignalR notify failed.

Nên bổ sung ngay trong scope này:

- Booking created:
  - `BookingId`
  - `UserId`
  - `CourtId`
  - `BookingDate`
  - `StartTime`
  - `EndTime`
  - `TotalPrice`
- Booking status updated:
  - `BookingId`
  - `OldStatus`
  - `NewStatus`
- Admin export booking report:
  - filter date/status/court
  - không log file content
- Auth/session settings updated:
  - actor admin/user id
  - lifetime values mới
- Unexpected exception:
  - exception object
  - sanitized request body
  - trace id

Không log các event quá nhiễu như mọi request DB hoặc mọi read query.

### Tests

Đề xuất thêm endpoint test-only chỉ trong IntegrationTests environment:

- `GET /test/throw`
- Endpoint này throw exception để kiểm tra middleware.

Hoặc tạo controller test-only qua test factory. Phương án ít thay đổi hơn là map endpoint khi environment là `IntegrationTests`.

Tests:

- `GlobalExceptionMiddlewareIntegrationTests`
  - `GET /test/throw` trả `500`.
  - Response content type JSON.
  - Body có `traceId`, `status`, `title`, `message`.
  - Body không chứa stack trace.

- `ApiLoggingMiddlewareTests` hoặc integration test nhẹ
  - Request body có `password` không xuất hiện plaintext trong log.
  - Request body bình thường có thể được sanitize/log theo config.
  - Excel export không log response body.

- Regression tests:
  - Existing validation/conflict tests vẫn pass.

## Các phương án triển khai đề xuất

### Phương án A: Middleware custom + Serilog console + configurable file folder

- Tạo middleware custom.
- Dùng Serilog console sink.
- Dùng rolling file sink với folder path cấu hình được.
- Thêm API logging middleware log metadata + sanitized request body cho write APIs.
- Ưu điểm:
  - Có log file local dễ kiểm tra.
  - Folder path đổi được theo môi trường.
  - Dễ review.
  - Phù hợp development/container logs.
- Nhược điểm:
  - Cần đảm bảo log folder không bị commit.

### Phương án B: Middleware custom + Serilog console only

- Tạo middleware custom.
- Chỉ dùng console sink.
- Thêm API logging middleware log metadata + sanitized request body cho write APIs.
- Ưu điểm:
  - Ít file sinh ra nhất.
  - Phù hợp container stdout.
- Nhược điểm:
  - Không có log file local để tra cứu sau khi tắt terminal.

### Phương án C: Dùng ASP.NET Core ProblemDetails thay middleware custom

- Dùng `AddProblemDetails`/exception handler của ASP.NET Core.
- Ít code custom hơn.
- Ưu điểm:
  - Theo built-in framework.
- Nhược điểm:
  - Ít chủ động hơn với format response theo yêu cầu project.
  - Vẫn cần cấu hình logging riêng.

## Khuyến nghị

Chọn Phương án A:

- Middleware custom giúp format lỗi rõ ràng và dễ test.
- Serilog console + rolling file folder path cấu hình được.
- Vẫn giữ console để phù hợp container/stdout.
- File log giúp kiểm tra local/debug dễ hơn.

## Rủi ro và lưu ý

- Middleware phải đặt đủ sớm để bắt exception từ controllers/services, nhưng không nên phá Swagger/dev pipeline.
- Không trả stack trace hoặc exception message nhạy cảm ở production.
- Không log quá nhiều business event gây nhiễu.
- Nếu bật request logging ở `Information`, cần override namespace Microsoft để tránh log quá dày.
- File sink cần đảm bảo `logs/` hoặc folder cấu hình không bị commit.
- Nếu folder path không có quyền ghi, app startup có thể fail hoặc Serilog không ghi file được; cần log rõ cấu hình.
- Hangfire exception/retry vẫn do Hangfire xử lý; global middleware chỉ bắt lỗi request pipeline.
- Log request body cần giới hạn size và sanitize kỹ, nếu không sẽ lộ dữ liệu nhạy cảm.
- Không nên log response body mặc định, đặc biệt với Excel export, auth response, user profile hoặc danh sách dữ liệu lớn.
