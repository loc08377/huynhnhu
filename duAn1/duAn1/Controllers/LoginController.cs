using duAn1.Models;
using duAn1.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace duAn1.Controllers
{
    public class LoginController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserService _userService;
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;

        public LoginController(
            ILogger<HomeController> logger,
            UserService userService,
            IAuthService authService,
            AppDbContext context
        )
        {
            _logger = logger;
            _userService = userService;
            _authService = authService;
            _context = context;
        }


        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = _userService.userByEmail(email);

            if (user == null)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu chưa đúng!");
                return View("~/Views/Login/Index.cshtml");
            }

            bool isValid = _authService.VerifyPassword(user, password);

            if (!isValid)
            {
                ModelState.AddModelError("", "Email hoặc mật khẩu chưa đúng!");
                return View("~/Views/Login/Index.cshtml");
            }

            // tạo claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Email),
                new Claim("UserId", user.Id.ToString()),
                new Claim("Role", user.Role.ToString())
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );

            return RedirectToAction("Index", "Home");
        }
    }
}