using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using POE_Library;
using PROG6212_ST10043367_POEPart1.Classes;
using System.Data.SqlClient;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using PROG6212_ST10043367_FinalPOE.Models;
using System.Globalization;


namespace PROG6212_ST10043367_FinalPOE.Controllers
{
    public class UsersController : Controller
    {

        private int loggedinUserID;


        private Semester NewSemestr = new Semester();

        private int UserID;

        public UsersController()
        {
            UserID = GetLoggedInUserID(); 
        }

        public IActionResult ViewModules()
        {
            List<Module> modules = NewSemestr.GetModulesFromDatabase(UserID);

            var output = modules.Select(m => $"{m.ToString()}").ToList();

            ViewBag.Modules = output;

            return View();
        }

        private int GetLoggedInUserID()
        {
            return 1; 
        }



        [HttpPost]
        public JsonResult DeductStudyHours(string moduleCode, double hoursStudied)
        {
            try
            {
                UpdateSelfStudyHoursInDatabase(moduleCode, hoursStudied);

                return Json(new { success = true, message = "Study hours deducted successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error deducting study hours: " + ex.Message });
            }
        }

        private void UpdateSelfStudyHoursInDatabase(string moduleCode, double hoursStudied)
        {
            using (SqlConnection conn = Connections.GetConnection())
            {
                conn.Open();

                string updateQuery = "UPDATE Modules SET SelfStudyHoursPerWeek = SelfStudyHoursPerWeek - @HoursStudied WHERE module_code = @ModuleCode";

                using (SqlCommand cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@ModuleCode", moduleCode);
                    cmd.Parameters.AddWithValue("@HoursStudied", hoursStudied);

                    cmd.ExecuteNonQuery();
                }
            }
        }


        public ActionResult Login() => View();



        public IActionResult Home()
        {

            var moduleData = GetModuleChartData(loggedinUserID);
            ViewBag.ModuleChartData = moduleData;


            return View();
        }


        private ModuleChartData GetModuleChartData(int userId)
        {
            var labels = new List<string> { "Week 1", "Week 2", "Week 3", "Week 4" };
            var hoursSpent = new List<int> { 10, 12, 8, 14 };
            var idealHours = new List<int> { 10, 10, 10, 10 };

            return new ModuleChartData
            {
                Labels = labels,
                HoursSpent = hoursSpent,
                IdealHours = idealHours
            };
        }



        public IActionResult LogStudyHours()
        {
            List<Module> modules = NewSemestr.GetModulesFromDatabase(UserID);

            ViewBag.Modules = modules;

            return View();
        }


        [HttpPost]
        public async Task<IActionResult> LogStudyHours(string moduleCode, double hoursStudied, string studyDate)
        {
            try
            {
                bool isValidModule = await CheckModuleBelongsToUserAsync(moduleCode);

                if (isValidModule)
                {
                    double currentHours = await GetCurrentSelfStudyHoursAsync(moduleCode);

                    if (currentHours >= hoursStudied)
                    {
                        if (DateTime.TryParseExact(studyDate, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                        {
                            if (IsWithinCurrentWeek(parsedDate))
                            {
                                double newHours = currentHours - hoursStudied;
                                await UpdateSelfStudyHoursAsync(moduleCode, newHours);

                                TempData["SuccessMessage"] = "Study hours logged successfully!";
                            }
                            else
                            {
                                TempData["ErrorMessage"] = "Study session must be within the current week.";
                            }
                        }
                        else
                        {
                            TempData["ErrorMessage"] = "Invalid date format. Please use dd/MM/yyyy.";
                        }
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Not enough self-study hours available for this module.";
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "Invalid module code. Please try again.";
                }

                List<Module> modules = NewSemestr.GetModulesFromDatabase(UserID);
                ViewBag.Modules = modules;

                return RedirectToAction("LogStudyHours");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error logging study hours: {ex.Message}");
                TempData["ErrorMessage"] = "Error logging study hours.";
                return RedirectToAction("LogStudyHours");
            }
        }








        private bool IsWithinCurrentWeek(DateTime studyDate)
        {
            // Compare the study date with the current week's start and end dates
            DateTime currentDate = DateTime.Now.Date;
            DateTime startOfWeek = currentDate.AddDays(-(int)currentDate.DayOfWeek);
            DateTime endOfWeek = startOfWeek.AddDays(6);

            return studyDate >= startOfWeek && studyDate <= endOfWeek;
        }

        // Helper method to check if a module belongs to the logged-in user
        private async Task<bool> CheckModuleBelongsToUserAsync(string moduleCode)
        {
            using (SqlConnection connection = Connections.GetConnection())
            {
                await connection.OpenAsync();

                string checkModuleQuery = "SELECT COUNT(*) FROM Modules WHERE Module_Code = @ModuleCode AND UserID = @UserID";

                using (SqlCommand cmd = new SqlCommand(checkModuleQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@ModuleCode", moduleCode);
                    cmd.Parameters.AddWithValue("@UserID", loggedinUserID);

                    int count = Convert.ToInt32(await cmd.ExecuteScalarAsync());

                    return count > 0;
                }
            }
        }

        private async Task<double> GetCurrentSelfStudyHoursAsync(string moduleCode)
        {
            using (SqlConnection connection = Connections.GetConnection())
            {
                await connection.OpenAsync();

                // Query to get current SelfStudyHoursPerWeek
                string getHoursQuery = "SELECT SelfStudyHoursPerWeek FROM Modules WHERE Module_Code = @ModuleCode AND UserID = @UserID";

                using (SqlCommand getCmd = new SqlCommand(getHoursQuery, connection))
                {
                    getCmd.Parameters.AddWithValue("@ModuleCode", moduleCode);
                    getCmd.Parameters.AddWithValue("@UserID", loggedinUserID);

                    return Convert.ToDouble(await getCmd.ExecuteScalarAsync());
                }
            }
        }

        // Helper method to update self study hours
        private async Task UpdateSelfStudyHoursAsync(string moduleCode, double hoursStudied)
        {
            using (SqlConnection connection = Connections.GetConnection())
            {
                await connection.OpenAsync();

                string updateQuery = "UPDATE Modules SET SelfStudyHoursPerWeek = @NewHours WHERE Module_Code = @ModuleCode AND UserID = @UserID";

                using (SqlCommand updateCmd = new SqlCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@NewHours", hoursStudied); 
                    updateCmd.Parameters.AddWithValue("@ModuleCode", moduleCode);
                    updateCmd.Parameters.AddWithValue("@UserID", loggedinUserID);

                    await updateCmd.ExecuteNonQueryAsync();
                }
            }
        }


        public ActionResult Register() => View();


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(IFormCollection col)
        {
            try
            {
                string username = col["txtUserName"];
                string password = col["txtPassword"];
                string confirmPassword = col["txtConfirmPassword"];
                string firstName = col["txtFirstName"];
                string surname = col["txtSurname"];

                // Ensure that the passwords match; otherwise, return an error
                if (password != confirmPassword)
                {
                    TempData["ErrorMessage"] = "Passwords do not match. Please try again.";
                    return RedirectToAction("Register");
                }

                string hashedPassword = HashPassword(password);

                string insertQuery = "INSERT INTO Users (Username, HashPassword, FirstName, Surname) " +
                                     "OUTPUT INSERTED.UserID VALUES (@Username, @HashPassword, @FirstName, @Surname)";

                using (SqlConnection connection = Connections.GetConnection())
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@Username", username);
                        cmd.Parameters.AddWithValue("@HashPassword", hashedPassword);
                        cmd.Parameters.AddWithValue("@FirstName", firstName);
                        cmd.Parameters.AddWithValue("@Surname", surname);

                        // Get the generated UserID after the insert
                        loggedinUserID = (int)cmd.ExecuteScalar();

                        TempData["SuccessMessage"] = "Thank you for registering!";
                        return RedirectToAction("Home");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                TempData["ErrorMessage"] = "Error during registration.";
                return RedirectToAction("Register");
            }
        }

        [HttpGet]
        public IActionResult AddModule()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddModule([Bind("Code,Name,Credits,HoursPerWeek")] Module module)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    int userId = GetLoggedInUserID();

                    // Retrieve the current semester ID
                    int semesterId = await GetCurrentSemesterIdAsync(userId); 

                    await SaveModuleToDatabaseAsync(module, userId, semesterId); 

                    TempData["SuccessMessage"] = "Module details have been saved! You may add another module.";
                    return RedirectToAction("AddModule");
                }

                return View(module); 
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during module addition: {ex.Message}");
                TempData["ErrorMessage"] = "Error during module addition.";
                return View(module);
            }
        }

        private async Task<int> GetCurrentSemesterIdAsync(int userId)
        {
            using (SqlConnection connection = Connections.GetConnection())
            {
                await connection.OpenAsync();
                string query = "SELECT TOP 1 SemesterId FROM Semesters WHERE UserID = @UserID ORDER BY StartDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    return (int)await cmd.ExecuteScalarAsync();
                }
            }
        }


        private double CalculateSelfStudyHours(double currentHours, double hoursStudied)
        {
            // Calculate new self study hours
            double newHours = currentHours - hoursStudied;

            return newHours;
        }

        private async Task SaveModuleToDatabaseAsync(Module module, int userId, int semesterId)
        {
            string insertQuery = "INSERT INTO Modules (UserID, SemesterID, Module_Code, Module_Name, Number_of_credits, Class_hours_per_week, SelfStudyHoursPerWeek) " +
                "VALUES (@UserID, @SemesterID, @Module_Code, @Module_Name, @Number_of_credits, @Class_hours_per_week, @SelfStudyHoursPerWeek)";

            try
            {
                using (SqlConnection connection = Connections.GetConnection())
                {
                    await connection.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                    {
                        double selfStudyHours = CalculateSelfStudyHours(module.Credits, module.HoursPerWeek);

                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddWithValue("@SemesterID", semesterId); 
                        cmd.Parameters.AddWithValue("@Module_Code", module.Code);
                        cmd.Parameters.AddWithValue("@Module_Name", module.Name);
                        cmd.Parameters.AddWithValue("@Number_of_credits", module.Credits);
                        cmd.Parameters.AddWithValue("@Class_hours_per_week", module.HoursPerWeek);
                        cmd.Parameters.AddWithValue("@SelfStudyHoursPerWeek", selfStudyHours);

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving module to database: {ex.Message}");
                TempData["ErrorMessage"] = "SQL Error saving module to the database.";
                RedirectToAction("AddModule");
            }
        }


        private int GetCurrentSemesterId(int userId)
        {
            using (SqlConnection connection = Connections.GetConnection())
            {
                connection.Open();
                string query = "SELECT TOP 1 SemesterId FROM Semesters WHERE UserID = @UserID ORDER BY StartDate DESC";

                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@UserID", userId);
                    return (int)cmd.ExecuteScalar();
                }
            }
        }


        private void SaveModuleToDatabase(Module module, int userId, int semesterId)
        {
            string insertQuery = "INSERT INTO Modules (UserID, SemesterID, Module_Code, Module_Name, Number_of_credits, Class_hours_per_week, SelfStudyHoursPerWeek) " +
                "VALUES (@UserID, @SemesterID, @Module_Code, @Module_Name, @Number_of_credits, @Class_hours_per_week, @SelfStudyHoursPerWeek)";

            try
            {
                using (SqlConnection connection = Connections.GetConnection())
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@UserID", userId);
                        cmd.Parameters.AddWithValue("@SemesterID", semesterId);
                        cmd.Parameters.AddWithValue("@Module_Code", module.Code);
                        cmd.Parameters.AddWithValue("@Module_Name", module.Name);
                        cmd.Parameters.AddWithValue("@Number_of_credits", module.Credits);
                        cmd.Parameters.AddWithValue("@Class_hours_per_week", module.HoursPerWeek);
                        cmd.Parameters.AddWithValue("@SelfStudyHoursPerWeek", module.SelfStudyHours);

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving module to database: {ex.Message}");
                TempData["ErrorMessage"] = "SQL Error saving module to the database.";
                RedirectToAction("AddModule");
            }
        }


        [HttpGet]
        public IActionResult AddSemester()
        {
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddSemester(Semester semester)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    int userId = UserID;

                    semester.SemesterId = SaveSemesterToDatabase(semester, userId);

                    if (semester.SemesterId > 0)
                    {
                        // Display a success message and disable the "Add Semester" button
                        TempData["SuccessMessage"] = "Semester details have been saved! You may proceed to add your modules.";
                        return RedirectToAction("AddModule");
                    }
                    else
                    {
                        // Display an error message if saving fails
                        TempData["ErrorMessage"] = "Error saving semester details.";
                    }
                }

                return View(semester);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error during semester addition.";
                return RedirectToAction("AddSemester");
            }
        }


        private int SaveSemesterToDatabase(Semester semester, int userId)
        {
            try
            {
                string insertQuery = "INSERT INTO Semesters (StartDate, SemesterWeeks, UserID) " +
                    "OUTPUT INSERTED.SemesterId VALUES (@StartDate, @SemesterWeeks, @UserID)";

                using (SqlConnection connection = Connections.GetConnection())
                {
                    connection.Open();
                    using (SqlCommand cmd = new SqlCommand(insertQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@StartDate", semester.StartDate);
                        cmd.Parameters.AddWithValue("@SemesterWeeks", semester.Weeks);
                        cmd.Parameters.AddWithValue("@UserID", userId);

                        return (int)cmd.ExecuteScalar();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during semester addition: {ex.Message}");
                TempData["ErrorMessage"] = "Error during semester addition.";
                return 0; 
            }
        }



        // GET: UsersController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: UsersController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UsersController/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: UsersController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: UsersController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: UsersController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(IFormCollection col)
        {
            string username = col["txtUsername"];
            string password = col["txtPassword"];

            string hashedPassword = HashPassword(password);

            string selectQuery = "SELECT UserID, FirstName, Surname, HashPassword FROM Users WHERE Username = @Username";

            using (SqlConnection connection = Connections.GetConnection())
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand(selectQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@Username", username);

                    using (SqlDataReader reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string storedPassword = reader["HashPassword"].ToString();
                            loggedinUserID = (int)reader["UserID"];

                            if (VerifyPassword(password, storedPassword))
                            {
                                string firstName = reader["FirstName"].ToString();
                                string surname = reader["Surname"].ToString();


                                return RedirectToAction("Home");
                            }
                            else
                            {
                                ViewData["ErrorMessage"] = "Invalid username or password";
                                return View("Login");
                            }
                        }
                        else
                        {
                            ViewData["ErrorMessage"] = "Invalid username or password";
                            return View("Login");


                        }
                    }
                }
            }
        }


        private string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < hashedBytes.Length; i++)
                {
                    builder.Append(hashedBytes[i].ToString("x2"));
                }

                return builder.ToString();
            }
        }

        //Verifying that the password is the same
        private bool VerifyPassword(string inputPassword, string storedHashedPassword)
        {
            string hashedInputPassword = HashPassword(inputPassword);
            return hashedInputPassword.Equals(storedHashedPassword, StringComparison.OrdinalIgnoreCase);
        }
    }
}