function filterCategory(id, el) {

    document.querySelectorAll(".category-btn").forEach(btn => {
        btn.classList.remove("bg-blue-600", "text-white");
        btn.classList.add("bg-white", "text-slate-600");
    });

    el.classList.remove("bg-white", "text-slate-600");
    el.classList.add("bg-blue-600", "text-white");


    let url = '/Home/loadProductList';

    if (id != null) {
        url += '?categoryId=' + id;
    }

    fetch(url)
        .then(res => res.text())
        .then(html => {
            document.getElementById("product-container").innerHTML = html;
        });

    // update URL
    let newUrl = '/Home/Collection';

    if (id != null) {
        newUrl += '?categoryId=' + id;
    }

    history.pushState(null, "", newUrl);
}