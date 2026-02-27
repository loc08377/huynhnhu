using duAn1.Models;
using duAn1.Repository;
using duAn1.Services;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// ==========================
// Add services
// ==========================
builder.Services.AddControllersWithViews();

// Kết nối SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);


// Đăng ký UserRepository và UserService cho DI
builder.Services.AddScoped<duAn1.Repository.UserRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<CryUtils>();
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductRepository>();
// Khi hệ thống cần IAuthService → hãy tạo ra AuthService để dùng.
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// ==========================
// Configure Middleware
// ==========================
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
