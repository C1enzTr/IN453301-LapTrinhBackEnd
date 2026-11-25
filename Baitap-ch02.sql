-- IN4533 - Chương 2 
-- Câu 1: Liệt kê tên các khoá học mà Nguyễn An đã đăng ký (JOIN)
SELECT c.title
FROM registrations r
JOIN members m ON m.id = r.member_id
JOIN courses c ON c.id = r.course_id
WHERE m.full_name = 'Nguyễn An';

-- Câu 2: Đếm số thành viên của mỗi khoá, khoá nhiều người nhất đứng đầu (GROUP BY)
SELECT c.title, COUNT(r.id) AS so_thanh_vien
FROM courses c
LEFT JOIN registrations r ON r.course_id = c.id
GROUP BY c.id, c.title
ORDER BY so_thanh_vien DESC;

-- Câu 3: Chuyển Lê Chi vào khoá "Cơ sở dữ liệu" bằng một transaction
BEGIN;
INSERT INTO registrations (member_id, course_id)
SELECT m.id, c.id
FROM members m, courses c
WHERE m.full_name = 'Lê Chi' AND c.title = 'Cơ sở dữ liệu';
COMMIT;

-- Kiểm tra lại
SELECT m.full_name, c.title, r.status
FROM registrations r
JOIN members m ON m.id = r.member_id
JOIN courses c ON c.id = r.course_id
WHERE m.full_name = 'Lê Chi';
