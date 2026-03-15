using duAn1.Models;
using duAn1.Repository;
using duAn1.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

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

// Dependency Injection
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<UserService>();

builder.Services.AddScoped<CryUtils>();

builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<ProductRepository>();

builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<CategoryLINQ>();

builder.Services.AddScoped<CartLINQ>();
builder.Services.AddScoped<CartService>();

builder.Services.AddScoped<AIRecommendationService>();
builder.Services.AddScoped<GeminiAIService>();

builder.Services.AddScoped<FavoriteService>();

builder.Services.AddScoped<IAuthService, AuthService>();

// ==========================
// Authentication Cookie
// ==========================

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login/Index";        // chưa login → về login
        options.LogoutPath = "/Login/Logout";
        options.AccessDeniedPath = "/Login/Index";

        options.Cookie.Name = "duAn1Auth";

           options.ExpireTimeSpan = TimeSpan.FromMinutes(100); // Cookie chỉ sống trong bao lau 
    });

builder.Services.AddAuthorization();

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

// BẮT BUỘC phải có
app.UseAuthentication();

// middleware check login giống doFilter
app.UseMiddleware<AuthMiddleware>();

app.UseAuthorization();

// Route mặc định
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();