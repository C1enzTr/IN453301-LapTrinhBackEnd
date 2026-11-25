using Npgsql;

var builder = WebApplication.CreateBuilder(args);

// Chuỗi kết nối lấy từ cấu hình (user-secrets / biến môi trường), KHÔNG ghi mật khẩu thật vào mã nguồn.
var connString = builder.Configuration.GetConnectionString("EduHub")
    ?? throw new InvalidOperationException("Thiếu ConnectionStrings:EduHub");

var app = builder.Build();

// 1. GET /api/v1/courses/{courseId}/registrations  -> 200 | 404
app.MapGet("/api/v1/courses/{courseId:int}/registrations",
    async Task<IResult> (int courseId, HttpRequest req) =>
{
    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    if (!await CourseExists(conn, courseId))
        return Problems.NotFound(req, $"Không tìm thấy khoá học {courseId}.");

    const string sql = """
        SELECT r.id, r.member_id, m.full_name, r.course_id, r.status
        FROM registrations r
        JOIN members m ON m.id = r.member_id
        WHERE r.course_id = @courseId
        ORDER BY r.id
        """;

    await using var cmd = new NpgsqlCommand(sql, conn);
    cmd.Parameters.AddWithValue("courseId", courseId);

    var list = new List<RegistrationDto>();
    await using var reader = await cmd.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
        list.Add(new RegistrationDto(
            reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2),
            reader.GetInt32(3), reader.GetString(4)));
    }
    return Results.Ok(list);
});

// 2. POST /api/v1/courses/{courseId}/registrations  -> 201 | 400 | 404 | 409
app.MapPost("/api/v1/courses/{courseId:int}/registrations",
    async Task<IResult> (int courseId, CreateRegistrationRequest body, HttpRequest req) =>
{
    if (body.MemberId <= 0)
        return Problems.BadRequest(req, "memberId phải là số nguyên dương.");

    await using var conn = new NpgsqlConnection(connString);
    await conn.OpenAsync();

    if (!await CourseExists(conn, courseId))
        return Problems.NotFound(req, $"Không tìm thấy khoá học {courseId}.");

    const string sql = """
        INSERT INTO registrations (member_id, course_id)
        VALUES (@memberId, @courseId)
        RETURNING id, member_id, course_id, status
        """;

    try
    {
        await using var cmd = new NpgsqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("memberId", body.MemberId);
        cmd.Parameters.AddWithValue("courseId", courseId);

        await using var reader = await cmd.ExecuteReaderAsync();
        await reader.ReadAsync();
        var id = reader.GetInt32(0);
        var created = new CreatedRegistrationDto(id, reader.GetInt32(1), reader.GetInt32(2), reader.GetString(3));
        return Results.Created($"/api/v1/registrations/{id}", created);
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        return Problems.Conflict(req, $"Thành viên {body.MemberId} đã đăng ký khoá học {courseId}.");
    }
    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
    {
        return Problems.NotFound(req, $"Không tìm thấy thành viên {body.MemberId}.");
    }
});

app.Run();

static async Task<bool> CourseExists(NpgsqlConnection conn, int courseId)
{
    await using var cmd = new NpgsqlCommand("SELECT 1 FROM courses WHERE id = @courseId", conn);
    cmd.Parameters.AddWithValue("courseId", courseId);
    return await cmd.ExecuteScalarAsync() is not null;
}

record RegistrationDto(int Id, int MemberId, string MemberName, int CourseId, string Status);
record CreatedRegistrationDto(int Id, int MemberId, int CourseId, string Status);
record CreateRegistrationRequest(int MemberId);

// Problem Details (RFC 9457) -> Content-Type: application/problem+json
static class Problems
{
    const string Base = "https://eduhub.example.com/problems/";

    public static IResult BadRequest(HttpRequest req, string detail) =>
        Results.Problem(detail: detail, instance: req.Path, statusCode: 400,
            title: "Bad request", type: Base + "bad-request");

    public static IResult NotFound(HttpRequest req, string detail) =>
        Results.Problem(detail: detail, instance: req.Path, statusCode: 404,
            title: "Resource not found", type: Base + "not-found");

    public static IResult Conflict(HttpRequest req, string detail) =>
        Results.Problem(detail: detail, instance: req.Path, statusCode: 409,
            title: "Duplicate registration", type: Base + "duplicate-registration");
}
