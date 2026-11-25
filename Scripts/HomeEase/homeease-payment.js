(function () {
    var container = document.getElementById("razorpayCheckout");
    var button = document.getElementById("openRazorpay");
    var form = document.getElementById("verifyPaymentForm");


    if (!container || !button || !form) {
        return;
    }

    button.addEventListener("click", function (event) {
        event.preventDefault();
        event.stopImmediatePropagation();
        if (typeof Razorpay === "undefined") {
            alert("Razorpay Checkout load nahi hua. Page refresh karke try karein.");
            return;
        }

        var key = container.getAttribute("data-key");
        var amount = parseInt(container.getAttribute("data-amount"), 10);
        var service = container.getAttribute("data-service");
        var orderId = container.getAttribute("data-order");

        if (!key || !amount || !orderId) {
            alert("Payment details missing hain. Please try again.");
            return;
        }

        var options = {
            key: key,
            amount: amount,
            currency: "INR",
            name: "HomeEase",
            description: service || "Home service booking",
            order_id: orderId,

            handler: function (response) {
                document.getElementById("razorpayPaymentId").value =
                    response.razorpay_payment_id;

                document.getElementById("razorpayOrderId").value =
                    response.razorpay_order_id;

                document.getElementById("razorpaySignature").value =
                    response.razorpay_signature;

                HTMLFormElement.prototype.submit.call(form);
            },

            modal: {
                ondismiss: function () {
                    // Checkout close hone par payment ko Paid mark nahi karna.
                }
            },

            theme: {
                color: "#2563eb"
            }
        };
        var checkout = new Razorpay(options);

        
        checkout.open();
    });
})();