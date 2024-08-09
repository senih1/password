using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using password.Models;

namespace password.Controllers
{
    public class AdminController : Controller
    {
        string connectionString = "Server=X;Initial Catalog=X;User Id =X; Password =X;TrustServerCertificate=X";
        public bool CheckLogin()
        {
            if (HttpContext.Session.GetString("Username") == null)
            {
                return false;
            }
            return true;
        }
        public IActionResult Index()
        {
            return View();
        }

        [Route("users")]
        public IActionResult Users()
        {
            ViewData["Username"] = HttpContext.Session.GetString("Username");
            ViewData["UserId"] = HttpContext.Session.GetString("UserId");

            if (!CheckLogin())
            {
                TempData["AuthError"] = "Bu işlemi gerçekleştirmek için bir hesaba giriş yapmalısın.";
                TempData["AuthErrorCss"] = "alert-warning";
                return RedirectToAction("Login", "Home");
            }

            using var connection = new SqlConnection(connectionString);
            var sql = "SELECT * FROM passwordUsers";

            var user = connection.Query<User>(sql);

            return View(user);
        }

        public IActionResult DeleteUser(int id)
        {
            if (!CheckLogin())
            {
                TempData["AuthError"] = "Bu işlemi gerçekleştirmek için bir hesaba giriş yapmalısın.";
                TempData["AuthErrorCss"] = "alert-warning";
                return RedirectToAction("Login", "Home");
            }

            using var connection = new SqlConnection(connectionString);
            var sql = "DELETE FROM psaswordUsers WHERE id = @Id";

            var rowsAffected = connection.Execute(sql, new { Id = id });

            TempData["Message"] = "Kullanıcı başarılı bir şekilde silindi.";
            TempData["MessageCss"] = "alert-success";
            return RedirectToAction("Users");
        }
    }
}


