
using duAn1.Models;
using duAn1.Services;
using duAn1.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace duAn1.Controllers
{
    public class CreateOrderRequest
    {
        public string? Address { get; set; }
        public int[]? CartIds { get; set; }
    }

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
        public IActionResult Index(string error)
        {
            int? userId = _authService.GetUserId(HttpContext);

            if (userId == null)
            {
                TempData["waring"] = "Phiên bản đăng nhập hết hạn!";
                return RedirectToAction("Login", "Home");
            }
            Message.HandleError(TempData, error);
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
                
                if (userId == null)
                {
                    return Json(new
                    {
                        status = false,
                        cartCount = 0,
                        message = "Chưa đăng nhập"
                    });
                }
                
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

        [HttpPost]
        public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                int? userId = _authService.GetUserId(HttpContext);
                if (userId == null)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập" });
                }

                string? address = request.Address;
                var cartIds = request.CartIds;

                if (string.IsNullOrWhiteSpace(address) || cartIds == null || cartIds.Length == 0)
                {
                    return Json(new { success = false, message = "Thông tin không hợp lệ" });
                }

                // Tạo đơn hàng mới
                var order = new Order
                {
                    UserId = userId,
                    Address = address,
                    CreateDate = DateTime.Now,
                    Status = false,
                    payment_status = 0  // 0 = Chờ xác nhận
                };

                _context.Orders.Add(order);
                _context.SaveChanges();

                // Lấy thông tin các sản phẩm từ cart
                foreach (var cartId in cartIds!)
                {
                    var cart = _context.Carts
                        .Where(c => c.Id == (int)cartId && c.UserId == userId)
                        .Include(c => c.Product)
                        .FirstOrDefault();

                    if (cart != null && cart.Product != null)
                    {
                        // Tạo OrderDetail
                        var orderDetail = new OrderDetail
                        {
                            OrderId = order.Id,
                            ProductId = cart.ProductId,
                            Price = cart.Product.Price ?? 0,
                            Quantity = cart.Quantity
                        };

                        _context.OrderDetails.Add(orderDetail);

                        // Xóa item khỏi cart
                        _context.Carts.Remove(cart);
                    }
                }

                _context.SaveChanges();

                return Json(new 
                { 
                    success = true, 
                    message = "Đặt hàng thành công", 
                    orderId = order.Id 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}