using duAn1.Models;
using duAn1.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using System.Diagnostics;

namespace duAn1.Controllers
{
    public class ProductController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly UserService _userService;
        private readonly ProductService _productService;
        private readonly CategoryService _categoryService;
        private readonly IAuthService _authService;
        private readonly AppDbContext _context;

        public ProductController(
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

        [HttpGet]
        public IActionResult ProductDetail(int id)
        {
            var product = _productService.GetProductById(id);
            if (product == null)
            {
                return NotFound();
            }
            return View("~/Views/CollectionProduct/ProductDetail.cshtml", product);
        }
    }
}