    // Xử lý xóa sản phẩm khỏi giỏ hàng
    window.handleRemoveCartItem = function(btn) {
        const row = btn.closest('.cart-item-row');
        const cartId = row.dataset.cartId;
        if (!confirm('Bạn có chắc muốn xóa sản phẩm này khỏi giỏ hàng?')) return;
        fetch(`/Cart/RemoveCartItem?cartId=${cartId}`, { method: 'POST' })
            .then(res => res.json())
            .then(data => {
                if (data.status) {
                    toastr.success('Đã xóa sản phẩm khỏi giỏ hàng');
                    row.remove();
                } else {
                    toastr.error(data.message || 'Xóa thất bại');
                }
            })
            .catch(() => toastr.error('Lỗi kết nối server'));
    };
// cart.js
// Xử lý chọn checkbox từng item, từng ngày, chọn tất cả và tìm kiếm theo ngày hoặc tên sản phẩm

document.addEventListener('DOMContentLoaded', function () {
    // Chọn tất cả
    const checkAll = document.getElementById('cart-check-all');
    if (checkAll) {
        checkAll.addEventListener('change', function () {
            document.querySelectorAll('.cart-day-checkbox, .cart-item-checkbox').forEach(cb => {
                cb.checked = checkAll.checked;
            });
        });
    }

    // Chọn theo ngày
    document.querySelectorAll('.cart-day-checkbox').forEach(dayCb => {
        dayCb.addEventListener('change', function () {
            const day = this.dataset.day;
            document.querySelectorAll('.cart-item-checkbox[data-day="' + day + '"]').forEach(cb => {
                cb.checked = this.checked;
            });
            updateCheckAll();
        });
    });

    // Chọn từng item
    document.querySelectorAll('.cart-item-checkbox').forEach(itemCb => {
        itemCb.addEventListener('change', function () {
            const day = this.dataset.day;
            const items = document.querySelectorAll('.cart-item-checkbox[data-day="' + day + '"]');
            const dayCb = document.querySelector('.cart-day-checkbox[data-day="' + day + '"]');
            if (dayCb) {
                dayCb.checked = Array.from(items).every(cb => cb.checked);
            }
            updateCheckAll();
        });
    });

    // Tìm kiếm
    const searchInput = document.getElementById('cart-search-input');
    if (searchInput) {
        searchInput.addEventListener('input', function () {
            const value = this.value.trim().toLowerCase();
            document.querySelectorAll('.cart-group').forEach(group => {
                let groupMatch = false;
                group.querySelectorAll('.cart-item-row').forEach(row => {
                    const name = row.dataset.name.toLowerCase();
                    const date = row.dataset.date;
                    const match = name.includes(value) || date.includes(value);
                    row.style.display = match ? '' : 'none';
                    if (match) groupMatch = true;
                });
                group.style.display = groupMatch ? '' : 'none';
            });
        });
    }

    function updateCheckAll() {
        const all = document.querySelectorAll('.cart-item-checkbox');
        const checked = document.querySelectorAll('.cart-item-checkbox:checked');
        if (checkAll) checkAll.checked = all.length > 0 && all.length === checked.length;
    }

    // Xử lý tăng/giảm số lượng với debounce 2s
    const debounceTimers = {};

    window.handleMinusQuantity = function (btn) {
        const row = btn.closest('.cart-item-row');
        const qtySpan = row.querySelector('span');
        const cartId = row.dataset.cartId;
        let current = parseInt(qtySpan.textContent);
        if (current <= 1) {
            toastr.warning('Số lượng tối thiểu là 1');
            return;
        }
        updateQuantity(cartId, qtySpan, current - 1);
    };

    window.handlePlusQuantity = function (btn) {
        const row = btn.closest('.cart-item-row');
        const qtySpan = row.querySelector('span');
        const cartId = row.dataset.cartId;
        let current = parseInt(qtySpan.textContent);
        updateQuantity(cartId, qtySpan, current + 1);
    };

    function updateQuantity(cartId, qtySpan, newQty) {
        if (newQty < 1) {
            toastr.warning('Số lượng tối thiểu là 1');
            return;
        }
        qtySpan.textContent = newQty;
        if (debounceTimers[cartId]) clearTimeout(debounceTimers[cartId]);
        debounceTimers[cartId] = setTimeout(() => {
            fetch(`/Cart/updateQuantity?cartId=${cartId}&quantity=${newQty}`)
                .then(res => res.json())
                .then(data => {
                    if (!data.status) {
                        toastr.error(data.message || 'Cập nhật thất bại');
                    } else {
                        toastr.success('Cập nhật số lượng thành công');
                    }
                })
                .catch(() => toastr.error('Lỗi kết nối server'));
        }, 2000);
    }
    // Gán lại onclick cho các nút
    document.querySelectorAll('.cart-item-row').forEach(row => {
        const minusBtn = row.querySelector('button:nth-child(1)');
        const plusBtn = row.querySelector('button:nth-child(3)');
        minusBtn.setAttribute('onclick', 'handleMinusQuantity(this)');
        plusBtn.setAttribute('onclick', 'handlePlusQuantity(this)');
    });
});
function addCart(id) {
    alert("Thêm sản phẩm có ID: " + id);

    fetch('/Cart/AddCart?id=' + id, {
        method: 'POST'
    })
        .then(res => res.json())
        .then(data => {
            alert("Đã thêm vào giỏ hàng");
        })
        .catch(err => {
            console.log(err);
        });
}