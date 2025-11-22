// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Elysia.Models; // Đảm bảo using Model của bạn
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace Elysia.Areas.Identity.Pages.Account
{
    public class RegisterModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserStore<ApplicationUser> _userStore;
        private readonly IUserEmailStore<ApplicationUser> _emailStore;
        private readonly ILogger<RegisterModel> _logger;
        private readonly IEmailSender _emailSender;

        // Thêm RoleManager để kiểm tra Role có tồn tại không (tùy chọn nhưng an toàn)
        private readonly RoleManager<IdentityRole> _roleManager;

        public RegisterModel(
            UserManager<ApplicationUser> userManager,
            IUserStore<ApplicationUser> userStore,
            SignInManager<ApplicationUser> signInManager,
            ILogger<RegisterModel> logger,
            IEmailSender emailSender,
            RoleManager<IdentityRole> roleManager) // Inject RoleManager
        {
            _userManager = userManager;
            _userStore = userStore;
            _emailStore = GetEmailStore();
            _signInManager = signInManager;
            _logger = logger;
            _emailSender = emailSender;
            _roleManager = roleManager;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public string ReturnUrl { get; set; }

        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Vui lòng nhập Email")]
            [EmailAddress]
            [Display(Name = "Email")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Vui lòng nhập Mật khẩu")]
            [StringLength(100, ErrorMessage = "{0} phải dài ít nhất {2} và tối đa {1} ký tự.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Mật khẩu")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Xác nhận mật khẩu")]
            [Compare("Password", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
            public string ConfirmPassword { get; set; }

            // --- BỔ SUNG & VALIDATION CHO FULLNAME ---
            [Required(ErrorMessage = "Vui lòng nhập Họ và tên")]
            [StringLength(100)]
            [Display(Name = "Họ và tên")]
            public string FullName { get; set; }

            // --- BỔ SUNG & VALIDATION CHO ROLE ---
            [Required(ErrorMessage = "Vui lòng chọn Loại tài khoản")]
            [Display(Name = "Bạn là?")]
            public string Role { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            ReturnUrl = returnUrl;
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");
            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var user = CreateUser();

                await _userStore.SetUserNameAsync(user, Input.Email, CancellationToken.None);
                await _emailStore.SetEmailAsync(user, Input.Email, CancellationToken.None);

                // 1. Lưu Họ tên
                user.FullName = Input.FullName;

                // 2. Tạo User (Mặc định EmailConfirmed = false do cấu hình trong Program.cs)
                var result = await _userManager.CreateAsync(user, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    // 3. Gán Role
                    if (!await _roleManager.RoleExistsAsync(Input.Role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(Input.Role));
                    }
                    await _userManager.AddToRoleAsync(user, Input.Role);

                    // === LOGIC PHÂN QUYỀN QUAN TRỌNG ===
                    if (Input.Role == "GiangVien")
                    {
                        // A. Đối với GIẢNG VIÊN:
                        // - Không set EmailConfirmed = true (để nguyên là false).
                        // - KHÔNG gọi SignInAsync (không cho đăng nhập).
                        // - Redirect về trang Login kèm thông báo.

                        return RedirectToPage("./Login", new
                        {
                            ErrorMessage = "Đăng ký thành công! Tài khoản Giảng viên cần Admin phê duyệt trước khi đăng nhập."
                        });
                    }
                    else
                    {
                        // B. Đối với SINH VIÊN:
                        // - Tự động kích hoạt tài khoản (để họ vào học luôn).
                        user.EmailConfirmed = true;
                        await _userManager.UpdateAsync(user);

                        // - Đăng nhập ngay lập tức
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        return RedirectToAction("Index", "Courses");
                    }
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            return Page();
        }

        private ApplicationUser CreateUser()
        {
            try
            {
                return Activator.CreateInstance<ApplicationUser>();
            }
            catch
            {
                throw new InvalidOperationException($"Can't create an instance of '{nameof(ApplicationUser)}'. " +
                    $"Ensure that '{nameof(ApplicationUser)}' is not an abstract class and has a parameterless constructor, or alternatively " +
                    $"override the register page in /Areas/Identity/Pages/Account/Register.cshtml");
            }
        }

        private IUserEmailStore<ApplicationUser> GetEmailStore()
        {
            if (!_userManager.SupportsUserEmail)
            {
                throw new NotSupportedException("The default UI requires a user store with email support.");
            }
            return (IUserEmailStore<ApplicationUser>)_userStore;
        }
    }
}