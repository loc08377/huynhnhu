// Interceptor cho tất cả AJAX requests
(function() {
    // Lưu giữ original fetch function
    const originalFetch = window.fetch;

    // Override fetch function
    window.fetch = function(...args) {
        return originalFetch.apply(this, args)
            .then(response => {
                // Check nếu response là 401 (Unauthorized) hoặc 403 (Forbidden)
                if (response.status === 401 || response.status === 403) {
                    return response.json().then(data => {
                        if (data.redirect) {
                            // Redirect tới URL được chỉ định
                            window.location.href = data.redirect;
                        }
                        return response;
                    });
                }
                return response;
            });
    };

    // Cũng cần handle jQuery AJAX nếu có sử dụng
    if (typeof jQuery !== 'undefined') {
        jQuery(document).ajaxComplete(function(event, xhr, settings) {
            if (xhr.status === 401 || xhr.status === 403) {
                try {
                    const data = JSON.parse(xhr.responseText);
                    if (data.redirect) {
                        window.location.href = data.redirect;
                    }
                } catch (e) {
                    // Nếu response không phải JSON, redirect về trang login
                    window.location.href = '/Login/Index?error=expired';
                }
            }
        });
    }
})();
