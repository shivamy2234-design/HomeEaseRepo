using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Web.Mvc;
using HomeEase.Models;

namespace HomeEase.Controllers
{
    public class ServicesController : Controller
    {
        [HttpGet]
        public ActionResult Index(string category)
        {
            List<ServiceViewModel> services =
                new List<ServiceViewModel>();

            string connectionString =
                ConfigurationManager
                .ConnectionStrings["HomeEaseConnection"]
                .ConnectionString;

            using (SqlConnection con =
                new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT ServiceId, ServiceName, Category,
                           Description, Price, DurationMinutes
                    FROM dbo.Services
                    WHERE IsActive = 1
                      AND (@Category IS NULL OR Category = @Category)
                    ORDER BY Category, ServiceName";

                using (SqlCommand cmd =
                    new SqlCommand(query, con))
                {
                    string selectedCategory =
                        string.IsNullOrWhiteSpace(category)
                        ? null
                        : category.Trim();

                    cmd.Parameters.Add("@Category", SqlDbType.NVarChar, 50)
                        .Value = (object)selectedCategory ?? DBNull.Value;

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            services.Add(new ServiceViewModel
                            {
                                ServiceId = Convert.ToInt32(reader["ServiceId"]),
                                ServiceName = reader["ServiceName"].ToString(),
                                Category = reader["Category"].ToString(),
                                Description =
                                    reader["Description"] == DBNull.Value
                                    ? ""
                                    : reader["Description"].ToString(),
                                Price = Convert.ToDecimal(reader["Price"]),
                                DurationMinutes =
                                    Convert.ToInt32(reader["DurationMinutes"])
                            });
                        }
                    }
                }
            }
            List<string> categories = new List<string>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string query = @"
        SELECT DISTINCT Category
        FROM dbo.Services
        WHERE IsActive = 1
        ORDER BY Category";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    connection.Open();

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader["Category"] != DBNull.Value)
                            {
                                categories.Add(reader["Category"].ToString());
                            }
                        }
                    }
                }
            }

            ViewBag.Categories = categories;

            ViewBag.SelectedCategory = category;

            return View(services);
        }

        [HttpGet]
        public ActionResult Details(int id)
        {
            if (id <= 0)
            {
                return new HttpStatusCodeResult(400);
            }

            ServiceViewModel service = null;

            string connectionString =
                ConfigurationManager
                .ConnectionStrings["HomeEaseConnection"]
                .ConnectionString;

            using (SqlConnection con =
                new SqlConnection(connectionString))
            {
                string query = @"
                    SELECT ServiceId, ServiceName, Category,
                           Description, Price, DurationMinutes
                    FROM dbo.Services
                    WHERE ServiceId = @ServiceId
                      AND IsActive = 1";

                using (SqlCommand cmd =
                    new SqlCommand(query, con))
                {
                    cmd.Parameters.Add("@ServiceId", SqlDbType.Int)
                        .Value = id;

                    con.Open();

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            service = new ServiceViewModel
                            {
                                ServiceId =
                                    Convert.ToInt32(reader["ServiceId"]),
                                ServiceName =
                                    reader["ServiceName"].ToString(),
                                Category =
                                    reader["Category"].ToString(),
                                Description =
                                    reader["Description"] == DBNull.Value
                                    ? ""
                                    : reader["Description"].ToString(),
                                Price = Convert.ToDecimal(reader["Price"]),
                                DurationMinutes =
                                    Convert.ToInt32(reader["DurationMinutes"])
                            };
                        }
                    }
                }
            }

            if (service == null)
            {
                return HttpNotFound();
            }

            return View(service);
        }
    }
}
