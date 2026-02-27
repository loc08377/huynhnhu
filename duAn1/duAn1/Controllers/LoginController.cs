using duAn1.Models;
using duAn1.Services;
using Microsoft.AspNetCore.Mvc;

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
        public IActionResult Login(string email, string password)
        {
            var user = _userService.userByEmail(email);

            if (user == null)
            {
                user.Email = email;
                ModelState.AddModelError("", "Email hoặc mật khẩu chưa đúng!");
                return View("~/Views/Login/Index.cshtml", user);
            }

            bool isValid = _authService.VerifyPassword(user, password);

            if (!isValid)
            {
                user.Email = email;
                ModelState.AddModelError("", "Email hoặc mật khẩu chưa đúng!");
                return View("~/Views/Login/Index.cshtml", user);
            }

            // Nếu đúng -> đăng nhập thành công
           // HttpContext.Session.SetString("UserEmail", user.Email);

            return View("~/Views/Home/Index.cshtml");
        }
    }
}