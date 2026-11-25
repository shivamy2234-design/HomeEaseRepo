
document.addEventListener("DOMContentLoaded", function () {
    const services = {
        "Home Cleaning": {
            price: 799,
            duration: "2–3 hours",
            intro: "A fresh, spotless space without the stress.",
            description: "A practical home cleaning service for everyday upkeep. Choose the scope and schedule that work for your home.",
            image: "https://images.unsplash.com/photo-1581578731548-c64695cc6952?auto=format&fit=crop&w=1200&q=85",
            included: [
                "Dusting and wiping reachable surfaces",
                "Sweeping and mopping accessible floors",
                "Cleaning common areas and bedrooms",
                "Basic tidy-up of agreed areas"
            ],
            before: "Please keep personal valuables safe and tell the professional about any areas that need special attention."
        },
        "Kitchen & Bathroom": {
            price: 599,
            duration: "1–2 hours",
            intro: "Detailed cleaning for the hardest-working rooms.",
            description: "Focused cleaning for kitchen and bathroom surfaces, helping make everyday spaces feel fresher.",
            image: "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=1200&q=85",
            included: [
                "Countertop and sink cleaning",
                "Accessible cabinet exterior wipe-down",
                "Bathroom fixture and surface cleaning",
                "Floor cleaning in selected rooms"
            ],
            before: "Please identify any delicate surfaces and confirm which rooms are included before the service starts."
        },
        "Electrical Services": {
            price: 299,
            duration: "30–90 minutes",
            intro: "Small repairs and installations, handled safely.",
            description: "Get help with common household electrical jobs from a professional, subject to inspection and safety requirements.",
            image: "https://images.unsplash.com/photo-1621905251918-48416bd8575a?auto=format&fit=crop&w=1200&q=85",
            included: [
                "Initial issue assessment",
                "Basic switch or socket troubleshooting",
                "Small installation work where suitable",
                "Service completion summary"
            ],
            before: "Do not touch exposed wires or attempt unsafe repairs. Final scope and parts cost must be confirmed after inspection."
        },
        "Plumbing": {
            price: 349,
            duration: "30–90 minutes",
            intro: "Leaks, fittings and everyday plumbing fixes.",
            description: "Support for common plumbing concerns, from minor leaks to fittings and fixture troubleshooting.",
            image: "https://images.unsplash.com/photo-1607472586893-edb57bdc0e39?auto=format&fit=crop&w=1200&q=85",
            included: [
                "Initial problem assessment",
                "Basic leak and fitting inspection",
                "Minor repair where possible",
                "Recommendations for larger repairs"
            ],
            before: "Keep the work area accessible. Replacement parts and major repairs may have additional charges after approval."
        },
        "Laundry & Ironing": {
            price: 199,
            duration: "1–2 hours",
            intro: "Fresh clothes, neatly folded and ready to go.",
            description: "Convenient help with everyday laundry and ironing needs based on the selected order and garment count.",
            image: "https://images.unsplash.com/photo-1527515637462-cff94eecc1ac?auto=format&fit=crop&w=1200&q=85",
            included: [
                "Sorting of agreed garments",
                "Laundry or ironing as selected",
                "Neat folding and handover",
                "Garment count confirmation"
            ],
            before: "Separate delicate garments and communicate fabric-care instructions before the service begins."
        },
        "AC & Appliance Care": {
            price: 499,
            duration: "45–120 minutes",
            intro: "Keep everyday appliances working smoothly.",
            description: "Arrange an initial check for common AC and home appliance concerns. Repairs depend on inspection and parts availability.",
            image: "https://images.unsplash.com/photo-1581092160562-40aa08e78837?auto=format&fit=crop&w=1200&q=85",
            included: [
                "Initial appliance inspection",
                "Basic troubleshooting",
                "Cleaning or maintenance where selected",
                "Repair recommendations"
            ],
            before: "Share the appliance type and issue. Spare parts, refrigerant and major repairs may be priced separately."
        },
        "Pest Control": {
            price: 499,
            duration: "1–2 hours",
            intro: "Thoughtful treatments to help protect your home.",
            description: "A home pest-control visit based on the pest type, affected area and treatment needed.",
            image: "https://images.unsplash.com/photo-1632923057155-6d5b0b0e4e2c?auto=format&fit=crop&w=1200&q=85",
            included: [
                "Issue and affected-area assessment",
                "Treatment plan explanation",
                "Treatment for the agreed area",
                "Aftercare and safety instructions"
            ],
            before: "Follow all professional safety instructions, including any required time away from treated areas. Treatment availability depends on pest type."
        },
        "Painting & Repairs": {
            price: 999,
            duration: "2–4 hours",
            intro: "Refresh your walls and fix the little things.",
            description: "Book help for small home maintenance tasks or discuss a painting requirement before work begins.",
            image: "https://images.unsplash.com/photo-1562259949-e8e7689d7828?auto=format&fit=crop&w=1200&q=85",
            included: [
                "Requirement review",
                "Basic surface or repair assessment",
                "Small repair work where agreed",
                "Estimate for larger work"
            ],
            before: "The displayed price is a starting estimate. Paint, materials, area size and major repair work can affect the final quote."
        },
        "Gardening": {
            price: 399,
            duration: "1–2 hours",
            intro: "Give your plants a little extra care.",
            description: "Home gardening assistance for routine plant care and small garden maintenance.",
            image: "https://images.unsplash.com/photo-1416879595882-3373a0480b5b?auto=format&fit=crop&w=1200&q=85",
            included: [
                "Plant and garden-area assessment",
                "Basic watering and care",
                "Light pruning where suitable",
                "General tidy-up of the agreed area"
            ],
            before: "Please point out delicate plants and mention any preferred care routine or products."
        },
        "Carpentry": {
            price: 399,
            duration: "45–120 minutes",
            intro: "Furniture fixes and fittings for everyday living.",
            description: "Book a visit for common carpentry needs such as small adjustments, fittings and minor furniture repairs.",
            image: "https://images.unsplash.com/photo-150414845532c376907d081c?auto=format&fit=crop&w=1200&q=85",
            included: [
                "Initial task assessment",
                "Minor furniture or fitting repair",
                "Basic adjustment where possible",
                "Recommendations for complex work"
            ],
            before: "Share photos or details of the task when requested. Hardware and materials may cost extra."
        },
        "Cook & Maid Services": {
            price: 499,
            duration: "As agreed",
            intro: "Find household support for everyday routines.",
            description: "Request household assistance based on your needs, preferred schedule and staff availability.",
            image: "https://images.unsplash.com/photo-1556911220-e15b29be8c8f?auto=format&fit=crop&w=1200&q=85",
            included: [
                "Requirement and schedule discussion",
                "Service preference selection",
                "Availability confirmation",
                "Household task scope agreement"
            ],
            before: "Staff availability, verification and ongoing arrangements must be confirmed by the service administrator."
        },
        "Gardening & Outdoor Care": {
            price: 399,
            duration: "1–2 hours",
            intro: "Keep your outdoor spaces tidy and thriving.",
            description: "Routine assistance for small gardens, balconies and outdoor areas.",
            image: "https://images.unsplash.com/photo-1416879595882-3373a0480b5b?auto=format&fit=crop&w=1200&q=85",
            included: [
                "Basic garden tidy-up",
                "Plant care for agreed areas",
                "Light pruning where suitable",
                "Care recommendations"
            ],
            before: "Mention plant-care preferences and identify any equipment or products that should be used."
        }
    };

    // Do nothing if this is not the service-details page.
    const titleElement = document.getElementById("serviceTitle");

    if (!titleElement) {
        return;
    }

    const params = new URLSearchParams(window.location.search);
    const requested = params.get("service") || "Home Cleaning";
    const name = services[requested] ? requested : "Home Cleaning";
    const service = services[name];

    document.title = name + " | HomeEase";

    function setText(id, value) {
        const element = document.getElementById(id);

        if (element) {
            element.textContent = value;
        }
    }

    setText("serviceTitle", name);
    setText("serviceIntro", service.intro);
    setText("serviceDescription", service.description);
    setText("servicePrice", "₹" + service.price);
    setText("serviceDuration", service.duration);
    setText("beforeService", service.before);
    setText("currentYear", String(new Date().getFullYear()));

    const serviceImage = document.getElementById("serviceImage");

    if (serviceImage) {
        serviceImage.src = service.image;
        serviceImage.alt = name;
    }

    const includedList = document.getElementById("includedList");

    if (includedList) {
        includedList.replaceChildren();

        service.included.forEach(function (item) {
            const li = document.createElement("li");
            const icon = document.createElement("i");
            const span = document.createElement("span");

            icon.className = "bi bi-check-circle-fill";
            span.textContent = item;

            li.appendChild(icon);
            li.appendChild(span);
            includedList.appendChild(li);
        });
    }

    // Booking URL must be supplied by the existing MVC view.
    // Avoid linking to a nonexistent booking.html file.
    const bookNow = document.getElementById("bookNow");

    if (bookNow && (!bookNow.getAttribute("href") ||
        bookNow.getAttribute("href") === "#")) {
        bookNow.addEventListener("click", function (event) {
            event.preventDefault();
            window.location.href = "/Services";
        });
    }

    const relatedServices = document.getElementById("relatedServices");

    if (relatedServices) {
        relatedServices.replaceChildren();

        Object.entries(services)
            .filter(function (entry) {
                return entry[0] !== name;
            })
            .slice(0, 3)
            .forEach(function (entry) {
                const serviceName = entry[0];
                const item = entry[1];

                const column = document.createElement("div");
                column.className = "col-md-4";

                const link = document.createElement("a");
                link.className = "related-card";
                link.href = "/Services";

                const image = document.createElement("img");
                image.src = item.image;
                image.alt = serviceName;
                image.loading = "lazy";

                const content = document.createElement("div");
                const strong = document.createElement("strong");
                const price = document.createElement("span");
                const arrow = document.createElement("i");

                strong.textContent = serviceName;
                price.textContent = "From ₹" + item.price;
                arrow.className = "bi bi-arrow-up-right";

                content.appendChild(strong);
                content.appendChild(price);

                link.appendChild(image);
                link.appendChild(content);
                link.appendChild(arrow);

                column.appendChild(link);
                relatedServices.appendChild(column);
            });
    }
});
