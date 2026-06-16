using HotelManagement.Constants;
using HotelManagement.Data;
using HotelManagement.Models;
using HotelManagement.Services;
using HotelManagement.ViewModels.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HotelManagement.Controllers
{
    public class AuthController : Controller
    {
        private readonly AuthService _authService;
        private readonly HotelDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(
            AuthService authService,
            HotelDbContext context,
            IConfiguration configuration)
        {
            _authService = authService;
            _context = context;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectByRole();
            }

            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var user = await _authService.ValidateLoginAsync(model.Email, model.Password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            await SignInUserAsync(user, model.RememberMe);

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectByRole(user.Role);
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectByRole();
            }

            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var emailExists = await _authService.EmailExistsAsync(model.Email);

            if (emailExists)
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã được sử dụng");
                return View(model);
            }

            await _authService.RegisterCustomerAsync(model);

            TempData["SuccessMessage"] = "Đăng ký thành công. Vui lòng đăng nhập.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            if (!IsGoogleLoginConfigured())
            {
                TempData["Error"] = "Đăng nhập Google chưa được cấu hình.";
                return RedirectToAction(nameof(Login), new { returnUrl });
            }

            var redirectUrl = Url.Action(
                nameof(GoogleCallback),
                "Auth",
                new { returnUrl });

            var properties = new AuthenticationProperties
            {
                RedirectUri = redirectUrl
            };

            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpGet]
        public async Task<IActionResult> GoogleCallback(string? returnUrl = null)
        {
            var externalResult = await HttpContext.AuthenticateAsync("External");

            if (!externalResult.Succeeded || externalResult.Principal == null)
            {
                TempData["Error"] = "Đăng nhập Google thất bại.";
                return RedirectToAction(nameof(Login));
            }

            var email = externalResult.Principal.FindFirst(ClaimTypes.Email)?.Value?.Trim().ToLower();
            var fullName = externalResult.Principal.FindFirst(ClaimTypes.Name)?.Value;

            if (string.IsNullOrWhiteSpace(email))
            {
                TempData["Error"] = "Không lấy được email từ Google.";
                await HttpContext.SignOutAsync("External");
                return RedirectToAction(nameof(Login));
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            if (user == null)
            {
                user = new User
                {
                    FullName = string.IsNullOrWhiteSpace(fullName) ? email : fullName,
                    Email = email,
                    PhoneNumber = null,
                    PasswordHash = "EXTERNAL_LOGIN_ONLY",
                    Role = UserRoles.Customer,
                    Status = UserStatuses.Active,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();
            }
            else
            {
                if (user.Status != UserStatuses.Active)
                {
                    TempData["Error"] = "Tài khoản của bạn đã bị khóa hoặc ngưng hoạt động.";
                    await HttpContext.SignOutAsync("External");
                    return RedirectToAction(nameof(Login));
                }

                if (user.Role != UserRoles.Customer)
                {
                    TempData["Error"] = "Tài khoản nhân viên không được đăng nhập bằng Google.";
                    await HttpContext.SignOutAsync("External");
                    return RedirectToAction(nameof(Login));
                }
            }

            await HttpContext.SignOutAsync("External");
            await SignInUserAsync(user);

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction("Index", "Home");
        }

        private bool IsGoogleLoginConfigured()
        {
            return !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientId"])
                && !string.IsNullOrWhiteSpace(_configuration["Authentication:Google:ClientSecret"]);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private async Task SignInUserAsync(User user, bool rememberMe = false)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = rememberMe
                    ? DateTimeOffset.UtcNow.AddDays(7)
                    : DateTimeOffset.UtcNow.AddHours(8)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);
        }

        private IActionResult RedirectByRole(string? role = null)
        {
            role ??= User.FindFirstValue(ClaimTypes.Role);

            return role switch
            {
                UserRoles.Admin => RedirectToAction("Dashboard", "Admin"),
                UserRoles.Receptionist => RedirectToAction("Dashboard", "Receptionist"),
                UserRoles.Customer => RedirectToAction("Index", "Home"),
                _ => RedirectToAction("Index", "Home")
            };
        }
    }
}
