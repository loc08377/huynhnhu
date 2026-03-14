using duAn1.Models;
using duAn1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace duAn1.Controllers
{
    public class UpdateOrderStatusRequest
    {
        public int Status { get; set; }
    }

    [Route("AdminJewel")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly UserService _userService;
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;

        public AdminController(
            ILogger<AdminController> logger,
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
        [Route("Home/Index")]
        public IActionResult Index()
        {
            return View("~/Views/AdminJewel/Components/Dashboard.cshtml");
        }

        [Route("Dashboard")]
        public IActionResult Dashboard()
        {
            try
            {
                // Tính doanh thu tháng hiện tại
                var currentMonth = DateTime.Now.Month;
                var currentYear = DateTime.Now.Year;
                
                // Lấy tất cả orders với 1 tháng gần đây (để tránh lỗi SQL translation)
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                var monthlyRevenue = _context.Orders
                    .Where(o => o.CreateDate >= thirtyDaysAgo)
                    .Include(o => o.OrderDetails)
                    .AsEnumerable() // Client-side evaluation từ đây
                    .Where(o => o.CreateDate.Month == currentMonth && o.CreateDate.Year == currentYear)
                    .SelectMany(o => o.OrderDetails!)
                    .Sum(od => od.Price * od.Quantity);

                // Số đơn hàng mới (chờ xác nhận - status = 0)
                var newOrders = _context.Orders.Count(o => o.payment_status == 0);

                // Tổng số sản phẩm
                var totalProducts = _context.Products.Count();

                // Tổng khách hàng
                var totalCustomers = _context.Users.Count(u => u.Role != 1); // 1 là admin

                // Danh sách đơn hàng gần đây
                var recentOrders = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails)
                    .OrderByDescending(o => o.CreateDate)
                    .Take(10)
                    .ToList();

                ViewBag.MonthlyRevenue = (int)monthlyRevenue;
                ViewBag.NewOrders = newOrders;
                ViewBag.TotalProducts = totalProducts;
                ViewBag.TotalCustomers = totalCustomers;
                ViewBag.RecentOrders = recentOrders;

                return View("~/Views/AdminJewel/Components/Dashboard.cshtml");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading dashboard");
                TempData["error"] = "Lỗi tải dashboard: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        [Route("ProductManager")]
        public IActionResult ProductManager()
        {
            var products = _context.Products.Include(p => p.Category).ToList();
            var categories = _context.Categories.ToList();
            ViewBag.Categories = categories;
            return View("~/Views/AdminJewel/Components/ProductManager.cshtml", products);
        }

        [Route("AddProduct")]
        [HttpPost]
        public IActionResult AddProduct(string name, int categoryId, int price, string description, string image)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || price <= 0)
                {
                    return Json(new { success = false, message = "Tên sản phẩm và giá không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(image))
                {
                    return Json(new { success = false, message = "Vui lòng nhập link hình ảnh" });
                }

                var category = _context.Categories.Find(categoryId);
                if (category == null)
                {
                    return Json(new { success = false, message = "Danh mục không tồn tại" });
                }

                var product = new Product
                {
                    Name = name,
                    CategoryId = categoryId,
                    Price = price,
                    Description = description,
                    Image = image.Trim(),
                    Actived = true,
                    CreatedDate = DateTime.Now
                };

                _context.Products.Add(product);
                _context.SaveChanges();

                return Json(new { success = true, message = "Thêm sản phẩm thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [Route("DeleteProduct/{id}")]
        [HttpPost]
        public IActionResult DeleteProduct(int id)
        {
            try
            {
                var product = _context.Products.Find(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                // Soft delete: set Actived = false
                product.Actived = false;
                _context.SaveChanges();

                return Json(new { success = true, message = "Xóa sản phẩm thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [Route("EditProduct/{id}")]
        [HttpPost]
        public IActionResult EditProduct(int id, string name, int categoryId, int price, string description, string image)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name) || price <= 0)
                {
                    return Json(new { success = false, message = "Tên sản phẩm và giá không hợp lệ" });
                }

                if (string.IsNullOrWhiteSpace(image))
                {
                    return Json(new { success = false, message = "Vui lòng nhập link hình ảnh" });
                }

                var product = _context.Products.Find(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                }

                var category = _context.Categories.Find(categoryId);
                if (category == null)
                {
                    return Json(new { success = false, message = "Danh mục không tồn tại" });
                }

                product.Name = name;
                product.CategoryId = categoryId;
                product.Price = price;
                product.Description = description;
                product.Image = image.Trim();

                _context.SaveChanges();

                return Json(new { success = true, message = "Cập nhật sản phẩm thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [Route("OrderManager")]
        public IActionResult OrderManager()
        {
            try
            {
                // Load all orders with user and order details
                var allOrders = _context.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderDetails)
                    .OrderByDescending(o => o.CreateDate)
                    .ToList();

                // Create a map of order details for easy access
                var orderDetailsMap = new Dictionary<int, List<dynamic>>();
                foreach (var order in allOrders)
                {
                    if (order.OrderDetails != null)
                    {
                        var details = new List<dynamic>();
                        foreach (var od in order.OrderDetails)
                        {
                            var product = _context.Products.Find(od.ProductId);
                            details.Add(new
                            {
                                ProductId = od.ProductId,
                                ProductName = product?.Name ?? "N/A",
                                ProductImage = product?.Image ?? "no-image.jpg",
                                Quantity = od.Quantity,
                                Price = od.Price,
                                Total = od.Price * od.Quantity
                            });
                        }
                        orderDetailsMap[order.Id] = details;
                    }
                }

                ViewBag.OrderDetailsMap = orderDetailsMap;
                return View("~/Views/AdminJewel/Components/OrderManager.cshtml", allOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading order manager");
                TempData["error"] = "Lỗi tải danh sách đơn hàng: " + ex.Message;
                return RedirectToAction("Dashboard");
            }
        }

        [Route("UserManager")]
        public IActionResult UserManager()
        {
            return View("~/Views/AdminJewel/Components/UserManager.cshtml");
        }

        [Route("UpdateOrderStatus/{orderId}")]
        [HttpPost]
        public IActionResult UpdateOrderStatus(int orderId, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                int newStatus = request.Status;
                var order = _context.Orders.Find(orderId);
                
                if (order == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
                }

                // Kiểm tra status hiện tại trước khi cập nhật
                // Chỉ cho phép xác nhận hoặc từ chối nếu status = 0
                if (order.payment_status != 0)
                {
                    if (newStatus == 1 || newStatus == 4) // Xác nhận hoặc từ chối
                    {
                        return Json(new { success = false, message = "Đơn hàng không ở trạng thái chờ xác nhận" });
                    }
                }

                // Kiểm tra chuyển đổi trạng thái hợp lệ
                // 0 -> 1 (xác nhận)
                // 0 -> 4 (từ chối)
                if (order.payment_status == 0)
                {
                    if (newStatus != 1 && newStatus != 4)
                    {
                        return Json(new { success = false, message = "Trạng thái chuyển đổi không hợp lệ" });
                    }
                }
                else if (order.payment_status == 1)
                {
                    // Status 1 (đang giao) - không được phép cập nhật từ admin side
                    return Json(new { success = false, message = "Đơn hàng đang giao. Không thể cập nhật" });
                }
                else if (order.payment_status == 2)
                {
                    return Json(new { success = false, message = "Đơn hàng đã hoàn thành" });
                }
                else if (order.payment_status == 3)
                {
                    return Json(new { success = false, message = "Đơn hàng đã bị hủy bởi client" });
                }
                else if (order.payment_status == 4)
                {
                    return Json(new { success = false, message = "Đơn hàng đã bị từ chối" });
                }

                order.payment_status = newStatus;
                _context.SaveChanges();

                var successMsg = newStatus == 1 ? "Xác nhận đơn hàng thành công" : "Từ chối đơn hàng thành công";
                return Json(new { success = true, message = successMsg });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status");
                return Json(new { success = false, message = "Lỗi hệ thống: " + ex.Message });
            }
        }
    }
}
