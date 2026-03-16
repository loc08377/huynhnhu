using System;

namespace duAn1.Utils
{
    public static class PriceFormatter
    {
        /// <summary>
        /// Format giá tiền theo định dạng VND chuẩn
        /// Ví dụ: 1000000 -> "1.000.000 VNĐ"
        /// </summary>
        public static string FormatVND(int? price)
        {
            if (price == null || price == 0)
                return "0 VNĐ";

            return price.Value.ToString("N0") + " VNĐ";
        }

        /// <summary>
        /// Format giá tiền theo định dạng VND chuẩn
        /// Ví dụ: 1000000 -> "1.000.000 VNĐ"
        /// </summary>
        public static string FormatVND(long? price)
        {
            if (price == null || price == 0)
                return "0 VNĐ";

            return price.Value.ToString("N0") + " VNĐ";
        }

        /// <summary>
        /// Format giá tiền theo định dạng VND chuẩn
        /// Ví dụ: 1000000 -> "1.000.000 VNĐ"
        /// </summary>
        public static string FormatVND(decimal? price)
        {
            if (price == null || price == 0)
                return "0 VNĐ";

            return ((long)price.Value).ToString("N0") + " VNĐ";
        }
    }
}
