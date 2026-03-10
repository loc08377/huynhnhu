using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading.Tasks;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;

    public AuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            var path = context.Request.Path.Value;

            // Danh sách path không cần login
            var allowPaths = new[]
            {
                "/Login/Index",
                "/Login/Login",
                "/Login/Register",
                "/Home/Register",
                "/Home/Index",
                "/css",
                "/js",
                "/images"
            };

            // Bỏ qua các path public
            if (allowPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
            {
                await _next(context);
                return;
            }

            // Check login
            if (!context.User.Identity.IsAuthenticated)
            {
                context.Response.Redirect("/Login/Index");
                return;
            }

            // Lấy role
            var role = context.User.FindFirst("Role")?.Value;

            // User thường không được vào admin
            if (role == "0" && path.StartsWith("/Admin", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.Redirect("/Home/Index?error=403");
                return;
            }

            await _next(context);

            // Bắt lỗi status code
            if (context.Response.StatusCode == 403)
            {
                context.Response.Redirect("/Home/Index?error=403");
            }

            if (context.Response.StatusCode == 404)
            {
                context.Response.Redirect("/Home/Index?error=404");
            }

            if (context.Response.StatusCode == 500)
            {
                context.Response.Redirect("/Home/Index?error=500");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);

            context.Response.Redirect("/Home/Index?error=500");
        }
    }
}