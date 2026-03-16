// Format giá tiền theo kiểu VND
// Ví dụ: 1000000 -> "1.000.000 VNĐ"
function formatVND(price) {
    if (!price || price === 0) {
        return '0 VNĐ';
    }
    
    // Chuyển thành số nguyên nếu là chuỗi
    const numPrice = typeof price === 'string' ? parseInt(price) : price;
    
    // Format với dấu chấm phân cách hàng ngàn
    return numPrice.toLocaleString('vi-VN') + ' VNĐ';
}

// Hàm này có thể được gọi sau khi DOM thay đổi để update tất cả giá đã format sẵn
function updatePriceDisplay(selector, priceValue) {
    const element = document.querySelector(selector);
    if (element) {
        element.textContent = formatVND(priceValue);
    }
}
