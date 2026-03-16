using duAn1.Services;
public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public AuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _configuration = configuration;
    }

    private bool IsAjaxRequest(HttpContext context)
    {
        return context.Request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
               context.Request.Headers["Accept"].ToString().Contains("application/json");
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var path = context.Request.Path.Value ?? "/";

            // Lấy danh sách path không cần login từ appsettings.json
            var allowPaths = _configuration.GetSection("Security:AllowedPaths").Get<string[]>() ?? new string[] { };


            // Bỏ qua các path public
            if (allowPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Check login
            if (context.User?.Identity == null || !context.User.Identity.IsAuthenticated)
            {
                string redirectUrl;
                // Nếu cookie hết hạn (không xác thực nhưng có cookie)
                if (context.Request.Cookies.ContainsKey("duAn1Auth"))
                {
                    redirectUrl = "/Login/Index?error=expired";
                }
                else
                {
                    redirectUrl = "/Login/Index?error=needlogin";
                }

                // Nếu là AJAX request, trả về 401 với JSON
                if (IsAjaxRequest(context))
                {
                    context.Response.StatusCode = 401;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { redirect = redirectUrl });
                    return;
                }

                context.Response.Redirect(redirectUrl);
                return;
            }

            // Lấy userId từ claim và check tài khoản có bị khóa không
            var userIdClaim = context.User.FindFirst("UserId")?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId))
            {
                var userService = context.RequestServices.GetRequiredService<UserService>();
                var user = userService.GetUserById(userId);
                if (user == null || user.Actived == false)
                {
                    string redirectUrl = "/Login/Index?error=locked";
                    
                    if (IsAjaxRequest(context))
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new { redirect = redirectUrl });
                        return;
                    }

                    context.Response.Redirect(redirectUrl);
                    return;
                }
            }

            // Lấy role
            var role = context.User.FindFirst("Role")?.Value;

            // Chỉ role = 1 mới được vào /AdminJewel, còn lại redirect về home
            if (path.StartsWith("/AdminJewel", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[AdminJewel Check] Role: {role}, Allowed: {role == "1"}");
                if (role != "1")
                {
                    Console.WriteLine("[AdminJewel] Unauthorized - Redirecting to Home/Index");
                    string redirectUrl = "/Home/Index?error=403";

                    if (IsAjaxRequest(context))
                    {
                        context.Response.StatusCode = 403;
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsJsonAsync(new { redirect = redirectUrl });
                        return;
                    }

                    context.Response.Redirect(redirectUrl);
                    return;
                }
            }

            await _next(context);

            // Bắt lỗi status code
            if (context.Response.StatusCode == 403)
            {
                string redirectUrl = "/Home/Index?error=403";
                if (IsAjaxRequest(context))
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { redirect = redirectUrl });
                    return;
                }
                context.Response.Redirect(redirectUrl);
            }
            else if (context.Response.StatusCode == 404)
            {
                string redirectUrl = "/Home/Index?error=404";
                if (IsAjaxRequest(context))
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { redirect = redirectUrl });
                    return;
                }
                context.Response.Redirect(redirectUrl);
            }
            else if (context.Response.StatusCode == 500)
            {
                string redirectUrl = "/Home/Index?error=500";
                if (IsAjaxRequest(context))
                {
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsJsonAsync(new { redirect = redirectUrl });
                    return;
                }
                context.Response.Redirect(redirectUrl);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);

            context.Response.Redirect("/Home/Index?error=500");
        }
    }
}