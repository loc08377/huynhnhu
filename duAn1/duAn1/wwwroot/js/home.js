// Gọi updateCartCount khi trang load để luôn cập nhật số lượng giỏ hàng
window.addEventListener('DOMContentLoaded', function() {
    updateCartCount();
    updateFavoriteCount();
    loadFavoriteStates();
});


function addCart(productId, quantity) {
    if (!productId) {
        toastr.warning("Không tìm thấy sản phẩm");
        return;
    }
    var qty = quantity && !isNaN(quantity) && quantity > 0 ? quantity : 1;
    customFetch(`/Cart/AddToCart?productId=${productId}&quantity=${qty}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
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

// Favorite functions
function toggleFavorite(productId, btn) {
    if (!btn || !productId || productId <= 0) {
        toastr.error('Lỗi: Sản phẩm không hợp lệ');
        return;
    }
    
    customFetch(`/Home/ToggleFavorite?productId=${productId}`, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json'
        }
    })
        .then(res => res.json())
        .then(data => {
            if (data.redirect) {
                window.location.href = data.redirect;
                return;
            }
            
            if (data.status) {
                if (data.isFavorited) {
                    btn.classList.add('favorite-active');
                    btn.title = 'Xóa khỏi yêu thích';
                    // Update button appearance
                    updateFavoriteBtnStyle(btn, true);
                } else {
                    btn.classList.remove('favorite-active');
                    btn.title = 'Thêm vào yêu thích';
                    // Update button appearance
                    updateFavoriteBtnStyle(btn, false);
                }
                toastr.success(data.message);
                updateFavoriteCount();
            } else {
                toastr.error(data.message);
            }
        })
        .catch(err => {
            toastr.error('Lỗi: ' + err.message);
        });
}

function updateFavoriteBtnStyle(btn, isFavorited) {
    if (isFavorited) {
        btn.style.backgroundColor = 'rgba(236, 72, 153, 0.3)';
        btn.style.color = 'rgb(236, 72, 153)';
    } else {
        btn.style.backgroundColor = 'rgba(255, 255, 255, 0.8)';
        btn.style.color = 'rgb(236, 72, 153)';
    }
}

function updateFavoriteCount() {
    customFetch(`/Home/FavoriteCount`)
        .then(res => res.json())
        .then(data => {
            let badge = document.getElementById("favorite-count");
            if (!badge) return;
            
            let count = data.count || 0;
            if (count > 0) {
                badge.innerText = count;
                badge.classList.remove("hidden");
            } else {
                badge.classList.add("hidden");
            }
        })
        .catch(() => {
            return;
        });
}

function loadFavoriteStates() {
    // Get all favorite buttons on the page
    const buttons = document.querySelectorAll('.favorite-btn');
    buttons.forEach(btn => {
        const productId = btn.getAttribute('onclick').match(/\d+/)[0];
        checkFavoriteState(productId, btn);
    });
}

function checkFavoriteState(productId, btn) {
    customFetch(`/Home/IsFavorited?productId=${productId}`)
        .then(res => res.json())
        .then(data => {
            if (data.isFavorited) {
                btn.classList.add('favorite-active');
                btn.title = 'Xóa khỏi yêu thích';
                updateFavoriteBtnStyle(btn, true);
            } else {
                btn.classList.remove('favorite-active');
                btn.title = 'Thêm vào yêu thích';
                updateFavoriteBtnStyle(btn, false);
            }
        })
        .catch(() => {
            return;
        });
}

