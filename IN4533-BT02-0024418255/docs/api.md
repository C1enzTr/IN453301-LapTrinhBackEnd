# Thiết kế REST API – registrations (EduHub mini)

Tiền tố chung: `/api/v1`. URI dùng danh từ số nhiều, không chứa động từ. Lỗi trả theo Problem Details (RFC 9457), `Content-Type: application/problem+json`.

## 2.1. Bảng thiết kế

| # | Chức năng | Method | URI | Mã thành công | Mã lỗi có thể gặp |
|---|-----------|--------|-----|---------------|-------------------|
| 1 | Xem danh sách đăng ký của một khoá học | GET | `/api/v1/courses/{courseId}/registrations` | 200 OK | 400 (courseId không hợp lệ), 404 (khoá học không tồn tại), 500 |
| 2 | Tạo đăng ký | POST | `/api/v1/courses/{courseId}/registrations` | 201 Created (kèm header `Location`) | 400 (body thiếu/sai), 404 (khoá học hoặc thành viên không tồn tại), 409 (đã đăng ký khoá này), 500 |
| 3 | Xác nhận đăng ký | PATCH | `/api/v1/registrations/{registrationId}` (body `{"status":"confirmed"}`) | 200 OK | 400 (status không hợp lệ), 404 (phiếu không tồn tại), 409 (phiếu đã huỷ, không thể xác nhận), 500 |
| 4 | Huỷ đăng ký | DELETE | `/api/v1/registrations/{registrationId}` | 204 No Content | 404 (phiếu không tồn tại), 500 |

Ghi chú thiết kế:
- Danh sách và tạo phiếu nằm dưới `courses/{courseId}` vì phiếu thuộc về một khoá học; xác nhận và huỷ thao tác trực tiếp trên một phiếu nên dùng `registrations/{registrationId}`.
- Xác nhận là cập nhật một phần trạng thái nên dùng PATCH, thay vì dùng động từ trong URI.
- Huỷ dùng DELETE nhưng là "huỷ mềm": phiếu được chuyển sang `cancelled` và vẫn còn trong CSDL (để giữ lịch sử). Huỷ phiếu đã huỷ vẫn trả 204 (idempotent).
- Vì có ràng buộc `UNIQUE (member_id, course_id)`, thành viên đã huỷ phiếu không tạo lại được phiếu mới cho cùng khoá học (trả 409).

## 2.2. JSON mẫu cho chức năng tạo đăng ký

Dữ liệu mẫu bám theo `sql/02-seed.sql`: đã có 6 phiếu, nên phiếu mới có `id` = 7; thành viên 5 chưa đăng ký khoá nào, thành viên 3 đã đăng ký khoá 2.

### Thành công

```http
POST /api/v1/courses/2/registrations
Content-Type: application/json

{ "memberId": 5 }
```

```http
HTTP/1.1 201 Created
Location: /api/v1/registrations/7
Content-Type: application/json

{
  "id": 7,
  "memberId": 5,
  "courseId": 2,
  "status": "pending"
}
```

### Lỗi trùng đăng ký (Problem Details, RFC 9457)

```http
POST /api/v1/courses/2/registrations
Content-Type: application/json

{ "memberId": 3 }
```

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json

{
  "type": "https://eduhub.example.com/problems/duplicate-registration",
  "title": "Duplicate registration",
  "status": 409,
  "detail": "Thành viên 3 đã đăng ký khoá học 2.",
  "instance": "/api/v1/courses/2/registrations"
}
```
