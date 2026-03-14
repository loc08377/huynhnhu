// Gọi updateCartCount khi trang load để luôn cập nhật số lượng giỏ hàng
window.addEventListener('DOMContentLoaded', function() {
    updateCartCount();
});


function addCart(productId, quantity) {
    if (!productId) {
        toastr.warning("Không tìm thấy sản phẩm");
        return;
    }
    var qty = quantity && !isNaN(quantity) && quantity > 0 ? quantity : 1;
    customFetch(`/Cart/AddToCart?productId=${productId}&quantity=${qty}`)
        .then(res => res.json())
        .then(data => {
            if (data.redirect) {
                window.location.href = data.redirect;
                return;
            }
            if (data.status) {
                toastr.success(data.message);
                updateCartCount();
            } else {
                toastr.error(data.message);
            }
        })
        .catch(() => {
            return;
        });
}


function updateCartCount() {
    customFetch(`/Cart/CountCartBag`)
        .then(res => res.json())
        .then(data => {
            let badge = document.getElementById("cart-count");
            if (!badge) return;
            if (data.status) {
                let count = data.cartCount;
                if (count > 0) {
                    badge.innerText = count;
                    badge.classList.remove("hidden");
                } else {
                    badge.innerText = 0;
                    badge.classList.add("hidden");
                }
            } else {
                // Nếu chưa đăng nhập thì không hiện thông báo lỗi
                if (!data.message || !/đăng nhập|login|chưa đăng nhập|not authenticated/i.test(data.message)) {
                    return
                }
            }
        })
        .catch(() => {
            return;
        });
}

