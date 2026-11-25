-- sql/03-queries.sql
SET client_encoding = 'UTF8';

-- (a) Họ tên thành viên và tên khoá học của các phiếu đã xác nhận (confirmed), sắp xếp theo họ tên.
SELECT m.full_name, c.title
FROM registrations r
JOIN members m ON m.id = r.member_id
JOIN courses  c ON c.id = r.course_id
WHERE r.status = 'confirmed'
ORDER BY m.full_name, c.title;

-- (b) Số phiếu đăng ký của từng khoá học, kể cả khoá chưa có phiếu nào (hiện số 0).
-- Dùng LEFT JOIN vì JOIN (INNER) sẽ loại các khoá không có phiếu, khiến khoá "Docker cơ bản" biến mất;
-- LEFT JOIN giữ mọi dòng của courses, còn COUNT(r.id) không đếm giá trị NULL nên cho kết quả 0.
SELECT c.title, COUNT(r.id) AS so_dang_ky
FROM courses c
LEFT JOIN registrations r ON r.course_id = c.id
GROUP BY c.id, c.title
ORDER BY c.id;

-- (c) Các thành viên chưa đăng ký khoá học nào.
SELECT m.id, m.full_name, m.email
FROM members m
WHERE NOT EXISTS (
    SELECT 1 FROM registrations r WHERE r.member_id = m.id
)
ORDER BY m.full_name;
