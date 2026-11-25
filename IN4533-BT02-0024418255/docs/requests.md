# Lệnh kiểm thử API (curl)

API chạy tại `http://localhost:5000`. Dữ liệu theo `sql/02-seed.sql` (chạy lại 01 và 02 để đưa CSDL về trạng thái ban đầu trước khi thử).

## Bốn chức năng chính

| # | Lệnh | Mã dự kiến |
|---|------|-----------|
| 1 | Xem danh sách đăng ký của khoá 2 | 200 |
| 2 | Tạo đăng ký (thành viên 5, khoá 2) | 201 |
| 3 | Xác nhận phiếu 2 | 200 |
| 4 | Huỷ phiếu 6 | 204 |

```bash
# 1. Danh sách đăng ký của khoá học 2 -> 200 OK
curl -i http://localhost:5000/api/v1/courses/2/registrations

# 2. Tạo đăng ký: thành viên 5 vào khoá 2 -> 201 Created (header Location: /api/v1/registrations/7)
curl -i -X POST http://localhost:5000/api/v1/courses/2/registrations \
  -H "Content-Type: application/json" -d '{"memberId": 5}'

# 3. Xác nhận phiếu 2 (đang pending) -> 200 OK
curl -i -X PATCH http://localhost:5000/api/v1/registrations/2 \
  -H "Content-Type: application/json" -d '{"status": "confirmed"}'

# 4. Huỷ phiếu 6 -> 204 No Content
curl -i -X DELETE http://localhost:5000/api/v1/registrations/6
```

## Các trường hợp lỗi (tuỳ chọn)

```bash
# Trùng đăng ký (thành viên 3 đã có phiếu ở khoá 2) -> 409 Conflict, application/problem+json
curl -i -X POST http://localhost:5000/api/v1/courses/2/registrations \
  -H "Content-Type: application/json" -d '{"memberId": 3}'

# Khoá học không tồn tại -> 404 Not Found
curl -i http://localhost:5000/api/v1/courses/999/registrations
```

> Trên Windows PowerShell, dùng `curl.exe` thay cho `curl` và đặt JSON trong dấu nháy kép có escape, ví dụ: `-d "{\"memberId\": 5}"`.
