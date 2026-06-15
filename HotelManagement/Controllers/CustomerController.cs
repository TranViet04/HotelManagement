using HotelManagement.Constants;
using HotelManagement.Services.Customer;
using HotelManagement.ViewModels.Customer;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace HotelManagement.Controllers
{
    [Authorize(Roles = UserRoles.Customer)]
    public class CustomerController : Controller
    {
        private readonly CustomerProfileService _customerProfileService;

        public CustomerController(CustomerProfileService customerProfileService)
        {
            _customerProfileService = customerProfileService;
        }

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!TryGetCurrentUserId(out var customerId))
            {
                return Challenge();
            }

            var model = await _customerProfileService.GetMyProfileAsync(customerId);

            if (model == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(CustomerProfileViewModel model)
        {
            if (!TryGetCurrentUserId(out var customerId))
            {
                return Challenge();
            }

            var currentProfile = await _customerProfileService.GetMyProfileAsync(customerId);

            if (currentProfile == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy hồ sơ khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            model.UserId = currentProfile.UserId;
            model.Email = currentProfile.Email;
            model.Role = currentProfile.Role;
            model.Status = currentProfile.Status;
            model.CreatedAt = currentProfile.CreatedAt;
            model.UpdatedAt = currentProfile.UpdatedAt;

            ModelState.Remove(nameof(CustomerProfileViewModel.UserId));
            ModelState.Remove(nameof(CustomerProfileViewModel.Email));
            ModelState.Remove(nameof(CustomerProfileViewModel.Role));
            ModelState.Remove(nameof(CustomerProfileViewModel.Status));
            ModelState.Remove(nameof(CustomerProfileViewModel.CreatedAt));
            ModelState.Remove(nameof(CustomerProfileViewModel.UpdatedAt));

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _customerProfileService.UpdateMyProfileAsync(model, customerId);

            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                return View(model);
            }

            var updatedProfile = await _customerProfileService.GetMyProfileAsync(customerId);

            if (updatedProfile != null)
            {
                await RefreshCustomerClaimsAsync(updatedProfile, customerId);
            }

            TempData["SuccessMessage"] = result.Message;

            return RedirectToAction(nameof(Profile));
        }

        private bool TryGetCurrentUserId(out long userId)
        {
            userId = 0;

            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);

            return long.TryParse(userIdValue, out userId);
        }

        private async Task RefreshCustomerClaimsAsync(CustomerProfileViewModel model, long customerId)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, customerId.ToString()),
                new Claim(ClaimTypes.Name, model.FullName),
                new Claim(ClaimTypes.Email, model.Email),
                new Claim(ClaimTypes.Role, UserRoles.Customer)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme
            );

            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal
            );
        }
    }
}
