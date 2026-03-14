using duAn1.Models;
using duAn1.Services;
using duAn1.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
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

        public IActionResult Index(string error)
        {
            Message.HandleError(TempData, error);
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
            if (!ModelState.IsValid)
            {
                return View("~/Views/Login/Register.cshtml", user);
            }

            var existUser = _userService.userByEmail(user.Email);

            if (existUser != null)
            {
                ModelState.AddModelError("Email", "Email đã tồn tại");
                return View("~/Views/Login/Register.cshtml", user);
            }

            // Hash password
            user.Password = _authService.HashPassword(user, user.Password);

            _context.Users.Add(user);
            _context.SaveChanges();

            Message.HandleSuccess(TempData, "Đăng ký thành công!");

            return RedirectToAction("Index", "Login");
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
        public IActionResult Collection(int? categoryId)
        {
            List<Product> products;

            if (!categoryId.HasValue || categoryId <= 0)
            {
                products = _productService.GetProducts();
            }
            else
            {
                products = _productService.getProductByCategory(categoryId.Value);
            }

            var categories = _categoryService.GetCategories();

            return View("~/Views/CollectionProduct/CollectionIndex.cshtml", (products, categories, categoryId));
        }

        public IActionResult loadProductList(int? categoryId)
        {
            List<Product> products;

            if (!categoryId.HasValue || categoryId <= 0)
            {
                products = _productService.GetProducts();
            }
            else
            {
                products = _productService.getProductByCategory(categoryId.Value);
            }

            return PartialView("~/Views/CollectionProduct/CollectionList.cshtml", products);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            Response.Cookies.Delete("duAn1Auth");
            return RedirectToAction("Index", "Home");
        }
    }
}