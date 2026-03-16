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
        private readonly FavoriteService _favoriteService;

        public HomeController(
            ILogger<HomeController> logger,
            UserService userService,
            IAuthService authService,
            ProductService productService,
            CategoryService categoryService,
            AppDbContext context,
            FavoriteService favoriteService
        )
        {
            _logger = logger;
            _userService = userService;
            _authService = authService;
            _productService = productService;
            _categoryService = categoryService;
            _context = context;
            _favoriteService = favoriteService;
        }

        public IActionResult Index(string error)
        {
            Message.HandleError(TempData, error);
            var viewModel = new HomeViewModel
            {
                NewestProducts = _productService.GetNewestProducts(10),
                BestSellingProducts = _productService.GetBestSellingProducts(30)
            };
            return View(viewModel);
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

            var existUser = _userService.userByEmail(user.Email ?? "");

            if (existUser != null)
            {
                ModelState.AddModelError("Email", "Email đã tồn tại");
                return View("~/Views/Login/Register.cshtml", user);
            }

            // Hash password
            user.Password = _authService.HashPassword(user, user.Password ?? "");

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
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync();
            Response.Cookies.Delete("duAn1Auth");
            return RedirectToAction("Index", "Home");
        }

        // Change Password API Endpoint
        [HttpPost]
        [Route("api/change-password")]
        public IActionResult ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // Check if user is authenticated
                int? userId = _authService.GetUserId(HttpContext);
                if (userId == null)
                {
                    return Json(new { success = false, message = "Phiên bản đăng nhập hết hạn!" });
                }

                // Validate input
                if (string.IsNullOrWhiteSpace(request.OldPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return Json(new { success = false, message = "Vui lòng điền đầy đủ thông tin" });
                }

                if (request.NewPassword.Length < 6)
                {
                    return Json(new { success = false, message = "Mật khẩu mới phải có ít nhất 6 kí tự" });
                }

                // Get current user
                var user = _context.Users.FirstOrDefault(u => u.Id == userId);
                if (user == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy người dùng" });
                }

                // Verify old password
                bool isPasswordValid = _authService.VerifyPassword(user, request.OldPassword);
                if (!isPasswordValid)
                {
                    return Json(new { success = false, message = "Mật khẩu cũ không chính xác" });
                }

                // Hash new password
                user.Password = _authService.HashPassword(user, request.NewPassword);

                // Save to database
                _context.Users.Update(user);
                _context.SaveChanges();

                _logger.LogInformation($"User {userId} changed password successfully");

                return Json(new { success = true, message = "Đổi mật khẩu thành công!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // Favorites endpoints
        [HttpGet]
        public IActionResult Favorites()
        {
            int? userId = _authService.GetUserId(HttpContext);

            if (userId == null)
            {
                TempData["warning"] = "Phiên bản đăng nhập hết hạn!";
                return RedirectToAction("Login", "Home");
            }

            var favorites = _favoriteService.GetFavoritesByUserId(userId.Value);
            return View("~/Views/Home/Favorites.cshtml", favorites);
        }

        [HttpPost]
        public IActionResult ToggleFavorite(int productId)
        {
            try
            {
                if (productId <= 0)
                {
                    return Json(new
                    {
                        status = false,
                        message = "Sản phẩm không hợp lệ"
                    });
                }

                int? userId = _authService.GetUserId(HttpContext);

                if (userId == null)
                {
                    return Json(new
                    {
                        status = false,
                        message = "Vui lòng đăng nhập",
                        redirect = "/Login"
                    });
                }

                var (success, isFavorited) = _favoriteService.ToggleFavorite(userId.Value, productId);

                if (!success)
                {
                    return Json(new
                    {
                        status = false,
                        message = "Lỗi khi cập nhật yêu thích"
                    });
                }

                return Json(new
                {
                    status = true,
                    isFavorited = isFavorited,
                    message = isFavorited ? "Đã thêm vào yêu thích" : "Đã xóa khỏi yêu thích"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = false,
                    message = "Lỗi: " + ex.Message
                });
            }
        }

        [HttpGet]
        public IActionResult IsFavorited(int productId)
        {
            try
            {
                if (productId <= 0)
                {
                    return Json(new { isFavorited = false });
                }

                int? userId = _authService.GetUserId(HttpContext);

                if (userId == null)
                {
                    return Json(new { isFavorited = false });
                }

                bool isFavorited = _favoriteService.IsFavorited(userId.Value, productId);
                return Json(new { isFavorited });
            }
            catch
            {
                return Json(new { isFavorited = false });
            }
        }

        [HttpGet]
        public IActionResult FavoriteCount()
        {
            try
            {
                int? userId = _authService.GetUserId(HttpContext);

                if (userId == null)
                {
                    return Json(new { count = 0 });
                }

                int count = _favoriteService.GetFavoriteCount(userId.Value);
                return Json(new { count });
            }
            catch
            {
                return Json(new { count = 0 });
            }
        }
    }
}