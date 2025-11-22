#nullable disable
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Elysia.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace Elysia.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<ApplicationUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }
        [BindProperty]
        public InputModel Input { get; set; }
        public IList<AuthenticationScheme> ExternalLogins { get; set; }
        public string ReturnUrl { get; set; }
        [TempData]
        public string ErrorMessage { get; set; }
        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập Email")]
            [EmailAddress]
            public string Email { get; set; }
            [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
            [DataType(DataType.Password)]
            public string Password { get; set; }
            [Display(Name = "Ghi nhớ tôi?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }
            returnUrl ??= Url.Content("~/");
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        if (await _signInManager.UserManager.IsInRoleAsync(user, "Admin"))
                        {
                            return Redirect("/Admin");
                        }
                        else if (await _signInManager.UserManager.IsInRoleAsync(user, "GiangVien"))
                        {
                            return Redirect("/Instructor");
                        }
                        else if (await _signInManager.UserManager.IsInRoleAsync(user, "SinhVien"))
                        {
                            return Redirect("/Courses");
                        }
                    }
                    return LocalRedirect(returnUrl);
                }

                if (result.IsNotAllowed)
                {
                    var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
                    if (user != null &&
                        await _signInManager.UserManager.CheckPasswordAsync(user, Input.Password) &&
                        await _signInManager.UserManager.IsInRoleAsync(user, "GiangVien") &&
                        !user.EmailConfirmed)
                    {
                        ModelState.AddModelError(string.Empty, "Tài khoản Giảng viên của bạn đang chờ Admin phê duyệt hoặc đã bị hủy quyền truy cập.");
                        return Page();
                    }
                }

                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Đăng nhập thất bại. Vui lòng kiểm tra lại Email và Mật khẩu.");
                    return Page();
                }
            }
            return Page();
        }
    }
}