    // Xử lý xóa sản phẩm khỏi giỏ hàng
    window.handleRemoveCartItem = function(btn) {
        const row = btn.closest('.cart-item-row');
        const cartId = row.dataset.cartId;
        if (!confirm('Bạn có chắc muốn xóa sản phẩm này khỏi giỏ hàng?')) return;
        customFetch(`/Cart/RemoveCartItem?cartId=${cartId}`, { 
            method: 'POST'
        })
            .then(res => res.json())
            .then(data => {
                if (data.status) {
                    toastr.success('Đã xóa sản phẩm khỏi giỏ hàng');
                    row.remove();
                    updateOrderSummary();
                } else {
                    toastr.error(data.message || 'Xóa thất bại');
                }
            })
    };

// Hàm cập nhật tổng đơn hàng
function updateOrderSummary() {
    let totalQty = 0;
    let totalPrice = 0;
    const selectedItems = [];

    // Lấy tất cả item đã tick
    document.querySelectorAll('.cart-item-checkbox:checked').forEach(checkbox => {
        const row = checkbox.closest('.cart-item-row');
        if (row) {
            const price = parseInt(row.dataset.price) || 0;
            const name = row.dataset.name || 'Không rõ tên';
            const qtySpan = row.querySelector('span');
            const quantity = parseInt(qtySpan?.textContent) || 0;
            const itemTotal = price * quantity;
            
            totalQty += quantity;
            totalPrice += itemTotal;

            // Thêm vào danh sách
            selectedItems.push({
                name: name,
                quantity: quantity,
                price: price,
                total: itemTotal
            });
        }
    });

    // Cập nhật danh sách sản phẩm
    const itemsContainer = document.getElementById('cart-selected-items');
    if (itemsContainer) {
        if (selectedItems.length === 0) {
            itemsContainer.innerHTML = '<p class="text-sm text-slate-500 italic">Chọn sản phẩm để xem chi tiết</p>';
        } else {
            itemsContainer.innerHTML = selectedItems.map(item => `
                <div class="flex justify-between items-start text-sm bg-slate-50 p-2 rounded-lg">
                    <div class="flex-grow">
                        <p class="font-semibold text-slate-700 line-clamp-1">${item.name}</p>
                        <p class="text-xs text-slate-500">x${item.quantity} × ${formatVND(item.price)}</p>
                    </div>
                    <p class="font-semibold text-blue-600 whitespace-nowrap ml-2">${formatVND(item.total)}</p>
                </div>
            `).join('');
        }
    }

    // Cập nhật giao diện
    const qtyElement = document.getElementById('cart-total-qty');
    const subtotalElement = document.getElementById('cart-subtotal');
    const totalElement = document.getElementById('cart-total');

    if (qtyElement) {
        qtyElement.textContent = totalQty;
    }

    if (subtotalElement) {
        subtotalElement.textContent = formatVND(totalPrice);
    }

    if (totalElement) {
        totalElement.textContent = formatVND(totalPrice);
    }
}

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
            updateOrderSummary();
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
            updateOrderSummary();
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
            updateOrderSummary();
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
        updateOrderSummary();
        if (debounceTimers[cartId]) clearTimeout(debounceTimers[cartId]);
        debounceTimers[cartId] = setTimeout(() => {
            customFetch(`/Cart/updateQuantity?cartId=${cartId}&quantity=${newQty}`)
                .then(res => res.json())
                .then(data => {
                    if (!data.status) {
                        toastr.error(data.message || 'Cập nhật thất bại');
                    } else {
                        toastr.success('Cập nhật số lượng thành công');
                    }
                })
        }, 2000);
    }
    // Gán lại onclick cho các nút
    document.querySelectorAll('.cart-item-row').forEach(row => {
        const minusBtn = row.querySelector('button:nth-child(1)');
        const plusBtn = row.querySelector('button:nth-child(3)');
        minusBtn.setAttribute('onclick', 'handleMinusQuantity(this)');
        plusBtn.setAttribute('onclick', 'handlePlusQuantity(this)');
    });

    // Cập nhật tổng ban đầu
    updateOrderSummary();
});
