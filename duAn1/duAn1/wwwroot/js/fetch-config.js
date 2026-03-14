/**
 * Custom fetch wrapper với auto headers và xử lý authentication
 * @param {string} url - URL cần fetch
 * @param {object} options - Fetch options (method, body, etc)
 * @returns {Promise} Response promise
 */
function customFetch(url, options = {}) {
    // Merge headers
    const headers = {
        'X-Requested-With': 'XMLHttpRequest',
        ...options.headers
    };

    // Merge options
    const finalOptions = {
        ...options,
        headers
    };

    return fetch(url, finalOptions)
        .then(response => {
            // Xử lý 401/403 - unauthorized
            if (response.status === 401 || response.status === 403) {
                return response.json().then(data => {
                    if (data.redirect) {
                        window.location.href = data.redirect;
                    }
                    return response;
                });
            }
            return response;
        });
}
