using duAn1.Models;
using duAn1.Services;
using duAn1.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;

namespace duAn1.Controllers
{
    public class OrderController : Controller
    {
        private readonly ILogger<OrderController> _logger;
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;

        public OrderController(
            ILogger<OrderController> logger,
            IAuthService authService,
            AppDbContext context
        )
        {
            _logger = logger;
            _authService = authService;
            _context = context;
        }

        public IActionResult Index(int? status)
        {
            int? userId = _authService.GetUserId(HttpContext);

            if (userId == null)
            {
                return RedirectToAction("Index", "Login", new { error = "needlogin" });
            }

            // Lấy tất cả đơn hàng của người dùng hiện tại
            var orders = _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreateDate)
                .ToList();

            // Thêm thông tin OrderDetail vào ViewBag
            var orderDetailsMap = new Dictionary<int, List<dynamic>>();
            foreach (var order in orders)
            {
                var orderDetails = _context.OrderDetails
                    .Where(od => od.OrderId == order.Id)
                    .Include(od => od.Product)
                    .ToList();

                // Tạo list dynamic với thông tin cần thiết
                var detailsList = new List<dynamic>();
                foreach (var detail in orderDetails)
                {
                    detailsList.Add(new
                    {
                        Id = detail.Id,
                        ProductName = detail.Product?.Name ?? "Không rõ",
                        ProductImage = detail.Product?.Image ?? "/images/no-image.jpg",
                        Price = detail.Price,
                        Quantity = detail.Quantity,
                        Total = detail.Price * detail.Quantity
                    });
                }
                orderDetailsMap[order.Id] = detailsList;
            }

            ViewBag.OrderDetailsMap = orderDetailsMap;
            ViewBag.CurrentStatus = status;
            return View("~/Views/Order/OrderIndex.cshtml", orders);
        }

        // Lấy danh sách chi tiết của một đơn hàng
        public IActionResult GetOrderDetails(int orderId)
        {
            int? userId = _authService.GetUserId(HttpContext);

            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var order = _context.Orders
                .FirstOrDefault(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại" });
            }

            var orderDetails = _context.OrderDetails
                .Where(od => od.OrderId == orderId)
                .Include(od => od.Product)
                .ToList();

            return Json(new
            {
                success = true,
                order = new
                {
                    id = order.Id,
                    address = order.Address,
                    createDate = order.CreateDate.ToString("dd/MM/yyyy HH:mm"),
                    status = order.payment_status,
                    statusText = GetStatusText(order.payment_status)
                },
                details = orderDetails.Select(od => new
                {
                    id = od.Id,
                    productName = od.Product?.Name,
                    productImage = od.Product?.Image,
                    price = od.Price,
                    quantity = od.Quantity,
                    total = od.Price * od.Quantity
                })
            });
        }

        // Hủy đơn hàng
        [HttpPost]
        public IActionResult CancelOrder(int orderId)
        {
            int? userId = _authService.GetUserId(HttpContext);

            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var order = _context.Orders
                .FirstOrDefault(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại" });
            }

            // Kiểm tra trạng thái đơn hàng - chỉ có thể hủy nếu status = 0
            if (order.payment_status == 0)
            {
                // Chuyển status sang 3 (Đã hủy)
                order.payment_status = 3;
                _context.SaveChanges();
                return Json(new { success = true, message = "Hủy đơn hàng thành công" });
            }
            else if (order.payment_status == 1)
            {
                return Json(new { success = false, message = "Đơn hàng đã được xác nhận bởi admin và đang giao. Không thể hủy" });
            }
            else if (order.payment_status == 2)
            {
                return Json(new { success = false, message = "Đơn hàng đã hoàn thành. Không thể hủy" });
            }
            else if (order.payment_status == 3)
            {
                return Json(new { success = false, message = "Đơn hàng đã được hủy trước đó" });
            }
            else if (order.payment_status == 4)
            {
                return Json(new { success = false, message = "Đơn hàng bị từ chối bởi admin. Không thể hủy" });
            }

            return Json(new { success = false, message = "Không thể hủy đơn hàng này" });
        }

        // Xác nhận đã nhận hàng (client)
        [HttpPost]
        public IActionResult ConfirmReceivedOrder(int orderId)
        {
            int? userId = _authService.GetUserId(HttpContext);

            if (userId == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập" });
            }

            var order = _context.Orders
                .FirstOrDefault(o => o.Id == orderId && o.UserId == userId);

            if (order == null)
            {
                return Json(new { success = false, message = "Đơn hàng không tồn tại" });
            }

            // Kiểm tra trạng thái đơn hàng - chỉ có thể xác nhận nếu status = 1
            if (order.payment_status == 1)
            {
                // Chuyển status sang 2 (Đã nhận)
                order.payment_status = 2;
                _context.SaveChanges();
                return Json(new { success = true, message = "Xác nhận nhận hàng thành công" });
            }
            else if (order.payment_status == 0)
            {
                return Json(new { success = false, message = "Đơn hàng chưa được xác nhận bởi admin. Vui lòng chờ" });
            }
            else if (order.payment_status == 2)
            {
                return Json(new { success = false, message = "Đơn hàng đã được xác nhận nhận hàng trước đó" });
            }
            else if (order.payment_status == 3)
            {
                return Json(new { success = false, message = "Đơn hàng đã bị hủy. Không thể xác nhận" });
            }
            else if (order.payment_status == 4)
            {
                return Json(new { success = false, message = "Đơn hàng bị từ chối bởi admin. Không thể xác nhận" });
            }

            return Json(new { success = false, message = "Không thể xác nhận đơn hàng này" });
        }

        private string GetStatusText(int status)
        {
            return status switch
            {
                0 => "Chờ xác nhận",
                1 => "Đang giao",
                2 => "Đã nhận được",
                3 => "Đã hủy",
                4 => "Từ chối",
                _ => "Không rõ"
            };
        }

        private string GetStatusColor(int status)
        {
            return status switch
            {
                0 => "amber",    // Chờ xác nhận - vàng
                1 => "blue",     // Đang giao - xanh
                2 => "green",    // Đã nhận - xanh lá
                3 => "red",      // Đã hủy - đỏ
                4 => "gray",     // Từ chối - xám
                _ => "slate"
            };
        }
    }
}
