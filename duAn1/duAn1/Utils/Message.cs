using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace duAn1.Utils
{
    public static class Message
    {
        /// <summary>
        /// Xử lý các thông báo lỗi dựa trên mã lỗi và lưu vào TempData
        /// </summary>
        /// <param name="tempData">TempData từ Controller</param>
        /// <param name="error">Mã lỗi (403, 404, 500, expired, needlogin,...)</param>
        public static void HandleError(ITempDataDictionary tempData, string error)
        {
            if (string.IsNullOrEmpty(error))
                return;

            switch (error.ToLower())
            {
                case "403":
                    tempData["error"] = "Bạn không có quyền truy cập";
                    break;

                case "404":
                    tempData["error"] = "Trang không tồn tại hoặc đang phát triển";
                    break;

                case "500":
                case "expired":
                    tempData["warning"] = "Phiên đăng nhập không tồn tại hoặc đã hết hạn. Vui lòng đăng nhập lại.";
                    break;

                case "needlogin":
                    tempData["warning"] = "Bạn cần đăng nhập để truy cập.";
                    break;
                case "locked":
                    tempData["warning"] = "Tài khoản hiện không hoạt động.";
                    break;
                // Thêm các case khác nếu cần
                default:
                    break;
            }
        }

        /// <summary>
        /// Xử lý thông báo success
        /// </summary>
        public static void HandleSuccess(ITempDataDictionary tempData, string message)
        {
            if (!string.IsNullOrEmpty(message))
                tempData["Success"] = message;
        }

        /// <summary>
        /// Xử lý thông báo warning
        /// </summary>
        public static void HandleWarning(ITempDataDictionary tempData, string message)
        {
            if (!string.IsNullOrEmpty(message))
                tempData["warning"] = message;
        }
    }
}
