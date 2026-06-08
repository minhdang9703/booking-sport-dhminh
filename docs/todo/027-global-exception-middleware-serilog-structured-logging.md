# Todo: Global Exception Middleware + Serilog Structured Logging

## Tóm tắt vấn đề

Backend hiện chưa có global exception middleware và chưa dùng Serilog structured logging. Nếu có exception không mong muốn trong controller/service, response lỗi có thể không thống nhất và logging phụ thuộc middleware mặc định của ASP.NET Core.

Cần bổ sung:

- Global middleware bắt unexpected exception.
- Response lỗi JSON thống nhất.
- Serilog structured logging.
- Request logging.
- API logging theo từng request.
- Log data có kiểm soát cho write APIs.
- Log exception kèm context request/user.
- Integration test cho lỗi 500.

## Phương án khuyến nghị

Chọn Phương án A trong plan:

- Custom `GlobalExceptionMiddleware`.
- Custom `ApiLoggingMiddleware`.
- Serilog console sink.
- Serilog rolling file sink với folder path cấu hình được.
- Log metadata cho mọi API và log sanitized request body cho write APIs.

## Các bước thực hiện

1. Xác nhận phương án
   - Duyệt Phương án A, B hoặc C.
   - Chốt có cần rolling file không.
   - Chốt error response format.
   - Chốt development có được trả `detail` exception message không.

2. Thêm Serilog packages
   - Cập nhật `backend/BookingSport.Api/BookingSport.Api.csproj`.
   - Package đề xuất:
     - `Serilog.AspNetCore`
     - `Serilog.Settings.Configuration`
     - `Serilog.Enrichers.Environment`
     - `Serilog.Enrichers.Thread`
     - `Serilog.Sinks.Console`
     - `Serilog.Sinks.File`

3. Thêm error response DTO
   - Tạo:
     - `backend/BookingSport.Api/DTOs/Errors/ErrorResponse.cs`
   - Fields đề xuất:
     - `Status`
     - `Title`
     - `Message`
     - `TraceId`
     - `Timestamp`
     - `Detail` nullable cho development nếu duyệt.

4. Thêm global exception middleware
   - Tạo:
     - `backend/BookingSport.Api/Middleware/GlobalExceptionMiddleware.cs`
   - Inject:
     - `RequestDelegate`
     - `ILogger<GlobalExceptionMiddleware>`
     - `IHostEnvironment`
   - Catch `Exception`.
   - Log unexpected error bằng `LogError`.
   - Trả HTTP 500 JSON.
   - Không throw lại sau khi đã write response.
   - Log exception kèm:
     - request method/path
     - trace id
     - user id nếu có
     - role nếu có
     - route values
     - sanitized request body nếu có

5. Thêm API logging middleware
   - Tạo:
     - `backend/BookingSport.Api/Middleware/ApiLoggingMiddleware.cs`
   - Log metadata cho từng API:
     - method
     - path
     - query string sanitized
     - status code
     - elapsed milliseconds
     - trace id
     - user id/role nếu có
   - Với write APIs, log sanitized request body:
     - `POST`
     - `PUT`
     - `PATCH`
     - `DELETE`
   - Giới hạn body size, đề xuất 16 KB.
   - Không log response body mặc định.
   - Không log body cho binary/file endpoints.
   - Bỏ qua hoặc mask path auth nhạy cảm:
     - `/api/auth/login`
     - `/api/auth/register`
     - `/api/auth/refresh`
     - `/api/auth/logout`

6. Thêm sanitizer cho log data
   - Có thể tạo:
     - `backend/BookingSport.Api/Logging/ILogSanitizer.cs`
     - `backend/BookingSport.Api/Logging/LogSanitizer.cs`
   - Mask các field:
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
   - Với JSON body, parse bằng JSON parser rồi mask property theo tên.
   - Nếu body không phải JSON, chỉ log placeholder hoặc truncate.

7. Cập nhật `Program.cs`
   - Setup Serilog trước `builder.Build()`.
   - Gọi `builder.Host.UseSerilog(...)`.
   - Đăng ký middleware:
     - `app.UseMiddleware<GlobalExceptionMiddleware>();`
     - `app.UseMiddleware<ApiLoggingMiddleware>();`
   - Thêm request logging:
     - `app.UseSerilogRequestLogging();`
   - Đảm bảo thứ tự middleware không phá:
     - Swagger
     - HTTPS redirection
     - CORS
     - Authentication
     - Authorization
     - Controllers
     - SignalR hub

8. Cập nhật appsettings
   - Cập nhật:
     - `backend/BookingSport.Api/appsettings.json`
     - `backend/BookingSport.Api/appsettings.Development.json` nếu cần.
   - Thêm section `Serilog`.
   - Minimum level:
     - Default `Information`
     - Microsoft `Warning`
     - Microsoft.AspNetCore `Warning`
     - Microsoft.EntityFrameworkCore `Warning`
   - Enrich:
     - `FromLogContext`
     - `WithMachineName`
     - `WithThreadId`
   - WriteTo:
     - `Console`
     - `File`
   - Thêm config API logging nếu cần:
     - `ApiLogging:EnableRequestBodyLogging`
     - `ApiLogging:MaxBodySizeBytes`
     - `ApiLogging:ExcludedPaths`
   - Thêm config log file folder:
     - `Logging:File:Enabled`
     - `Logging:File:FolderPath`
     - `Logging:File:FileNamePattern`
     - `Logging:File:RetainedFileCountLimit`
   - Giá trị đề xuất:
     - `Enabled = true`
     - `FolderPath = logs`
     - `FileNamePattern = booking-sport-api-.log`
     - `RetainedFileCountLimit = 14`

9. Thêm xử lý log folder path
   - Trong `Program.cs` hoặc helper extension:
     - đọc `Logging:File:FolderPath`.
     - nếu relative path thì combine với content root.
     - tạo folder nếu chưa tồn tại.
     - build full file path bằng `FileNamePattern`.
   - Nếu file logging disabled:
     - chỉ ghi console.
   - Đảm bảo không ghi password/secret vào log.
   - Cập nhật `.gitignore` nếu chưa ignore:
     - `logs/`
     - `*.log`

10. Bổ sung business event logs
   - `BookingService.CreateBookingAsync`
     - log booking created sau khi DB save thành công.
   - `BookingService.UpdateBookingStatusAsync`
     - log old/new status.
   - `AdminController.ExportBookings`
     - đã có log filter, rà lại field.
   - `AuthSettingsService` hoặc controller liên quan
     - log update setting quan trọng.
   - Không log password/token/cookie.

11. Thêm test-only endpoint để throw exception
   - Trong `Program.cs`, chỉ map khi:
     - `app.Environment.IsEnvironment("IntegrationTests")`
   - Endpoint đề xuất:
     - `GET /test/throw`
   - Endpoint throw `InvalidOperationException`.
   - Không expose endpoint này trong development/production.

12. Thêm integration tests
   - Tạo:
     - `backend/BookingSport.Api.Tests/Integration/GlobalExceptionMiddlewareIntegrationTests.cs`
   - Test:
     - `GET /test/throw` trả `500`.
     - Body là JSON.
     - Body có `status = 500`.
     - Body có `traceId`.
     - Body không chứa stack trace.
   - Thêm test logging/sanitizer:
     - password bị mask.
     - access token bị mask.
     - body bình thường được giữ field an toàn.
     - body quá lớn bị truncate hoặc bỏ qua theo config.
   - Test log folder path helper nếu tách helper:
     - relative path resolve đúng.
     - absolute path giữ nguyên.
     - disabled file logging không tạo file sink.

13. Chạy kiểm tra
   - `dotnet restore`
   - `dotnet build backend\BookingSport.sln`
   - `dotnet test backend\BookingSport.sln`
   - Nếu có frontend không đổi thì không cần E2E.

14. Ghi report sau triển khai
   - Tạo:
     - `docs/report/027-global-exception-middleware-serilog-structured-logging-report.md`
   - Ghi:
     - File đã thay đổi.
     - Test đã chạy.
     - Kết quả.
     - Lưu ý cấu hình logging.

## Phạm vi ảnh hưởng

### Backend files dự kiến

- `backend/BookingSport.Api/BookingSport.Api.csproj`
- `backend/BookingSport.Api/Program.cs`
- `backend/BookingSport.Api/appsettings.json`
- `backend/BookingSport.Api/appsettings.Development.json`
- `.gitignore`
- `backend/BookingSport.Api/Middleware/GlobalExceptionMiddleware.cs`
- `backend/BookingSport.Api/Middleware/ApiLoggingMiddleware.cs`
- `backend/BookingSport.Api/DTOs/Errors/ErrorResponse.cs`
- `backend/BookingSport.Api/Logging/ILogSanitizer.cs`
- `backend/BookingSport.Api/Logging/LogSanitizer.cs`

### Test files dự kiến

- `backend/BookingSport.Api.Tests/Integration/GlobalExceptionMiddlewareIntegrationTests.cs`
- `backend/BookingSport.Api.Tests/Logging/LogSanitizerTests.cs`

### Function/middleware ảnh hưởng

- Request pipeline trong `Program.cs`.
- Unexpected exception response toàn API.
- Logging output của backend.
- API request metadata logging.
- Sanitized request body logging cho write APIs.

## Các cách triển khai đề xuất khác

### Phương án A: Custom middleware + Serilog console

- Khuyến nghị.
- Dùng console + rolling file sink.
- Log folder path cấu hình được.
- Dễ test và dễ xem log local.
- Có API logging middleware và sanitizer.

### Phương án B: Custom middleware + Serilog console only

- Không ghi file log.
- Phù hợp container stdout.
- Ít file phát sinh hơn nhưng khó xem lại log local sau khi tắt terminal.
- Có API logging middleware và sanitizer.

### Phương án C: Built-in ProblemDetails

- Dùng cơ chế built-in.
- Ít code custom.
- Format có thể kém linh hoạt hơn.

## Cần bạn duyệt

Vui lòng chọn:

- Phương án A, B hoặc C.
- Log folder mặc định `logs` có ổn không.
- File name pattern `booking-sport-api-.log` có ổn không.
- Retained file count `14` có ổn không.
- Development có trả `detail` exception message trong response không.
- Có bật log request body cho write APIs không.
- Body log max size đề xuất `16 KB` có ổn không.
