
using HomeEase.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Text;
using System.Security.Cryptography;
using System.Web.Mvc;

namespace HomeEase.Controllers
{
    public class PaymentsController : Controller
    {
        private string ConnectionString
        {
            get
            {
                return ConfigurationManager
                    .ConnectionStrings["HomeEaseConnection"]
                    .ConnectionString;
            }
        }

        private string RazorpayKeyId
        {
            get
            {
                return ConfigurationManager
                    .AppSettings["RazorpayKeyId"];
            }
        }

        private string RazorpayKeySecret
        {
            get
            {
                return ConfigurationManager
                    .AppSettings["RazorpayKeySecret"];
            }
        }

        private bool IsCustomerLoggedIn()
        {
            return Session["UserId"] != null
                && Session["Role"] != null
                && Session["Role"].ToString() == "Customer";
        }

        private PaymentViewModel GetBookingForPayment(int bookingId,int userId)
        {
            PaymentViewModel model = null;

            string query = @"
                SELECT
                    b.BookingId,
                    s.ServiceName,
                    b.BookingDate,
                    b.TimeSlot,
                    b.TotalAmount
                FROM dbo.Bookings b
                INNER JOIN dbo.Services s
                    ON b.ServiceId = s.ServiceId
                WHERE b.BookingId = @BookingId
                  AND b.UserId = @UserId";

            using (SqlConnection con =
                new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@BookingId",
                    SqlDbType.Int).Value = bookingId;

                cmd.Parameters.Add("@UserId",
                    SqlDbType.Int).Value = userId;

                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        model = new PaymentViewModel
                        {
                            BookingId = Convert.ToInt32(
                                reader["BookingId"]),

                            ServiceName = reader["ServiceName"].ToString(),

                            BookingDate = Convert.ToDateTime(
                                reader["BookingDate"]),

                            TimeSlot = reader["TimeSlot"].ToString(),

                            Amount = Convert.ToDecimal(
                                reader["TotalAmount"]),

                            RazorpayKeyId = RazorpayKeyId
                        };
                    }

                }
            }

            return model;
        }


        public ActionResult Pay(int id)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            PaymentViewModel model = GetBookingForPayment(id, userId);

            if (model == null)
            {
                return HttpNotFound();
            }

            model.RazorpayKeyId = RazorpayKeyId;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult StartPayment(int bookingId)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            PaymentViewModel model =
                GetBookingForPayment(bookingId, userId);

            if (model == null)
            {
                return HttpNotFound();
            }

            try
            {
                if (string.IsNullOrWhiteSpace(RazorpayKeyId)
                    || string.IsNullOrWhiteSpace(RazorpayKeySecret))
                {
                    throw new Exception("Razorpay configuration is missing.");
                }

                if (model.Amount <= 0)
                {
                    throw new Exception("Invalid booking amount.");
                }

                long amountInPaise = Convert.ToInt64(
                    model.Amount * 100m);

                string receipt = "HE" + bookingId + "_" +
                    Guid.NewGuid().ToString("N").Substring(0, 12);

                JObject requestData = new JObject
                {
                    ["amount"] = amountInPaise,
                    ["currency"] = "INR",
                    ["receipt"] = receipt
                };

                using (WebClient client = new WebClient())
                {
                    string credentials = Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(
                            RazorpayKeyId + ":" + RazorpayKeySecret));

                    client.Headers[HttpRequestHeader.Authorization] =
                        "Basic " + credentials;

                    client.Headers[HttpRequestHeader.ContentType] =
                        "application/json";

                    string response = client.UploadString(
                        "https://api.razorpay.com/v1/orders",
                        "POST",
                        requestData.ToString());

                    JObject order = JObject.Parse(response);

                    string orderId = (string)order["id"];

                    if (string.IsNullOrWhiteSpace(orderId))
                    {
                        throw new Exception("Razorpay order was not created.");
                    }

                    string insertQuery = @"
                INSERT INTO dbo.Payments
                (
                    BookingId,
                    Amount,
                    PaymentMethod,
                    PaymentStatus,
                    TransactionReference,
                    PaidAt,
                    CreatedAt,
                    RazorpayOrderId
                )
                VALUES
                (
                    @BookingId,
                    @Amount,
                    @PaymentMethod,
                    @PaymentStatus,
                    NULL,
                    NULL,
                    GETDATE(),
                    @RazorpayOrderId
                )";

                    using (SqlConnection con =
                        new SqlConnection(ConnectionString))
                    using (SqlCommand cmd =
                        new SqlCommand(insertQuery, con))
                    {
                        cmd.Parameters.Add("@BookingId",
                            SqlDbType.Int).Value = bookingId;

                        cmd.Parameters.Add("@Amount",
                            SqlDbType.Decimal).Value = model.Amount;

                        cmd.Parameters["@Amount"].Precision = 10;
                        cmd.Parameters["@Amount"].Scale = 2;

                        cmd.Parameters.Add("@PaymentMethod",
                            SqlDbType.NVarChar, 50).Value = "Razorpay";

                        cmd.Parameters.Add("@PaymentStatus",
                            SqlDbType.NVarChar, 30).Value = "Pending";

                        cmd.Parameters.Add("@RazorpayOrderId",
                            SqlDbType.NVarChar, 100).Value = orderId;

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }

                    model.RazorpayOrderId = orderId;
                    model.RazorpayKeyId = RazorpayKeyId;
                    model.PaymentStatus = "Pending";
                }

                return View("Pay", model);
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    "HomeEase StartPayment error: " + ex);

                TempData["PaymentError"] =
                    "Unable to start payment. Please try again.";

                return RedirectToAction("Pay", new { id = bookingId });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyPayment(
            int bookingId,
            string razorpayOrderId,
            string razorpayPaymentId,
            string razorpaySignature)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            if (string.IsNullOrWhiteSpace(razorpayOrderId)
                || string.IsNullOrWhiteSpace(razorpayPaymentId)
                || string.IsNullOrWhiteSpace(razorpaySignature))
            {
                TempData["PaymentError"] = "Payment details are incomplete.";
                return RedirectToAction("Pay", new { id = bookingId });
            }

            try
            {
                // 1. Confirm that this booking belongs to the logged-in customer.
                PaymentViewModel booking =
                    GetBookingForPayment(bookingId, userId);

                if (booking == null)
                {
                    return HttpNotFound();
                }

                // 2. Find the matching pending payment in our database.
                decimal storedAmount;
                string paymentQuery = @"
            SELECT TOP 1 Amount
            FROM dbo.Payments
            WHERE BookingId = @BookingId
              AND RazorpayOrderId = @OrderId
              AND PaymentStatus = 'Pending'";

                using (SqlConnection con = new SqlConnection(ConnectionString))
                using (SqlCommand cmd = new SqlCommand(paymentQuery, con))
                {
                    cmd.Parameters.Add("@BookingId", SqlDbType.Int).Value =
                        bookingId;

                    cmd.Parameters.Add("@OrderId", SqlDbType.NVarChar, 100).Value =
                        razorpayOrderId;

                    con.Open();

                    object result = cmd.ExecuteScalar();

                    if (result == null || result == DBNull.Value)
                    {
                        TempData["PaymentError"] =
                            "Pending payment record not found. Please check your bookings.";

                        return RedirectToAction("Pay", new { id = bookingId });
                    }

                    storedAmount = Convert.ToDecimal(result);
                }

                // 3. Verify the Razorpay signature using our server-side secret.
                string signaturePayload =
                    razorpayOrderId + "|" + razorpayPaymentId;

                string expectedSignature;

                using (HMACSHA256 hmac = new HMACSHA256(
                    Encoding.UTF8.GetBytes(RazorpayKeySecret)))
                {
                    byte[] hash = hmac.ComputeHash(
                        Encoding.UTF8.GetBytes(signaturePayload));

                    expectedSignature = BitConverter.ToString(hash)
                        .Replace("-", "")
                        .ToLowerInvariant();
                }

                if (!string.Equals(
                    expectedSignature,
                    razorpaySignature,
                    StringComparison.OrdinalIgnoreCase))
                {
                    TempData["PaymentError"] =
                        "Payment signature verification failed.";

                    return RedirectToAction("Pay", new { id = bookingId });
                }

                // 4. Ask Razorpay for the actual payment details.
                JObject payment;

                using (WebClient client = new WebClient())
                {
                    string credentials = Convert.ToBase64String(
                        Encoding.UTF8.GetBytes(
                            RazorpayKeyId + ":" + RazorpayKeySecret));

                    client.Headers[HttpRequestHeader.Authorization] =
                        "Basic " + credentials;

                    string response = client.DownloadString(
                        "https://api.razorpay.com/v1/payments/"
                        + Uri.EscapeDataString(razorpayPaymentId));

                    payment = JObject.Parse(response);
                }

                string actualPaymentId = (string)payment["id"];
                string actualOrderId = (string)payment["order_id"];
                string actualStatus = (string)payment["status"];

                long actualAmount = Convert.ToInt64(payment["amount"]);
                long expectedAmount = Convert.ToInt64(storedAmount * 100m);

                if (actualPaymentId != razorpayPaymentId
                    || actualOrderId != razorpayOrderId
                    || actualStatus != "captured"
                    || actualAmount != expectedAmount
                    || (string)payment["currency"] != "INR")
                {
                    TempData["PaymentError"] =
                        "Payment is not captured or the payment details do not match.";

                    return RedirectToAction("Pay", new { id = bookingId });
                }

                // 5. Mark Paid only if the matching record is still Pending.
                string updateQuery = @"
            UPDATE dbo.Payments
            SET PaymentStatus = 'Paid',
                TransactionReference = @PaymentId,
                PaidAt = GETDATE()
            WHERE BookingId = @BookingId
              AND RazorpayOrderId = @OrderId
              AND PaymentStatus = 'Pending'
              AND Amount = @Amount";

                int rowsUpdated;

                using (SqlConnection con = new SqlConnection(ConnectionString))
                using (SqlCommand cmd = new SqlCommand(updateQuery, con))
                {
                    cmd.Parameters.Add("@PaymentId", SqlDbType.NVarChar, 100).Value =
                        razorpayPaymentId;

                    cmd.Parameters.Add("@BookingId", SqlDbType.Int).Value =
                        bookingId;

                    cmd.Parameters.Add("@OrderId", SqlDbType.NVarChar, 100).Value =
                        razorpayOrderId;

                    cmd.Parameters.Add("@Amount", SqlDbType.Decimal).Value =
                        storedAmount;

                    cmd.Parameters["@Amount"].Precision = 10;
                    cmd.Parameters["@Amount"].Scale = 2;

                    con.Open();
                    rowsUpdated = cmd.ExecuteNonQuery();
                }

                if (rowsUpdated != 1)
                {
                    TempData["PaymentError"] =
                        "Payment record was not updated. Please check your booking.";

                    return RedirectToAction("Pay", new { id = bookingId });
                }

                TempData["PaymentSuccess"] =
                    "Payment successful! Your payment has been verified.";

                return RedirectToAction("MyBookings", "Bookings");
            }
            catch (Exception ex)
            {
                Trace.TraceError("HomeEase VerifyPayment error: " + ex);

                TempData["PaymentError"] =
                    "Payment verification could not be completed. Please check your payment status before retrying.";

                return RedirectToAction("Pay", new { id = bookingId });
            }
        }


    }
}
