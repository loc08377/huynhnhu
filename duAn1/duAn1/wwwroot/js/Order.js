window.showCheckoutModal = function() {
                const checkedItems = document.querySelectorAll('.cart-item-checkbox:checked');
                
                if (checkedItems.length === 0) {
                    toastr.warning('Vui lòng chọn sản phẩm trước khi thanh toán');
                    return;
                }

                // Update modal summary
                let totalQty = 0;
                let totalPrice = 0;

                checkedItems.forEach(checkbox => {
                    const row = checkbox.closest('.cart-item-row');
                    const price = parseInt(row.dataset.price) || 0;
                    const qtySpan = row.querySelector('span');
                    const quantity = parseInt(qtySpan?.textContent) || 0;
                    
                    totalQty += quantity;
                    totalPrice += price * quantity;
                });

                document.getElementById('modal-qty').textContent = totalQty;
                document.getElementById('modal-total').textContent = formatVND(totalPrice);
                document.getElementById('modal-grand-total').textContent = formatVND(totalPrice);

                // Show modal
                document.getElementById('checkoutModal').classList.remove('hidden');
                document.body.style.overflow = 'hidden';
            };

            window.closeCheckoutModal = function() {
                document.getElementById('checkoutModal').classList.add('hidden');
                document.getElementById('deliveryAddress').value = '';
                document.body.style.overflow = '';
            };

            window.submitCheckout = function() {
                const address = document.getElementById('deliveryAddress').value.trim();
                
                if (!address) {
                    toastr.error('Vui lòng nhập địa chỉ giao hàng');
                    return;
                }

                // Lấy danh sách sản phẩm đã chọn
                const checkedItems = document.querySelectorAll('.cart-item-checkbox:checked');
                const cartIds = Array.from(checkedItems).map(cb => {
                    return parseInt(cb.closest('.cart-item-row').dataset.cartId);
                });

                if (cartIds.length === 0) {
                    toastr.error('Vui lòng chọn sản phẩm');
                    return;
                }

                // Gửi request tạo đơn hàng
                customFetch('/Cart/CreateOrder', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({
                        address: address,
                        cartIds: cartIds
                    })
                })
                    .then(res => res.json())
                    .then(data => {
                        if (data.success) {
                            toastr.success('Đặt hàng thành công!');
                            closeCheckoutModal();
                            setTimeout(() => {
                                window.location.href = '/Order/Index';
                            }, 1500);
                        } else {
                            toastr.error(data.message || 'Đặt hàng thất bại');
                        }
                    })
                    .catch(err => {
                        console.error(err);
                        toastr.error('Có lỗi xảy ra');
                    });
            };

            // Close modal when pressing Escape
            document.addEventListener('keydown', (e) => {
                if (e.key === 'Escape' && !document.getElementById('checkoutModal').classList.contains('hidden')) {
                    closeCheckoutModal();
                }
            });