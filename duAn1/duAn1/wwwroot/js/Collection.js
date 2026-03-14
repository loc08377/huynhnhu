function filterCategory(id, el) {

    document.querySelectorAll(".category-btn").forEach(btn => {
        btn.classList.remove("bg-blue-600");
        btn.classList.remove("text-white");
        btn.classList.add("bg-white");
        btn.classList.add("text-slate-600");
    });

    el.classList.remove("bg-white");
    el.classList.remove("text-slate-600");
    el.classList.add("bg-blue-600");
    el.classList.add("text-white");


    let url = '/Home/loadProductList';

    if (id != null) {
        url += '?categoryId=' + id;
    }

    customFetch(url)
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