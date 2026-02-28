using duAn1.Models;
using duAn1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace duAn1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserService _userService;
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;

        public HomeController(
            ILogger<HomeController> logger,
            UserService userService,
            IAuthService authService,
            ProductService productService,
            CategoryService categoryService,
            AppDbContext context
        )
        {
            _logger = logger;
            _userService = userService;
            _authService = authService;
            _productService = productService;
            _categoryService = categoryService;
            _context = context;   
        }

        public IActionResult Index()
        {
            return View(_productService.GetProducts());
        }

        public IActionResult Login()
        {
            return View("~/Views/Login/Index.cshtml");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View("~/Views/Login/Register.cshtml");
        }

        [HttpPost]
        public IActionResult Register(User user)
        {
            if (_userService.userByEmail(user.Email) != null)
            {
                ModelState.AddModelError("Email", "Email đã tồn tại");
                return View("~/Views/Login/Register.cshtml", user);
            }
            user.Password = _authService.HashPassword(user, user.Password);

            _context.Users.Add(user);
                    _context.SaveChanges();

            return View("~/Views/Login/Index.cshtml");
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
        [HttpGet]
        public IActionResult Collection()
        {
            return View("~/Views/CollectionProduct/CollectionIndex.cshtml", (_productService.GetProducts(), _categoryService.GetCategories()));
        }
        [HttpPost]
        public IActionResult Collection(int idCategory)
        {
            return View("~/Views/CollectionProduct/CollectionIndex.cshtml", _productService.getProductByCategory(idCategory));
        }
    }
}