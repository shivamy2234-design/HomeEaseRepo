using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Web.Mvc;
using HomeEase.Models;

namespace HomeEase.Controllers
{
    public class BookingsController : Controller
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

        private bool IsCustomerLoggedIn()
        {
            return Session["UserId"] != null
                && Session["Role"] != null
                && Session["Role"].ToString() == "Customer";
        }

        private void LoadFormOptions(int userId, int selectedAddressId = 0)
        {
            List<SelectListItem> addresses = new List<SelectListItem>();

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string query = @"
                    SELECT AddressId, AddressLine, City, State, PostalCode
                    FROM dbo.Addresses
                    WHERE UserId = @UserId
                    ORDER BY IsDefault DESC, AddressId DESC";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string addressText =
                                reader["AddressLine"].ToString() + ", " +
                                reader["City"].ToString() + ", " +
                                reader["State"].ToString() + " - " +
                                reader["PostalCode"].ToString();

                            addresses.Add(new SelectListItem
                            {
                                Value = reader["AddressId"].ToString(),
                                Text = addressText,
                                Selected = Convert.ToInt32(reader["AddressId"])
                                    == selectedAddressId
                            });
                        }
                    }
                }
            }

            ViewBag.Addresses = addresses;

            ViewBag.TimeSlots = new List<SelectListItem>
            {
                new SelectListItem { Text = "9 AM - 11 AM", Value = "9 AM - 11 AM" },
                new SelectListItem { Text = "11 AM - 1 PM", Value = "11 AM - 1 PM" },
                new SelectListItem { Text = "1 PM - 3 PM", Value = "1 PM - 3 PM" },
                new SelectListItem { Text = "3 PM - 5 PM", Value = "3 PM - 5 PM" },
                new SelectListItem { Text = "5 PM - 7 PM", Value = "5 PM - 7 PM" }
            };
        }

        private bool IsActiveService(int serviceId, out string serviceName,
            out decimal price)
        {
            serviceName = "";
            price = 0;

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string query = @"
                    SELECT ServiceName, Price
                    FROM dbo.Services
                    WHERE ServiceId = @ServiceId AND IsActive = 1";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@ServiceId", SqlDbType.Int).Value = serviceId;

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return false;
                        }

                        serviceName = reader["ServiceName"].ToString();
                        price = Convert.ToDecimal(reader["Price"]);
                        return true;
                    }
                }
            }
        }

        private bool AddressBelongsToCustomer(int addressId, int userId)
        {
            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string query = @"
                    SELECT COUNT(*)
                    FROM dbo.Addresses
                    WHERE AddressId = @AddressId AND UserId = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@AddressId", SqlDbType.Int).Value = addressId;
                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                    con.Open();

                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        [HttpGet]
        public ActionResult Create(int serviceId)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            string serviceName;
            decimal price;

            if (!IsActiveService(serviceId, out serviceName, out price))
            {
                return HttpNotFound("Active service not found.");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            LoadFormOptions(userId);

            List<SelectListItem> addresses = ViewBag.Addresses;

            if (addresses.Count == 0)
            {
                TempData["AddressError"] =
                    "Please add an address before booking a service.";

                return RedirectToAction("Addresses", "Account");
            }

            BookingViewModel model = new BookingViewModel
            {
                ServiceId = serviceId,
                ServiceName = serviceName,
                TotalAmount = price,
                BookingDate = DateTime.Today.AddDays(1)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(BookingViewModel model)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            string serviceName;
            decimal price;

            bool serviceExists =
                IsActiveService(model.ServiceId, out serviceName, out price);

            if (!serviceExists)
            {
                return HttpNotFound("Active service not found.");
            }

            model.ServiceName = serviceName;
            model.TotalAmount = price;

            if (model.BookingDate.Date < DateTime.Today)
            {
                ModelState.AddModelError(
                    "BookingDate", "Booking date cannot be in the past.");
            }

            string[] validSlots =
            {
                "9 AM - 11 AM",
                "11 AM - 1 PM",
                "1 PM - 3 PM",
                "3 PM - 5 PM",
                "5 PM - 7 PM"
            };

            if (Array.IndexOf(validSlots, model.TimeSlot) < 0)
            {
                ModelState.AddModelError(
                    "TimeSlot", "Please select a valid time slot.");
            }

            if (!AddressBelongsToCustomer(model.AddressId, userId))
            {
                ModelState.AddModelError(
                    "AddressId", "Please select one of your saved addresses.");
            }

            if (!ModelState.IsValid)
            {
                LoadFormOptions(userId, model.AddressId);
                return View(model);
            }

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    string query = @"
                        INSERT INTO dbo.Bookings
                        (
                            UserId, ServiceId, AddressId, BookingDate,
                            TimeSlot, Status, TotalAmount, SpecialInstructions
                        )
                        VALUES
                        (
                            @UserId, @ServiceId, @AddressId, @BookingDate,
                            @TimeSlot, @Status, @TotalAmount, @SpecialInstructions
                        );

                        SELECT CAST(SCOPE_IDENTITY() AS INT);";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                        cmd.Parameters.Add("@ServiceId", SqlDbType.Int)
                            .Value = model.ServiceId;
                        cmd.Parameters.Add("@AddressId", SqlDbType.Int)
                            .Value = model.AddressId;
                        cmd.Parameters.Add("@BookingDate", SqlDbType.Date)
                            .Value = model.BookingDate.Date;
                        cmd.Parameters.Add("@TimeSlot", SqlDbType.NVarChar, 50)
                            .Value = model.TimeSlot;
                        cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 30)
                            .Value = "Pending";
                        cmd.Parameters.Add("@TotalAmount", SqlDbType.Decimal)
                            .Value = price;

                        cmd.Parameters["@TotalAmount"].Precision = 10;
                        cmd.Parameters["@TotalAmount"].Scale = 2;

                        cmd.Parameters.Add("@SpecialInstructions",
                            SqlDbType.NVarChar, 500).Value =
                            string.IsNullOrWhiteSpace(model.SpecialInstructions)
                            ? (object)DBNull.Value
                            : model.SpecialInstructions.Trim();

                        con.Open();

                        int bookingId = Convert.ToInt32(cmd.ExecuteScalar());

                        TempData["BookingMessage"] =
                            "Your booking has been submitted successfully.";

                        return RedirectToAction("Confirmation",
                            new { id = bookingId });
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Booking creation failed: " + ex);

                ModelState.AddModelError("",
                    "We could not save your booking. Please try again.");

                LoadFormOptions(userId, model.AddressId);
                return View(model);
            }
        }
        [HttpGet]
        public ActionResult MyBookings()
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            List<BookingHistoryViewModel> bookings =
                new List<BookingHistoryViewModel>();

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    string query = @"
                
SELECT
    b.BookingId,
    s.ServiceName,
    b.BookingDate,
    b.TimeSlot,
    b.Status,
    b.TotalAmount,

    CASE
        WHEN EXISTS (
            SELECT 1
            FROM dbo.Reviews r
            WHERE r.BookingId = b.BookingId
        )
        THEN 1
        ELSE 0
    END AS HasReview,

    CASE
        WHEN EXISTS (
            SELECT 1
            FROM dbo.Payments p
            WHERE p.BookingId = b.BookingId
              AND p.PaymentStatus = 'Paid'
        )
        THEN 1
        ELSE 0
    END AS HasPaid

FROM dbo.Bookings b
INNER JOIN dbo.Services s
    ON b.ServiceId = s.ServiceId
WHERE b.UserId = @UserId
ORDER BY b.CreatedAt DESC";


                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                        con.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                bookings.Add(new BookingHistoryViewModel
                                {
                                    BookingId = Convert.ToInt32(reader["BookingId"]),
                                    ServiceName = reader["ServiceName"].ToString(),
                                    BookingDate = Convert.ToDateTime(reader["BookingDate"]),
                                    TimeSlot = reader["TimeSlot"].ToString(),
                                    Status = reader["Status"].ToString(),
                                    TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                    HasReview = Convert.ToBoolean(reader["HasReview"]),
                                    HasPaid = Convert.ToBoolean(reader["HasPaid"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Loading booking history failed: " + ex);
                ViewBag.ErrorMessage =
                    "We could not load your bookings. Please try again.";
            }

            return View(bookings);
        }

        [HttpGet]
        public ActionResult Confirmation(int id)
        {
            if (!IsCustomerLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string query = @"
                    SELECT b.BookingId, b.BookingDate, b.TimeSlot,
                           b.Status, b.TotalAmount, s.ServiceName
                    FROM dbo.Bookings b
                    INNER JOIN dbo.Services s ON b.ServiceId = s.ServiceId
                    WHERE b.BookingId = @BookingId AND b.UserId = @UserId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@BookingId", SqlDbType.Int).Value = id;
                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            return HttpNotFound();
                        }

                        ViewBag.BookingId = Convert.ToInt32(reader["BookingId"]);
                        ViewBag.BookingDate =
                            Convert.ToDateTime(reader["BookingDate"]).ToString("dd MMM yyyy");
                        ViewBag.TimeSlot = reader["TimeSlot"].ToString();
                        ViewBag.Status = reader["Status"].ToString();
                        ViewBag.TotalAmount = Convert.ToDecimal(reader["TotalAmount"]);
                        ViewBag.ServiceName = reader["ServiceName"].ToString();
                    }
                }
            }

            return View();
        }

[HttpGet]
public ActionResult CreateReview(int id)
{
    if (!IsCustomerLoggedIn())
    {
        return RedirectToAction("Login", "Account");
    }

    int userId = Convert.ToInt32(Session["UserId"]);

    string query = @"
        SELECT s.ServiceName
        FROM dbo.Bookings b
        INNER JOIN dbo.Services s ON b.ServiceId = s.ServiceId
        WHERE b.BookingId = @BookingId
          AND b.UserId = @UserId
          AND b.Status = 'Completed'
          AND NOT EXISTS (
              SELECT 1 FROM dbo.Reviews r
              WHERE r.BookingId = b.BookingId
          )";

    using (SqlConnection con = new SqlConnection(ConnectionString))
    using (SqlCommand cmd = new SqlCommand(query, con))
    {
        cmd.Parameters.Add("@BookingId", SqlDbType.Int).Value = id;
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        con.Open();
        object result = cmd.ExecuteScalar();

        if (result == null)
        {
            TempData["ReviewMessage"] =
                "Only completed bookings without an existing review can be reviewed.";

            return RedirectToAction("MyBookings");
        }

        return View(new ReviewViewModel
        {
            BookingId = id,
            ServiceName = result.ToString()
        });
    }
}


[HttpPost]
[ValidateAntiForgeryToken]
public ActionResult CreateReview(ReviewViewModel model)
{
    if (!IsCustomerLoggedIn())
    {
        return RedirectToAction("Login", "Account");
    }

    int userId = Convert.ToInt32(Session["UserId"]);

    if (!ModelState.IsValid)
    {
        string serviceName = GetReviewServiceName(model.BookingId, userId);

        if (serviceName == null)
        {
            return HttpNotFound();
        }

        model.ServiceName = serviceName;
        return View(model);
    }

    string query = @"
        INSERT INTO dbo.Reviews
            (BookingId, UserId, Rating, Comment, CreatedAt)
        SELECT b.BookingId, b.UserId, @Rating, @Comment, GETDATE()
        FROM dbo.Bookings b
        WHERE b.BookingId = @BookingId
          AND b.UserId = @UserId
          AND b.Status = 'Completed'
          AND NOT EXISTS (
              SELECT 1 FROM dbo.Reviews r
              WHERE r.BookingId = b.BookingId
          );";

    try
    {
        using (SqlConnection con = new SqlConnection(ConnectionString))
        using (SqlCommand cmd = new SqlCommand(query, con))
        {
            cmd.Parameters.Add("@BookingId", SqlDbType.Int).Value =
                model.BookingId;

            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

            cmd.Parameters.Add("@Rating", SqlDbType.Int).Value =
                model.Rating;

            cmd.Parameters.Add("@Comment", SqlDbType.NVarChar, 500).Value =
                string.IsNullOrWhiteSpace(model.Comment)
                    ? (object)DBNull.Value
                    : model.Comment.Trim();

            con.Open();

            int rows = cmd.ExecuteNonQuery();

            if (rows == 0)
            {
                ModelState.AddModelError("",
                    "Review could not be saved. The booking may already have a review.");

                model.ServiceName = GetReviewServiceName(
                    model.BookingId, userId);

                if (model.ServiceName == null)
                {
                    return RedirectToAction("MyBookings");
                }

                return View(model);
            }
        }

        TempData["ReviewMessage"] = "Thank you! Your review has been submitted.";
        return RedirectToAction("MyBookings");
    }
    catch (SqlException ex)
    {
        Trace.TraceError("Saving review failed: " + ex);

        ModelState.AddModelError("",
            "Unable to save your review. Please try again.");

        model.ServiceName = GetReviewServiceName(model.BookingId, userId);

        if (model.ServiceName == null)
        {
            return RedirectToAction("MyBookings");
        }

        return View(model);
    }
}


private string GetReviewServiceName(int bookingId, int userId)
{
    string query = @"
        SELECT s.ServiceName
        FROM dbo.Bookings b
        INNER JOIN dbo.Services s ON b.ServiceId = s.ServiceId
        WHERE b.BookingId = @BookingId
          AND b.UserId = @UserId
          AND b.Status = 'Completed'
          AND NOT EXISTS (
              SELECT 1 FROM dbo.Reviews r
              WHERE r.BookingId = b.BookingId
          )";

    using (SqlConnection con = new SqlConnection(ConnectionString))
    using (SqlCommand cmd = new SqlCommand(query, con))
    {
        cmd.Parameters.Add("@BookingId", SqlDbType.Int).Value = bookingId;
        cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

        con.Open();
        object result = cmd.ExecuteScalar();

        return result == null ? null : result.ToString();
    }
}

    }
}
