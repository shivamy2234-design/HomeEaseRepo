using HomeEase.Models;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Web.Mvc;

namespace HomeEase.Controllers
{
    public class AdminController : Controller
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

        [HttpGet]
        public ActionResult Index()
        {
            
            if (Session["UserId"] == null ||
                Session["Role"] == null ||
                Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                using (SqlConnection con =
                    new SqlConnection(ConnectionString))
                {
                    string query = @"
                        SELECT
                            (SELECT COUNT(*) FROM dbo.Users
                             WHERE Role = 'Customer' AND IsActive = 1)
                                AS TotalCustomers,

                            (SELECT COUNT(*) FROM dbo.Services
                             WHERE IsActive = 1)
                                AS TotalServices,

                            (SELECT COUNT(*) FROM dbo.Bookings)
                                AS TotalBookings,

                            (SELECT COUNT(*) FROM dbo.Bookings
                             WHERE Status = 'Pending')
                                AS PendingBookings";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        con.Open();

                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                ViewBag.TotalCustomers =
                                    Convert.ToInt32(reader["TotalCustomers"]);

                                ViewBag.TotalServices =
                                    Convert.ToInt32(reader["TotalServices"]);

                                ViewBag.TotalBookings =
                                    Convert.ToInt32(reader["TotalBookings"]);

                                ViewBag.PendingBookings =
                                    Convert.ToInt32(reader["PendingBookings"]);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Admin dashboard failed: " + ex);

                ViewBag.ErrorMessage =
                    "Dashboard data could not be loaded. Please try again.";
            }

            return View();
        }
        [HttpGet]
        public ActionResult ManageServices()
        {
            if (Session["UserId"] == null ||
                Session["Role"] == null ||
                Session["Role"].ToString() != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            List<AdminServiceViewModel> services =
                new List<AdminServiceViewModel>();

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string query = @"
            SELECT ServiceId, ServiceName, Category, Description,
                   Price, DurationMinutes, IsActive
            FROM dbo.Services
            ORDER BY IsActive DESC, ServiceName";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            services.Add(new AdminServiceViewModel
                            {
                                ServiceId = Convert.ToInt32(reader["ServiceId"]),
                                ServiceName = reader["ServiceName"].ToString(),
                                Category = reader["Category"].ToString(),
                                Description = reader["Description"] == DBNull.Value
                                    ? ""
                                    : reader["Description"].ToString(),
                                Price = Convert.ToDecimal(reader["Price"]),
                                DurationMinutes =
                                    Convert.ToInt32(reader["DurationMinutes"]),
                                IsActive = Convert.ToBoolean(reader["IsActive"])
                            });
                        }
                    }
                }
            }

            return View(services);
        }

        [HttpGet]
        public ActionResult CreateService()
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            return View(new AdminServiceViewModel
            {
                IsActive = true,
                DurationMinutes = 60
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateService(AdminServiceViewModel model)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    string query = @"
                INSERT INTO dbo.Services
                    (ServiceName, Category, Description, Price,
                     DurationMinutes, IsActive, CreatedAt)
                VALUES
                    (@ServiceName, @Category, @Description, @Price,
                     @DurationMinutes, @IsActive, GETDATE())";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@ServiceName", SqlDbType.NVarChar, 100)
                            .Value = model.ServiceName.Trim();

                        cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 50)
                            .Value = model.Category.Trim();

                        cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 500)
                            .Value = string.IsNullOrWhiteSpace(model.Description)
                                ? (object)DBNull.Value
                                : model.Description.Trim();

                        SqlParameter priceParameter =
                            cmd.Parameters.Add("@Price", SqlDbType.Decimal);
                        priceParameter.Precision = 10;
                        priceParameter.Scale = 2;
                        priceParameter.Value = model.Price;

                        cmd.Parameters.Add("@DurationMinutes", SqlDbType.Int)
                            .Value = model.DurationMinutes;

                        cmd.Parameters.Add("@IsActive", SqlDbType.Bit)
                            .Value = model.IsActive;

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }

                TempData["AdminMessage"] = "Service added successfully.";
                return RedirectToAction("ManageServices");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Adding service failed: " + ex);
                ModelState.AddModelError("",
                    "Could not add the service. Please try again.");
                return View(model);
            }
        }

        private bool IsAdmin()
        {
            return Session["UserId"] != null
                && Session["Role"] != null
                && Session["Role"].ToString() == "Admin";
        }
        [HttpGet]
        public ActionResult EditService(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                string query = @"
            SELECT ServiceId, ServiceName, Category, Description,
                   Price, DurationMinutes, IsActive
            FROM dbo.Services
            WHERE ServiceId = @ServiceId";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@ServiceId", SqlDbType.Int).Value = id;
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (!reader.Read())
                            return HttpNotFound();

                        AdminServiceViewModel model = new AdminServiceViewModel
                        {
                            ServiceId = Convert.ToInt32(reader["ServiceId"]),
                            ServiceName = reader["ServiceName"].ToString(),
                            Category = reader["Category"].ToString(),
                            Description = reader["Description"] == DBNull.Value
                                ? ""
                                : reader["Description"].ToString(),
                            Price = Convert.ToDecimal(reader["Price"]),
                            DurationMinutes =
                                Convert.ToInt32(reader["DurationMinutes"]),
                            IsActive = Convert.ToBoolean(reader["IsActive"])
                        };

                        return View(model);
                    }
                }
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditService(AdminServiceViewModel model)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    string query = @"
                UPDATE dbo.Services
                SET ServiceName = @ServiceName,
                    Category = @Category,
                    Description = @Description,
                    Price = @Price,
                    DurationMinutes = @DurationMinutes,
                    IsActive = @IsActive
                WHERE ServiceId = @ServiceId";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@ServiceId", SqlDbType.Int)
                            .Value = model.ServiceId;
                        cmd.Parameters.Add("@ServiceName", SqlDbType.NVarChar, 100)
                            .Value = model.ServiceName.Trim();
                        cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 50)
                            .Value = model.Category.Trim();
                        cmd.Parameters.Add("@Description", SqlDbType.NVarChar, 500)
                            .Value = string.IsNullOrWhiteSpace(model.Description)
                                ? (object)DBNull.Value
                                : model.Description.Trim();

                        SqlParameter priceParameter =
                            cmd.Parameters.Add("@Price", SqlDbType.Decimal);
                        priceParameter.Precision = 10;
                        priceParameter.Scale = 2;
                        priceParameter.Value = model.Price;

                        cmd.Parameters.Add("@DurationMinutes", SqlDbType.Int)
                            .Value = model.DurationMinutes;
                        cmd.Parameters.Add("@IsActive", SqlDbType.Bit)
                            .Value = model.IsActive;

                        con.Open();

                        if (cmd.ExecuteNonQuery() == 0)
                            return HttpNotFound();
                    }
                }

                TempData["AdminMessage"] = "Service updated successfully.";
                return RedirectToAction("ManageServices");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Updating service failed: " + ex);
                ModelState.AddModelError("",
                    "Could not update the service. Please try again.");
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeactivateService(int id)
        {
            if (!IsAdmin())
                return RedirectToAction("Login", "Account");

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    string query = @"
                UPDATE dbo.Services
                SET IsActive = 0
                WHERE ServiceId = @ServiceId AND IsActive = 1";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@ServiceId", SqlDbType.Int).Value = id;
                        con.Open();

                        int rowsAffected = cmd.ExecuteNonQuery();

                        TempData["AdminMessage"] = rowsAffected > 0
                            ? "Service deactivated successfully."
                            : "Service not found or already inactive.";
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Deactivating service failed: " + ex);
                TempData["AdminMessage"] =
                    "Could not deactivate the service. Please try again.";
            }

            return RedirectToAction("ManageServices");
        }

        private List<SelectListItem> GetAvailableStaff()
        {
            List<SelectListItem> staffList = new List<SelectListItem>();

            string query = @"
        SELECT sp.StaffProfileId, u.FullName, sp.Skills,
               sp.ExperienceYears
        FROM dbo.StaffProfiles sp
        INNER JOIN dbo.Users u ON sp.UserId = u.UserId
        WHERE u.Role = 'Staff'
          AND u.IsActive = 1
          AND sp.IsAvailable = 1
        ORDER BY u.FullName";

            using (SqlConnection con = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string skills = reader["Skills"] == DBNull.Value
                            ? "General"
                            : reader["Skills"].ToString();

                        string text = reader["FullName"].ToString()
                            + " - " + skills
                            + " (" + Convert.ToInt32(reader["ExperienceYears"])
                            + " yrs)";

                        staffList.Add(new SelectListItem
                        {
                            Value = reader["StaffProfileId"].ToString(),
                            Text = text
                        });
                    }
                }
            }

            return staffList;
        }

        public ActionResult ManageBookings()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            List<AdminBookingViewModel> bookings =
                new List<AdminBookingViewModel>();

            string query = @"
        SELECT
            b.BookingId,
            u.FullName AS CustomerName,
            u.Email AS CustomerEmail,
            s.ServiceName,
            b.StaffProfileId,
            assignedUser.FullName AS AssignedStaffName,
            b.BookingDate,
            b.TimeSlot,
            b.Status,
            b.TotalAmount
        FROM dbo.Bookings b
        LEFT JOIN dbo.StaffProfiles assignedProfile
        ON b.StaffProfileId = assignedProfile.StaffProfileId
        LEFT JOIN dbo.Users assignedUser
    ON assignedProfile.UserId = assignedUser.UserId
        INNER JOIN dbo.Users u ON b.UserId = u.UserId
        INNER JOIN dbo.Services s ON b.ServiceId = s.ServiceId
        ORDER BY b.CreatedAt DESC";

            using (SqlConnection con = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        bookings.Add(new AdminBookingViewModel
                        {
                            BookingId = Convert.ToInt32(reader["BookingId"]),
                            CustomerName = reader["CustomerName"].ToString(),
                            CustomerEmail = reader["CustomerEmail"].ToString(),
                            ServiceName = reader["ServiceName"].ToString(),
                            BookingDate = Convert.ToDateTime(reader["BookingDate"]),
                            TimeSlot = reader["TimeSlot"].ToString(),
                            Status = reader["Status"].ToString(),
                            TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),

                            StaffProfileId = reader["StaffProfileId"] == DBNull.Value
                          ? (int?)null
                         : Convert.ToInt32(reader["StaffProfileId"]),

                            AssignedStaffName = reader["AssignedStaffName"] == DBNull.Value
                              ? "Not assigned"
                           : reader["AssignedStaffName"].ToString()
                        });
                    }
                }
            }
            ViewBag.AvailableStaff = GetAvailableStaff();

            return View(bookings);
        }

        public ActionResult ManageStaff()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            List<AdminStaffViewModel> staffList =
                new List<AdminStaffViewModel>();

            string query = @"
        SELECT
            u.UserId,
            u.FullName,
            u.Email,
            u.Phone,
            u.IsActive,
            sp.StaffProfileId,
            sp.Skills,
            sp.ExperienceYears,
            sp.IsAvailable
        FROM dbo.Users u
        INNER JOIN dbo.StaffProfiles sp
            ON u.UserId = sp.UserId
        WHERE u.Role = 'Staff'
        ORDER BY u.FullName";

            using (SqlConnection con = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        staffList.Add(new AdminStaffViewModel
                        {
                            UserId = Convert.ToInt32(reader["UserId"]),
                            StaffProfileId = Convert.ToInt32(reader["StaffProfileId"]),
                            FullName = reader["FullName"].ToString(),
                            Email = reader["Email"].ToString(),
                            Phone = reader["Phone"] == DBNull.Value
                                ? "" : reader["Phone"].ToString(),
                            IsActive = Convert.ToBoolean(reader["IsActive"]),
                            Skills = reader["Skills"] == DBNull.Value
                                ? "" : reader["Skills"].ToString(),
                            ExperienceYears = Convert.ToInt32(reader["ExperienceYears"]),
                            IsAvailable = Convert.ToBoolean(reader["IsAvailable"])
                        });
                    }
                }
            }

            return View(staffList);
        }

        private string HashStaffPassword(string password)
        {
            const int iterations = 210000;
            byte[] salt = new byte[16];

            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            using (var pbkdf2 = new Rfc2898DeriveBytes(
                password, salt, iterations))
            {
                byte[] hash = pbkdf2.GetBytes(32);

                return "PBKDF2$" + iterations + "$"
                    + Convert.ToBase64String(salt) + "$"
                    + Convert.ToBase64String(hash);
            }
        }


        [HttpGet]
        public ActionResult CreateStaff()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            return View(new AdminStaffViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateStaff(AdminStaffViewModel model)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string passwordHash = HashStaffPassword(model.Password);

            string userQuery = @"
        INSERT INTO dbo.Users
            (FullName, Email, PasswordHash, Phone, Role, IsActive, CreatedAt)
        VALUES
            (@FullName, @Email, @PasswordHash, @Phone, 'Staff', 1, GETDATE());

        SELECT CAST(SCOPE_IDENTITY() AS INT);";

            string profileQuery = @"
        INSERT INTO dbo.StaffProfiles
            (UserId, Skills, ExperienceYears, IsAvailable, CreatedAt)
        VALUES
            (@UserId, @Skills, @ExperienceYears, @IsAvailable, GETDATE());";

            using (SqlConnection con = new SqlConnection(ConnectionString))
            {
                con.Open();

                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        int userId;

                        using (SqlCommand cmd = new SqlCommand(
                            userQuery, con, transaction))
                        {
                            cmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 100)
                                .Value = model.FullName.Trim();

                            cmd.Parameters.Add("@Email", SqlDbType.NVarChar, 150)
                                .Value = model.Email.Trim();

                            cmd.Parameters.Add("@PasswordHash", SqlDbType.NVarChar, -1)
                                .Value = passwordHash;

                            cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 20)
                                .Value = string.IsNullOrWhiteSpace(model.Phone)
                                    ? (object)DBNull.Value : model.Phone.Trim();

                            userId = (int)cmd.ExecuteScalar();
                        }

                        using (SqlCommand cmd = new SqlCommand(
                            profileQuery, con, transaction))
                        {
                            cmd.Parameters.Add("@UserId", SqlDbType.Int)
                                .Value = userId;

                            cmd.Parameters.Add("@Skills", SqlDbType.NVarChar, -1)
                                .Value = string.IsNullOrWhiteSpace(model.Skills)
                                    ? (object)DBNull.Value : model.Skills.Trim();

                            cmd.Parameters.Add("@ExperienceYears", SqlDbType.Int)
                                .Value = model.ExperienceYears;

                            cmd.Parameters.Add("@IsAvailable", SqlDbType.Bit)
                                .Value = model.IsAvailable;

                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();

                        TempData["SuccessMessage"] = "Staff created successfully.";
                        return RedirectToAction("ManageStaff");
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();

                        if (ex.Number == 2601 || ex.Number == 2627)
                        {
                            ModelState.AddModelError(
                                "Email", "This email is already registered.");
                        }
                        else
                        {
                            ModelState.AddModelError(
                                "", "Unable to create staff. Please check the database fields.");
                        }

                        return View(model);
                    }
                    catch
                    {
                        transaction.Rollback();

                        ModelState.AddModelError(
                            "", "Unable to create staff. Please try again.");

                        return View(model);
                    }
                }
            }
        }
        [HttpGet]
        public ActionResult EditStaff(int id)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            AdminStaffViewModel staff = null;

            string query = @"
        SELECT u.UserId, u.FullName, u.Email, u.Phone, u.IsActive,
               sp.StaffProfileId, sp.Skills,
               sp.ExperienceYears, sp.IsAvailable
        FROM dbo.Users u
        INNER JOIN dbo.StaffProfiles sp ON u.UserId = sp.UserId
        WHERE u.UserId = @UserId AND u.Role = 'Staff'";

            using (SqlConnection con = new SqlConnection(ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, con))
            {
                cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = id;
                con.Open();

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        staff = new AdminStaffViewModel
                        {
                            UserId = Convert.ToInt32(reader["UserId"]),
                            StaffProfileId = Convert.ToInt32(reader["StaffProfileId"]),
                            FullName = reader["FullName"].ToString(),
                            Email = reader["Email"].ToString(),
                            Phone = reader["Phone"] == DBNull.Value
                                ? "" : reader["Phone"].ToString(),
                            IsActive = Convert.ToBoolean(reader["IsActive"]),
                            Skills = reader["Skills"] == DBNull.Value
                                ? "" : reader["Skills"].ToString(),
                            ExperienceYears = Convert.ToInt32(reader["ExperienceYears"]),
                            IsAvailable = Convert.ToBoolean(reader["IsAvailable"])
                        };
                    }
                }
            }

            if (staff == null)
            {
                return HttpNotFound();
            }

            return View(staff);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditStaff(AdminStaffViewModel model)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            // Password is required for CreateStaff, not for editing.
            ModelState.Remove("Password");

            if (model.ExperienceYears < 0 || model.ExperienceYears > 50)
            {
                ModelState.AddModelError(
                    "ExperienceYears", "Experience must be between 0 and 50 years.");
            }

            if (model.Skills != null && model.Skills.Length > 500)
            {
                ModelState.AddModelError(
                    "Skills", "Skills cannot exceed 500 characters.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string query = @"
        UPDATE sp
        SET sp.Skills = @Skills,
            sp.ExperienceYears = @ExperienceYears,
            sp.IsAvailable = @IsAvailable
        FROM dbo.StaffProfiles sp
        INNER JOIN dbo.Users u ON sp.UserId = u.UserId
        WHERE u.UserId = @UserId AND u.Role = 'Staff';

        UPDATE dbo.Users
        SET IsActive = @IsActive
        WHERE UserId = @UserId AND Role = 'Staff';";

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@Skills", SqlDbType.NVarChar, 500).Value =
                        string.IsNullOrWhiteSpace(model.Skills)
                            ? (object)DBNull.Value : model.Skills.Trim();

                    cmd.Parameters.Add("@ExperienceYears", SqlDbType.Int).Value =
                        model.ExperienceYears;

                    cmd.Parameters.Add("@IsAvailable", SqlDbType.Bit).Value =
                        model.IsAvailable;

                    cmd.Parameters.Add("@IsActive", SqlDbType.Bit).Value =
                        model.IsActive;

                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value =
                        model.UserId;

                    con.Open();
                    int rows = cmd.ExecuteNonQuery();

                    if (rows == 0)
                    {
                        return HttpNotFound();
                    }
                }

                TempData["SuccessMessage"] = "Staff details updated successfully.";
                return RedirectToAction("ManageStaff");
            }
            catch (SqlException)
            {
                ModelState.AddModelError(
                    "", "Unable to update staff. Please try again.");

                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateBookingStatus(int id, string status)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            string[] validStatuses =
            {
        "Pending",
        "Confirmed",
        "InProgress",
        "Completed",
        "Cancelled"
    };

            if (Array.IndexOf(validStatuses, status) < 0)
            {
                TempData["AdminMessage"] = "Invalid booking status.";
                return RedirectToAction("ManageBookings");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    string query = @"
                UPDATE dbo.Bookings
                SET Status = @Status
                WHERE BookingId = @BookingId";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.Add("@Status", SqlDbType.NVarChar, 30)
                            .Value = status;

                        cmd.Parameters.Add("@BookingId", SqlDbType.Int)
                            .Value = id;

                        con.Open();

                        int rows = cmd.ExecuteNonQuery();

                        TempData["AdminMessage"] = rows > 0
                            ? "Booking status updated successfully."
                            : "Booking not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Updating booking status failed: " + ex);
                TempData["AdminMessage"] =
                    "Unable to update booking status. Please try again.";
            }

            return RedirectToAction("ManageBookings");
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AssignStaff(int id, int? staffProfileId)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                {
                    con.Open();

                    if (staffProfileId.HasValue)
                    {
                        string checkQuery = @"
                    SELECT COUNT(*)
                    FROM dbo.StaffProfiles sp
                    INNER JOIN dbo.Users u ON sp.UserId = u.UserId
                    WHERE sp.StaffProfileId = @StaffProfileId
                      AND u.Role = 'Staff'
                      AND u.IsActive = 1
                      AND sp.IsAvailable = 1";

                        using (SqlCommand checkCmd =
                            new SqlCommand(checkQuery, con))
                        {
                            checkCmd.Parameters.Add("@StaffProfileId", SqlDbType.Int)
                                .Value = staffProfileId.Value;

                            if (Convert.ToInt32(checkCmd.ExecuteScalar()) == 0)
                            {
                                TempData["AdminMessage"] =
                                    "Selected staff member is not available.";
                                return RedirectToAction("ManageBookings");
                            }
                        }
                    }

                    string updateQuery = @"
                UPDATE dbo.Bookings
                SET StaffProfileId = @StaffProfileId
                WHERE BookingId = @BookingId";

                    using (SqlCommand cmd =
                        new SqlCommand(updateQuery, con))
                    {
                        cmd.Parameters.Add("@BookingId", SqlDbType.Int).Value = id;

                        cmd.Parameters.Add("@StaffProfileId", SqlDbType.Int).Value =
                            staffProfileId.HasValue
                                ? (object)staffProfileId.Value
                                : DBNull.Value;

                        int rows = cmd.ExecuteNonQuery();

                        TempData["AdminMessage"] = rows > 0
                            ? "Staff assignment updated successfully."
                            : "Booking not found.";
                    }
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError("Assigning staff failed: " + ex);
                TempData["AdminMessage"] =
                    "Unable to assign staff. Please try again.";
            }

            return RedirectToAction("ManageBookings");
        }

        [HttpGet]
        public ActionResult ManagePayments()
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            List<AdminPaymentViewModel> payments =
                new List<AdminPaymentViewModel>();

            string query = @"
        SELECT
            p.PaymentId,
            p.BookingId,
            u.FullName AS CustomerName,
            u.Email AS CustomerEmail,
            s.ServiceName,
            p.Amount,
            p.PaymentMethod,
            p.PaymentStatus,
            p.TransactionReference,
            p.PaidAt,
            p.CreatedAt
        FROM dbo.Payments p
        INNER JOIN dbo.Bookings b
            ON p.BookingId = b.BookingId
        INNER JOIN dbo.Users u
            ON b.UserId = u.UserId
        INNER JOIN dbo.Services s
            ON b.ServiceId = s.ServiceId
        ORDER BY p.CreatedAt DESC";

            try
            {
                using (SqlConnection con = new SqlConnection(ConnectionString))
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            payments.Add(new AdminPaymentViewModel
                            {
                                PaymentId = Convert.ToInt32(reader["PaymentId"]),
                                BookingId = Convert.ToInt32(reader["BookingId"]),
                                CustomerName = reader["CustomerName"].ToString(),
                                CustomerEmail = reader["CustomerEmail"].ToString(),
                                ServiceName = reader["ServiceName"].ToString(),
                                Amount = Convert.ToDecimal(reader["Amount"]),
                                PaymentMethod = reader["PaymentMethod"].ToString(),
                                PaymentStatus = reader["PaymentStatus"].ToString(),

                                TransactionReference =
                                    reader["TransactionReference"] == DBNull.Value
                                    ? ""
                                    : reader["TransactionReference"].ToString(),

                                PaidAt = reader["PaidAt"] == DBNull.Value
                                    ? (DateTime?)null
                                    : Convert.ToDateTime(reader["PaidAt"]),

                                CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                            });
                        }
                    }
                }

                ViewBag.TotalCollected = payments
                    .Where(p => p.PaymentStatus == "Paid")
                    .Sum(p => p.Amount);

                ViewBag.PaidCount = payments
                    .Count(p => p.PaymentStatus == "Paid");

                ViewBag.PendingCount = payments
                    .Count(p => p.PaymentStatus == "Pending");

                ViewBag.FailedCount = payments
                    .Count(p => p.PaymentStatus == "Failed");
            }
            catch (Exception ex)
            {
                Trace.TraceError("Loading admin payments failed: " + ex);

                ViewBag.ErrorMessage =
                    "Payment records could not be loaded. Please try again.";
            }

            return View(payments);
        }


    }
}