using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Data.SqlClient;
using password.Models;
using System.Diagnostics;
using System.Net.Mail;
using System.Net;
using System.Reflection;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace password.Controllers
{
    public class HomeController : Controller
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
            using var connection = new SqlConnection(connectionString);

            var sql = "SELECT passwordPosts.*, passwordUsers.username, passwordUsers.profilepictureurl FROM passwordPosts LEFT JOIN passwordUsers ON passwordPosts.userId = passwordUsers.Id WHERE isPrivate = 0 ORDER BY passwordPosts.id DESC;";
            var posts = connection.Query<Post>(sql).ToList();

            ViewData["Username"] = HttpContext.Session.GetString("Username");
            ViewData["UserId"] = HttpContext.Session.GetString("UserId");

            return View(posts);
        }

        [HttpPost]
        [Route("addPost")]
        public IActionResult AddPost(Post model)
        {
            if (!ModelState.IsValid)
            {
                TempData["LoginCss"] = "alert-danger";
                TempData["Login"] = "Eksik form bilgisi.";
                return RedirectToAction("Index");
            }

            ViewData["Username"] = HttpContext.Session.GetString("Username");
            var userId = HttpContext.Session.GetString("UserId");

            if (!CheckLogin())
            {
                TempData["AuthError"] = "Gönderi gönderebilmek için bir hesaba giriş yapmalısın.";
                TempData["AuthErrorCss"] = "alert-danger";
                return RedirectToAction("Login", "Home");
            }

            using var connection = new SqlConnection(connectionString);
            var sql = "INSERT INTO passwordPosts (message, isPrivate, createdDate, userId) VALUES (@Message, @IsPrivate, @CreatedDate, @UserId)";

            var data = new
            {
                Message = model.Message,
                UserId = userId,
                isPrivate = model.isPrivate,
                CreatedDate = DateTime.Now,
            };

            var rowsAffected = connection.Execute(sql, data);

            TempData["Login"] = "Gönderi başarı ile eklendi!";
            TempData["LoginCss"] = "alert-success";
            return RedirectToAction("Index");
        }

        [Route("login")]
        public IActionResult Login()
        {
            if (CheckLogin())
            {
                TempData["Login"] = "Zaten bir hesaba giriş yapılmış.";
                TempData["LoginCss"] = "alert-success";
                return RedirectToAction("Index", "Home");
            }

            ViewData["Username"] = HttpContext.Session.GetString("Username");
            return View(new Register());
        }

        [Route("login")]
        [HttpPost]
        public IActionResult Login(User model)
        {
            var inputPasswordHash = Helper.Hash(model.Password);

            using var connection = new SqlConnection(connectionString);

            var sql = "SELECT Id,FullName,Username,Email,Password,RoleId,CreatedDate FROM passwordUsers WHERE Username = @Username AND Password = @Password";
            var user = connection.QuerySingleOrDefault<User>(sql, new { Username = model.Username, Password = inputPasswordHash });

            if (user != null)
            {
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("UserId", user.Id.ToString());

                TempData["Login"] = $"Giriş başarılı. Hoşgeldin {user.Username}";
                TempData["LoginCss"] = "alert-success";
                return RedirectToAction("Index");
            }
            else
            {
                TempData["AuthError"] = "Kullanıcı adı veya şifre hatalı.";
                TempData["AuthErrorCss"] = "alert-danger";
                return RedirectToAction("Login");
            }
        }

        [Route("register")]
        [HttpPost]
        public IActionResult Register(Register model)
        {
            if (!ModelState.IsValid)
            {
                TempData["AuthErrorCss"] = "alert-danger";
                TempData["AuthError"] = "Eksik form bilgisi.";
                return RedirectToAction("Login", model);
            }

            if (model.Password != model.PasswordCheck)
            {
                TempData["AuthErrorCss"] = "alert-danger";
                TempData["AuthError"] = "Sifre dogrulamasi hatali.";
                return View("Login", model);
            }

            model.Password = Helper.Hash(model.Password);

            using var connection = new SqlConnection(connectionString);
            var sql = "INSERT INTO passwordUsers (fullname, username, email, password, createdDate) VALUES (@FullName, @Username, @Email, @Password, @CreatedDate)";

            var data = new
            {
                Username = model.Username,
                FullName = model.FullName,
                Email = model.Email,
                Password = model.Password,
                CreatedDate = DateTime.Now,
            };

            var rowsAffected = connection.Execute(sql, data);

            var client = new SmtpClient("X", X)
            {
                Credentials = new NetworkCredential("X", "X"),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("X", "password - Hesabınız başarı ile oluşturuldu."),
                Subject = $"Password'e Hoşgeldin {model.FullName}!",
                Body = $@"
                        <!DOCTYPE html>
                        <html>
                        <head>
                            <meta charset='UTF-8'>
                            <title>Hoşgeldiniz!</title>
                            <style>
                                body {{
                                    font-family: Arial, sans-serif;
                                    line-height: 1.6;
                                }}
                                .bold {{
                                    font-weight: bold;
                                }}
                            </style>
                        </head>
                        <body>
                            <p><span class='bold'>Sevgili {model.FullName},</span></p>

                            <p>Password'e hoş geldiniz!</p>
                            <p>Üyeliğiniz başarıyla oluşturulmuştur ve artık sitemizin sunduğu tüm özellikleri kullanabilirsiniz.</p>
            
                            <p>Size özel bilgiler:</p>
                            <ul>
                                <li><strong>Kullanıcı Adı:</strong> {model.Username}</li>
                                <li><strong>Email Adresiniz:</strong> {model.Email}</li>
                            </ul>

                            <p>Herhangi bir sorun yaşarsanız, destek ekibimizle iletişime geçmekten çekinmeyin.</p>

                            <p>Teşekkür ederiz ve keyifli kullanımlar dileriz!</p>

                            <p><span class='bold'>Saygılarımızla,</span><br>
                            Password Ekibi</p>
                        </body>
                        </html>
                        ",
                IsBodyHtml = true,
            };

            mailMessage.To.Add(new MailAddress(model.Email, model.FullName));

            client.Send(mailMessage);

            TempData["AuthErrorCss"] = "alert-success";
            TempData["AuthError"] = "Kullanıcı kayıdı başarı ile oluşturuldu!";
            return RedirectToAction("Login");
        }

        [Route("logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("Username");

            TempData["AuthErrorCss"] = "alert-warning";
            TempData["AuthError"] = "Hesaptan çıkış yapıldı.";

            return RedirectToAction("Login");
        }

        [Route("/post/{userId}/{id?}")]
        public IActionResult Post(PostModel model, int id, int userId)
        {
            string userLoginId = HttpContext.Session.GetString("UserId");
            ViewData["Username"] = HttpContext.Session.GetString("Username");
            string idString = userId.ToString();

            ViewBag.isOwner = false;

            if (CheckLogin() && userLoginId == idString)
            {
                ViewBag.isOwner = true;
            }

            var postModel = new PostModel();

            using (var connection = new SqlConnection(connectionString))
            {
                var sql = "SELECT pp.*, pu.username, pu.profilepictureurl FROM passwordPosts pp LEFT JOIN passwordUsers pu ON pp.UserId = pu.Id WHERE pp.Id = @Id";
                var post = connection.QuerySingleOrDefault<Post>(sql, new { Id = id });
                postModel.Post = post;
            }

            using (var connection = new SqlConnection(connectionString))
            {
                var sql = "SELECT pc.*, pu.username, pu.profilepictureurl, pu.email FROM passwordComments pc LEFT JOIN passwordUsers pu ON pu.Id = pc.UserId LEFT JOIN passwordPosts pp ON pp.Id = pc.PostId WHERE pp.id = @Id ORDER BY createdDate DESC";
                var comments = connection.Query<Comments>(sql, new { Id = id }).ToList();
                postModel.Comments = comments;
            }

            return View(postModel);
        }

        [Route("profile/{username}/{userId?}")]
        public IActionResult Profile(int userId, string username)
        {
            ViewData["Username"] = HttpContext.Session.GetString("Username");
            string userLoginId = HttpContext.Session.GetString("UserId");
            string idString = userId.ToString();

            var profileModel = new Profile();

            string isPrivateCondition = "";

            if (CheckLogin() && userLoginId == idString)
            {
                isPrivateCondition = "AND (pp.isPrivate = 0 OR (pp.isPrivate = 1 AND pu.id = @UserLoginId))";
            }
            else
            {
                isPrivateCondition = "AND pp.isPrivate = 0";
            }

            using (var connection = new SqlConnection(connectionString))
            {
                var sql = $"SELECT pp.*, pu.username, pu.profilepictureUrl FROM passwordPosts pp LEFT JOIN passwordUsers pu ON pp.userId = pu.id WHERE pu.id = @userId {isPrivateCondition} ORDER BY createdDate DESC";
                var posts = connection.Query<Post>(sql, new { UserId = idString, Username = username, UserLoginId = userLoginId }).ToList();
                profileModel.Posts = posts;
            }

            using (var connection = new SqlConnection(connectionString))
            {
                var sql = "SELECT * FROM passwordUsers WHERE id = @id;";
                var user = connection.QuerySingleOrDefault<User>(sql, new { id = userId });
                profileModel.User = user;
            }

            return View(profileModel);
        }

        [Route("editProfile/{id}")]
        [HttpGet]
        public IActionResult EditProfile(User model)
        {
            if (!CheckLogin())
            {
                TempData["AuthError"] = "Hesap detaylarını görebilmek için bir hesaba giriş yapmalısın.";
                TempData["AuthErrorCss"] = "alert-danger";
                return RedirectToAction("Login", "Home");
            }

            ViewData["Username"] = HttpContext.Session.GetString("Username");

            string userId = HttpContext.Session.GetString("UserId");
            string idString = model.Id.ToString();

            if (userId != idString)
            {
                TempData["Login"] = "Bu hesabın bilgilerini değiştirmek için doğru hesaba giriş yapmalısın!";
                TempData["LoginCss"] = "alert-danger";
                return RedirectToAction("Index");
            }

            using var connection = new SqlConnection(connectionString);
            var user = connection.QuerySingleOrDefault<User>("SELECT * FROM passwordUsers WHERE id = @Id", new { Id = model.Id });

            return View(user);
        }

        [Route("addProfilePicture/{id}")]
        [HttpPost]
        public IActionResult AddProfilePicture(User model)
        {
            string userId = HttpContext.Session.GetString("UserId");
            string idString = model.Id.ToString();

            if (userId == idString)
            {
                var imageName = Guid.NewGuid().ToString() + Path.GetExtension(model.ProfilePicture.FileName);
                var path = Path.Combine(/*Directory.GetCurrentDirectory(), */"wwwroot", "uploads", imageName);

                using var stream = new FileStream(path, FileMode.Create);
                model.ProfilePicture.CopyTo(stream);

                ViewBag.Image = $"/uploads/{imageName}";
                string imageUrl = ViewBag.Image;

                using var connection = new SqlConnection(connectionString);
                var sql = "UPDATE passwordUsers SET profilePictureUrl = @ProfilePictureUrl WHERE id = @Id;";

                var data = new
                {
                    ProfilePictureUrl = imageUrl,
                    Id = model.Id,
                };

                var rowsAffected = connection.Execute(sql, data);

                TempData["Login"] = "Fotograf basari ile eklendi!";
                TempData["LoginCss"] = "alert-success";
                return RedirectToAction("Index");
            }

            TempData["Login"] = "Bu hesabin fotografini degistirmek icin o hesaba giris yapmalisin!";
            TempData["LoginCss"] = "alert-danger";
            return RedirectToAction("Index");
        }

        [Route("changePassword/{id}")]
        [HttpPost]
        public IActionResult ChangePassword(User model)
        {
            string userId = HttpContext.Session.GetString("UserId");
            string idString = model.Id.ToString();

            if (userId == idString)
            {
                var inputPasswordHash = Helper.Hash(model.Password);

                using var connection = new SqlConnection(connectionString);
                var sql = "UPDATE passwordUsers SET Password = @Password WHERE id = @Id;";

                var data = new
                {
                    Password = inputPasswordHash,
                    Id = model.Id,
                };

                var rowsAffected = connection.Execute(sql, data);

                TempData["Login"] = "Şifre başarı ile değiştirildi!";
                TempData["LoginCss"] = "alert-success";
                return RedirectToAction("Index");
            }

            TempData["Login"] = "Hatalı kullanıcı bilgisi!";
            TempData["LoginCss"] = "alert-danger";
            return RedirectToAction("Index");
        }

        [Route("changeFullName/{id}")]
        [HttpPost]
        public IActionResult ChangeFullName(User model)
        {
            string userId = HttpContext.Session.GetString("UserId");
            string idString = model.Id.ToString();

            if (userId == idString)
            {
                using var connection = new SqlConnection(connectionString);
                var sql = "UPDATE passwordUsers SET Fullname = @Fullname WHERE id = @Id;";

                var data = new
                {
                    Fullname = model.FullName,
                    Id = model.Id,
                };

                var rowsAffected = connection.Execute(sql, data);

                TempData["Login"] = "İsim başarı ile değiştirildi!";
                TempData["LoginCss"] = "alert-success";
                return RedirectToAction("Index");
            }

            TempData["Login"] = "Hatalı kullanıcı bilgisi!";
            TempData["LoginCss"] = "alert-danger";
            return RedirectToAction("Index");
        }

        [Route("/token")]
        [HttpGet]
        public IActionResult Token()
        {
            return View();
        }

        [HttpPost]
        [Route("/token/{token?}")]
        public IActionResult Token(string token)
        {
            using var connection = new SqlConnection(connectionString);

            var sql = "SELECT * FROM passwordResetTokens WHERE Token = @token;";
            var user = connection.QuerySingleOrDefault<User>(sql, new { Token = token });

            return View("Token", user);
        }

        [HttpPost]
        [Route("/addcomment")]
        public IActionResult AddComment(Comments model, int postId)
        {
            if (!CheckLogin())
            {
                TempData["AuthError"] = "Yorum yapabilmek için bir hesaba giriş yapmalısın.";
                TempData["AuthErrorCss"] = "alert-danger";
                return RedirectToAction("Login", "Home");
            }

            string userId = HttpContext.Session.GetString("UserId");
            ViewData["Username"] = HttpContext.Session.GetString("Username");

            using var connection = new SqlConnection(connectionString);
            var sql = "INSERT INTO passwordComments (message, userId, postId, createddate) VALUES (@Message, @UserId, @PostId, @CreatedDate)";

            var data = new
            {
                Message = model.Message,
                UserId = userId,
                PostId = postId,
                CreatedDate = DateTime.Now,
            };

            var rowsAffected = connection.Execute(sql, data);

            TempData["Login"] = "Yorum başarı ile eklendi!";
            TempData["LoginCss"] = "alert-success";
            return RedirectToAction("Index");
        }

        [Route("/deleteComment/{id?}")]
        public IActionResult DeleteComment(int id)
        {
            ViewData["Username"] = HttpContext.Session.GetString("Username");

            if (!CheckLogin())
            {
                TempData["AuthError"] = "Yorum silebilmek için bir hesaba giriş yapmalısın.";
                TempData["AuthErrorCss"] = "alert-danger";
                return RedirectToAction("Login");
            }

            using var connection = new SqlConnection(connectionString);
            var sql = "DELETE FROM passwordComments WHERE Id = @Id";

            var rowsAffected = connection.Execute(sql, new { Id = id });

            TempData["Login"] = "Yorum başarı ile silindi!";
            TempData["LoginCss"] = "alert-success";
            return RedirectToAction("Index");
        }

        [Route("/resetPassword")]
        [HttpPost]
        public IActionResult ResetPassword(string email)
        {
            var token = Guid.NewGuid().ToString();

            var client = new SmtpClient("X", X)
            {
                Credentials = new NetworkCredential("X", "X"),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("postmaster@mesaj.senihay.com", "password - Şifre sıfırlama isteği."),
                Subject = $"Şifre sıfırlama",
                Body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <title>Hoşgeldiniz!</title>
                <style>
                    body {{
                        font-family: Arial, sans-serif;
                        line-height: 1.6;
                    }}
                    .bold {{
                        font-weight: bold;
                    }}
                </style>
            </head>
            <body>
                <p><span class='bold'>Token : {token}</span></p>
            </body>
            </html>
            ",
                IsBodyHtml = true,
            };

            mailMessage.To.Add(new MailAddress(email, email));

            client.Send(mailMessage);

            using var connection = new SqlConnection(connectionString);
            var sql = "INSERT INTO passwordResetTokens (userId, token, createddate, used) VALUES (@UserId, @Token, @createdDate, @Used)";

            var data = new
            {
                Token = token,
                UserId = 7,
                Used = false,
                CreatedDate = DateTime.Now,
            };

            var rowsAffected = connection.Execute(sql, data);

            return View("Token");
        }
    }
}