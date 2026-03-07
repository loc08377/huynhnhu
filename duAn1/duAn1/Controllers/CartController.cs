
using duAn1.Models;
using duAn1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace duAn1.Controllers
{
    public class CartController : Controller
    {
        private readonly ILogger<CartController> _logger;
        private readonly CartService _cartService;
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;

        public CartController(
            ILogger<CartController> logger,
            CartService cartService,
            IAuthService authService,
            AppDbContext context
        )
        {
            _logger = logger;
            _cartService = cartService;
            _authService = authService;
            _context = context;

        }
        public IActionResult Index()
        {
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
            return View("~/Views/Cart/CartIndex.cshtml", _cartService.GetCartsByUserId(userId));
        }
        public IActionResult AddToCart(int productId, int quantity)
        {
            try
            {
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

                var cartCheck = _cartService.CheckAddProductInCart(productId, userId.Value);

                if (cartCheck != null)
                {
                    cartCheck.Quantity += quantity;
                    cartCheck.CreateDate = DateTime.Now;
                    _context.SaveChanges();
                }
                else
                {
                    Cart cart = new Cart
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        UserId = userId.Value,
                        CreateDate = DateTime.Now,
                        Checked = false
                    };

                    _cartService.InsertCart(cart);
                }

                return Json(new
                {
                    status = true,
                    message = "Thêm vào giỏ hàng thành công",
                });
            }
            catch
            {
                return Json(new
                {
                    status = false,
                    message = "Lỗi hệ thống"
                });
            }
        }

        public IActionResult CountCartBag()
        {
            try
            {
                int? userId = _authService.GetUserId(HttpContext);
                int cartCount = _cartService.GetCartCount(userId.Value);

                return Json(new
                {
                    status = true,
                    cartCount = cartCount
                });
            }
            catch
            {
                return Json(new
                {
                    status = false,
                    message = "Lỗi hệ thống"
                });
            }
        }
        public IActionResult updateQuantity(int cartId, int quantity)
        {
            try
            {
                int? userId = _authService.GetUserId(HttpContext);
                if (userId == null)
                {
                    return Json(new { status = false, message = "Vui lòng đăng nhập" });
                }
                if (quantity < 1)
                {
                    return Json(new { status = false, message = "Số lượng tối thiểu là 1" });
                }
                var success = _cartService.UpdateQuantity(cartId, quantity);
                if (!success)
                {
                    return Json(new { status = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }
                return Json(new
                {
                    status = true
                });
            }
            catch
            {
                return Json(new
                {
                    status = false,
                    message = "Lỗi hệ thống"
                });
            }
        }
        [HttpPost]
        public IActionResult RemoveCartItem(int cartId)
        {
            try
            {
                int? userId = _authService.GetUserId(HttpContext);
                if (userId == null)
                {
                    return Json(new { status = false, message = "Vui lòng đăng nhập" });
                }
                var success = _cartService.RemoveCartItem(cartId);
                if (!success)
                {
                    return Json(new { status = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                }
                return Json(new { status = true });
            }
            catch
            {
                return Json(new { status = false, message = "Lỗi hệ thống" });
            }
        }
    }
}