using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Web.Mvc;
using HomeEase.Models;
using System.Linq;
using System.Web;
using System.Collections.Generic;



namespace HomeEase.Controllers
{
    public class AccountController : Controller
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
        public ActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string passwordHash = HashPassword(model.Password);

            try
            {
                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                using (SqlCommand command = new SqlCommand(
                    @"INSERT INTO dbo.Users
                      (FullName, Email, PasswordHash, Phone, Role, IsActive)
                      VALUES
                      (@FullName, @Email, @PasswordHash, @Phone,
                       @Role, @IsActive)", connection))
                {
                    command.Parameters.Add(
                        "@FullName", SqlDbType.NVarChar, 100)
                        .Value = model.FullName.Trim();

                    command.Parameters.Add(
                        "@Email", SqlDbType.NVarChar, 150)
                        .Value = model.Email.Trim();

                    command.Parameters.Add(
                        "@PasswordHash", SqlDbType.NVarChar, 255)
                        .Value = passwordHash;

                    command.Parameters.Add(
                        "@Phone", SqlDbType.NVarChar, 15)
                        .Value = string.IsNullOrWhiteSpace(model.Phone)
                            ? (object)DBNull.Value
                            : model.Phone.Trim();

                    // Role is set by the server, not the form.
                    command.Parameters.Add(
                        "@Role", SqlDbType.NVarChar, 20)
                        .Value = "Customer";

                    command.Parameters.Add(
                        "@IsActive", SqlDbType.Bit)
                        .Value = true;

                    connection.Open();
                    command.ExecuteNonQuery();
                }

                TempData["SuccessMessage"] =
                    "Registration successful! Your account has been created.";

                return RedirectToAction("Register");
            }
            catch (SqlException ex)
            {
                // SQL Server duplicate-key errors.
                if (ex.Number == 2601 || ex.Number == 2627)
                {
                    ModelState.AddModelError(
                        "Email",
                        "This email is already registered.");

                    return View(model);
                }

                // Keep technical database details out of the UI.
                Trace.TraceError(ex.ToString());

                ViewBag.ErrorMessage =
                    "Registration could not be completed. Please try again.";

                return View(model);
            }
        }

        private string HashPassword(string password)
        {
            const int iterations = 210000;
            const int saltSize = 16;
            const int hashSize = 32;

            byte[] salt = new byte[saltSize];

            using (RandomNumberGenerator random =
                RandomNumberGenerator.Create())
            {
                random.GetBytes(salt);
            }

            using (Rfc2898DeriveBytes pbkdf2 =
                new Rfc2898DeriveBytes(password, salt, iterations))
            {
                byte[] hash = pbkdf2.GetBytes(hashSize);

                return "PBKDF2$" + iterations + "$"
                    + Convert.ToBase64String(salt) + "$"
                    + Convert.ToBase64String(hash);
            }
        }

        [HttpGet]
        public ActionResult Login()
        {
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                int userId;
                string fullName;
                string passwordHash;
                string role;
                bool isActive;

                using (SqlConnection connection =
                    new SqlConnection(ConnectionString))
                using (SqlCommand command = new SqlCommand(
                    @"SELECT UserId, FullName, PasswordHash, Role, IsActive
              FROM dbo.Users
              WHERE Email = @Email", connection))
                {
                    command.Parameters.Add(
                        "@Email", SqlDbType.NVarChar, 150)
                        .Value = model.Email.Trim();

                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (!reader.Read())
                        {
                            ModelState.AddModelError("",
                                "Invalid email or password.");
                            return View(model);
                        }

                        userId = reader.GetInt32(0);
                        fullName = reader.GetString(1);
                        passwordHash = reader.GetString(2);
                        role = reader.GetString(3);
                        isActive = reader.GetBoolean(4);
                    }
                }

                if (!VerifyPassword(model.Password, passwordHash) || !isActive)
                {
                    ModelState.AddModelError("",
                        "Invalid email or password.");
                    return View(model);
                }

                // Remove any old session identity before creating a new one.
                Session.Clear();
                Session["UserId"] = userId;
                Session["FullName"] = fullName;
                Session["Role"] = role;

                if (role == "Admin")
                {
                    return RedirectToAction("Index", "Admin");
                }

                if (role == "Staff")
                {
                    return RedirectToAction("Index", "Staff");
                }

                return RedirectToAction("Dashboard");
            }
            catch (SqlException ex)
            {
                System.Diagnostics.Trace.TraceError(ex.ToString());

                ModelState.AddModelError("",
                    "Login is temporarily unavailable. Please try again.");

                return View(model);
            }
        }

        private bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                string[] parts = storedHash.Split('$');

                if (parts.Length != 4 || parts[0] != "PBKDF2")
                {
                    return false;
                }

                int iterations = int.Parse(parts[1]);
                byte[] salt = Convert.FromBase64String(parts[2]);
                byte[] expectedHash = Convert.FromBase64String(parts[3]);

                if (iterations < 1 || salt.Length == 0 ||
                    expectedHash.Length == 0)
                {
                    return false;
                }

                using (Rfc2898DeriveBytes pbkdf2 =
                    new Rfc2898DeriveBytes(password, salt, iterations))
                {
                    byte[] actualHash = pbkdf2.GetBytes(expectedHash.Length);

                    if (actualHash.Length != expectedHash.Length)
                    {
                        return false;
                    }

                    int difference = 0;

                    for (int i = 0; i < actualHash.Length; i++)
                    {
                        difference |= actualHash[i] ^ expectedHash[i];
                    }

                    return difference == 0;
                }
            }
            catch (FormatException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public ActionResult Dashboard()
        {
            if (Session["UserId"] == null ||
                (Session["Role"] as string) != "Customer")
            {
                return RedirectToAction("Login");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Clear();
            Session.Abandon();

            return RedirectToAction("Login");
        }

        [HttpGet]
        public ActionResult Profile()
        {
            if (Session["UserId"] == null ||
                Session["Role"] == null ||
                Session["Role"].ToString() != "Customer")
            {
                return RedirectToAction("Login");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            string connectionString =
                ConfigurationManager.ConnectionStrings["HomeEaseConnection"].ConnectionString;

            CustomerProfileViewModel model = null;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"SELECT UserId, FullName, Email, Phone
                         FROM dbo.Users
                         WHERE UserId = @UserId AND Role = 'Customer'";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            model = new CustomerProfileViewModel
                            {
                                UserId = Convert.ToInt32(reader["UserId"]),
                                FullName = reader["FullName"].ToString(),
                                Email = reader["Email"].ToString(),
                                Phone = reader["Phone"] == DBNull.Value
                                    ? ""
                                    : reader["Phone"].ToString()
                            };
                        }
                    }
                }
            }

            if (model == null)
            {
                Session.Clear();
                Session.Abandon();
                return RedirectToAction("Login");
            }

            return View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(CustomerProfileViewModel model)
        {
            if (Session["UserId"] == null ||
                Session["Role"] == null ||
                Session["Role"].ToString() != "Customer")
            {
                return RedirectToAction("Login");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string connectionString =
                ConfigurationManager.ConnectionStrings["HomeEaseConnection"].ConnectionString;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"UPDATE dbo.Users
                         SET FullName = @FullName, Phone = @Phone
                         WHERE UserId = @UserId AND Role = 'Customer'";

                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@FullName", SqlDbType.NVarChar, 100)
                        .Value = model.FullName.Trim();

                    cmd.Parameters.Add("@Phone", SqlDbType.NVarChar, 15)
                        .Value = string.IsNullOrWhiteSpace(model.Phone)
                            ? (object)DBNull.Value
                            : model.Phone.Trim();

                    cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                    con.Open();
                    int rowsAffected = cmd.ExecuteNonQuery();

                    if (rowsAffected == 0)
                    {
                        ModelState.AddModelError("", "Profile could not be updated.");
                        return View(model);
                    }
                }
            }

            Session["FullName"] = model.FullName.Trim();
            TempData["ProfileMessage"] = "Profile updated successfully.";

            return RedirectToAction("Profile");
        }


        [HttpGet]
        public ActionResult Addresses()
        {
            if (Session["UserId"] == null ||
                Session["Role"] == null ||
                Session["Role"].ToString() != "Customer")
            {
                return RedirectToAction("Login");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            string connectionString =
                ConfigurationManager.ConnectionStrings["HomeEaseConnection"].ConnectionString;

            List<AddressViewModel> addresses = new List<AddressViewModel>();

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                string query = @"SELECT AddressId, AddressLine, City, State,
                                PostalCode, IsDefault
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
                            addresses.Add(new AddressViewModel
                            {
                                AddressId = Convert.ToInt32(reader["AddressId"]),
                                AddressLine = reader["AddressLine"].ToString(),
                                City = reader["City"].ToString(),
                                State = reader["State"].ToString(),
                                PostalCode = reader["PostalCode"].ToString(),
                                IsDefault = Convert.ToBoolean(reader["IsDefault"])
                            });
                        }
                    }
                }
            }

            return View(addresses);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddAddress(AddressViewModel model)
        {
            if (Session["UserId"] == null ||
                Session["Role"] == null ||
                Session["Role"].ToString() != "Customer")
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                TempData["AddressError"] =
                    "Please enter valid address details, including a 6-digit PIN code.";

                return RedirectToAction("Addresses");
            }

            int userId = Convert.ToInt32(Session["UserId"]);

            string connectionString =
                ConfigurationManager.ConnectionStrings["HomeEaseConnection"].ConnectionString;

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                con.Open();

                using (SqlTransaction transaction = con.BeginTransaction())
                {
                    try
                    {
                        if (model.IsDefault)
                        {
                            string resetQuery =
                                "UPDATE dbo.Addresses SET IsDefault = 0 WHERE UserId = @UserId";

                            using (SqlCommand resetCmd =
                                new SqlCommand(resetQuery, con, transaction))
                            {
                                resetCmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;
                                resetCmd.ExecuteNonQuery();
                            }
                        }

                        string insertQuery = @"
                    INSERT INTO dbo.Addresses
                    (UserId, AddressLine, City, State, PostalCode, IsDefault)
                    VALUES
                    (@UserId, @AddressLine, @City, @State, @PostalCode, @IsDefault)";

                        using (SqlCommand cmd =
                            new SqlCommand(insertQuery, con, transaction))
                        {
                            cmd.Parameters.Add("@UserId", SqlDbType.Int).Value = userId;

                            cmd.Parameters.Add("@AddressLine", SqlDbType.NVarChar, 200)
                                .Value = model.AddressLine.Trim();

                            cmd.Parameters.Add("@City", SqlDbType.NVarChar, 100)
                                .Value = model.City.Trim();

                            cmd.Parameters.Add("@State", SqlDbType.NVarChar, 100)
                                .Value = model.State.Trim();

                            cmd.Parameters.Add("@PostalCode", SqlDbType.NVarChar, 10)
                                .Value = model.PostalCode.Trim();

                            cmd.Parameters.Add("@IsDefault", SqlDbType.Bit)
                                .Value = model.IsDefault;

                            cmd.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        TempData["AddressMessage"] = "Address added successfully.";
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        System.Diagnostics.Trace.TraceError(
                            "AddAddress failed: " + ex);

                        TempData["AddressError"] =
                            "Address could not be saved. Please try again.";
                    }
                }
            }

            return RedirectToAction("Addresses");
        }


    }
}
