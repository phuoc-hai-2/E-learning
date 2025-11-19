// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
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

        /// <summary>
        ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///       directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [BindProperty]
        public InputModel Input { get; set; }

        /// <summary>
        ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///       directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        /// <summary>
        ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///       directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public string ReturnUrl { get; set; }

        /// <summary>
        ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///       directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///       directly from your code. This API may change or be removed in future releases.
        /// </summary>
        public class InputModel
        {
            /// <summary>
            ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///       directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Vui lòng nhập Email")]
            [EmailAddress]
            public string Email { get; set; }

            /// <summary>
            ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///       directly from your code. This API may change or be removed in future releases.
            /// </summary>
            [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            /// <summary>
            ///       This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
            ///       directly from your code. This API may change or be removed in future releases.
            /// </summary>
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

            // Clear the existing external cookie to ensure a clean login process
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
                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");

                    // =========================================================================
                    // 🎯 LOGIC CHUYỂN HƯỚNG DỰA TRÊN VAI TRÒ (ROLE-BASED REDIRECTION)
                    // =========================================================================

                    // 1. Lấy thông tin user vừa đăng nhập
                    var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);

                    if (user != null)
                    {
                        // 2. Kiểm tra vai trò và chuyển hướng
                        if (await _signInManager.UserManager.IsInRoleAsync(user, "Admin"))
                        {
                            // Chuyển hướng đến Dashboard Admin
                            return Redirect("/Admin");
                        }
                        else if (await _signInManager.UserManager.IsInRoleAsync(user, "GiangVien"))
                        {
                            // Chuyển hướng đến Dashboard Giảng viên
                            return Redirect("/Instructor");
                        }
                        else if (await _signInManager.UserManager.IsInRoleAsync(user, "SinhVien"))
                        {
                            // Chuyển hướng đến Dashboard Sinh viên (Courses)
                            return Redirect("/Courses");
                        }
                    }

                    // 3. Fallback: Nếu không có vai trò đặc biệt (hoặc không tìm thấy user), sử dụng URL trả về mặc định
                    return LocalRedirect(returnUrl);

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

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}