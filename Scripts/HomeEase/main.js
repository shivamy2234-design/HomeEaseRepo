
document.addEventListener("DOMContentLoaded", function () {
    const navbar = document.querySelector(".home-navbar");
    const year = document.getElementById("currentYear");
    const toastElement = document.getElementById("homeEaseToast");
    const toastMessage = document.getElementById("toastMessage");

    const toast = toastElement && window.bootstrap
        ? window.bootstrap.Toast.getOrCreateInstance(toastElement, { delay: 3200 })
        : null;

    if (year) {
        year.textContent = new Date().getFullYear();
    }

    // Navbar scroll effect
    function updateNavbar() {
        if (navbar) {
            navbar.classList.toggle("scrolled", window.scrollY > 12);
        }
    }

    updateNavbar();
    window.addEventListener("scroll", updateNavbar, { passive: true });

    // Toast message
    function showToast(message) {
        if (!toast || !toastMessage) {
            window.alert(message);
            return;
        }

        toastMessage.textContent = message;
        toast.show();
    }

    // Navigate service cards to the existing MVC Services page.
    // This also handles cards added dynamically by Show More Services.
    document.addEventListener("click", function (event) {
        const target = event.target;

        if (!(target instanceof Element)) {
            return;
        }

        const link = target.closest("[data-service]");

        if (!link) {
            return;
        }

        const serviceName = link.getAttribute("data-service");

        if (!serviceName) {
            return;
        }

        const href = link.getAttribute("href");

        // Keep a working MVC link unchanged.
        if (href && href !== "#") {
            return;
        }

        event.preventDefault();

        // Safe fallback until the exact service-details MVC route is confirmed.
        window.location.href = "/Services";
    });

    // Demo-only feature links
    document.querySelectorAll("[data-demo]").forEach(function (link) {
        link.addEventListener("click", function (event) {
            event.preventDefault();

            const feature = link.getAttribute("data-demo");

            if (!feature) {
                return;
            }

            const message =
                feature.charAt(0).toUpperCase() +
                feature.slice(1) +
                " is currently a demo feature.";

            showToast(message);
        });
    });

    // Newsletter demo
    const newsletterForm = document.getElementById("newsletterForm");
    const newsletterEmail = document.getElementById("newsletterEmail");
    const newsletterMessage = document.getElementById("newsletterMessage");

    if (newsletterForm) {
        newsletterForm.addEventListener("submit", function (event) {
            event.preventDefault();

            if (!newsletterForm.checkValidity()) {
                newsletterForm.reportValidity();
                return;
            }

            if (newsletterMessage) {
                newsletterMessage.textContent =
                    "Thanks! Newsletter signup is a frontend demo for now.";
            }

            if (newsletterEmail) {
                newsletterEmail.value = "";
            }
        });
    }

    // Show More Services
    const showMoreBtn = document.getElementById("showMoreBtn");
    const serviceGrid = document.getElementById("serviceGrid");

    if (showMoreBtn && serviceGrid) {
        let expanded = false;

        const extraServices = [
            {
                name: "Pest Control",
                desc: "Thoughtful treatments to help protect your home.",
                price: "₹499",
                icon: "bi-bug",
                tag: "Home health",
                image: "https://images.unsplash.com/photo-1632923057155-6d5b0b0e4e2c?auto=format&fit=crop&w=800&q=80"
            },
            {
                name: "Painting & Repairs",
                desc: "Refresh your walls and fix the little things.",
                price: "₹999",
                icon: "bi-brush",
                tag: "Home refresh",
                image: "https://images.unsplash.com/photo-1562259949-e8e7689d7828?auto=format&fit=crop&w=800&q=80"
            },
            {
                name: "Gardening",
                desc: "Give your plants a little extra care.",
                price: "₹399",
                icon: "bi-flower1",
                tag: "Green living",
                image: "https://images.unsplash.com/photo-1416879595882-3373a0480b5b?auto=format&fit=crop&w=800&q=80"
            }
        ];

        showMoreBtn.addEventListener("click", function () {
            if (!expanded) {
                extraServices.forEach(function (service) {
                    const col = document.createElement("div");
                    col.className = "col-sm-6 col-lg-4";

                    const article = document.createElement("article");
                    article.className = "service-card";

                    const imageContainer = document.createElement("div");
                    imageContainer.className = "service-image";

                    const image = document.createElement("img");
                    image.src = service.image;
                    image.alt = service.name;
                    image.loading = "lazy";

                    const tag = document.createElement("span");
                    tag.className = "service-tag";
                    tag.textContent = service.tag;

                    imageContainer.appendChild(image);
                    imageContainer.appendChild(tag);

                    const content = document.createElement("div");
                    content.className = "service-content";

                    const iconContainer = document.createElement("div");
                    iconContainer.className = "service-icon";

                    const icon = document.createElement("i");
                    icon.className = "bi " + service.icon;
                    iconContainer.appendChild(icon);

                    const heading = document.createElement("h3");
                    heading.textContent = service.name;

                    const description = document.createElement("p");
                    description.textContent = service.desc;

                    const bottom = document.createElement("div");
                    bottom.className = "service-bottom";

                    const price = document.createElement("span");
                    price.textContent = "Starting at ";

                    const priceStrong = document.createElement("strong");
                    priceStrong.textContent = service.price;
                    price.appendChild(priceStrong);

                    const link = document.createElement("a");
                    link.href = "#";
                    link.className = "service-link";
                    link.setAttribute("data-service", service.name);
                    link.setAttribute("aria-label", "View " + service.name);

                    const arrow = document.createElement("i");
                    arrow.className = "bi bi-arrow-up-right";
                    link.appendChild(arrow);

                    bottom.appendChild(price);
                    bottom.appendChild(link);

                    content.appendChild(iconContainer);
                    content.appendChild(heading);
                    content.appendChild(description);
                    content.appendChild(bottom);

                    article.appendChild(imageContainer);
                    article.appendChild(content);
                    col.appendChild(article);

                    serviceGrid.appendChild(col);
                });

                showMoreBtn.innerHTML =
                    'Show fewer services <i class="bi bi-arrow-up ms-2"></i>';

                expanded = true;
            } else {
                while (serviceGrid.children.length > 6) {
                    serviceGrid.removeChild(serviceGrid.lastElementChild);
                }

                showMoreBtn.innerHTML =
                    'Explore all services <i class="bi bi-arrow-right ms-2"></i>';

                expanded = false;
            }
        });
    }
});
