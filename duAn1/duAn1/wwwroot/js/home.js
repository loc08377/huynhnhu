// Gọi updateCartCount khi trang load để luôn cập nhật số lượng giỏ hàng
window.addEventListener('DOMContentLoaded', function() {
    updateCartCount();
});

function addCart(productId) {

    if (!productId) {
        toastr.warning("Không tìm thấy sản phẩm");
        return;
    }

    fetch(`/Cart/AddToCart?productId=${productId}&quantity=1`)
        .then(res => res.json())
        .then(data => {

            if (data.redirect) {
                window.location.href = data.redirect;
                return;
            }

            if (data.status) {

                toastr.success(data.message);

                // cập nhật số lượng cart
                updateCartCount();

            } else {
                toastr.error(data.message);
            }

        })
        .catch(() => {
            toastr.error("Lỗi kết nối server");
        });
}


function updateCartCount() {
    fetch(`/Cart/CountCartBag`)
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
                toastr.error(data.message);
            }
        })
        .catch(() => {
            toastr.error("Lỗi kết nối server");
        });
}

