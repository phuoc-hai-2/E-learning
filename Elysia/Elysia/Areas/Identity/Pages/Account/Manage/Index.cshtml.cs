// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Elysia.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Elysia.Areas.Identity.Pages.Account.Manage
{
    public class IndexModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IWebHostEnvironment _webHostEnvironment; // Thêm môi trường để lưu file

        public IndexModel(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IWebHostEnvironment webHostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _webHostEnvironment = webHostEnvironment;
        }

        public string Username { get; set; }

        [TempData]
        public string StatusMessage { get; set; }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Phone]
            [Display(Name = "Số điện thoại")]
            public string PhoneNumber { get; set; }

            [Display(Name = "Họ và tên")]
            public string FullName { get; set; }

            [Display(Name = "Ảnh đại diện")]
            public IFormFile AvatarFile { get; set; } // Nhận file upload

            public string CurrentAvatarUrl { get; set; } // Hiển thị ảnh hiện tại
        }

        private async Task LoadAsync(ApplicationUser user)
        {
            var userName = await _userManager.GetUserNameAsync(user);
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);

            Username = userName;

            Input = new InputModel
            {
                PhoneNumber = phoneNumber,
                FullName = user.FullName,
                CurrentAvatarUrl = user.AvatarUrl ?? "/images/default-avatar.png" // Ảnh mặc định nếu chưa có
            };
        }

        public async Task<IActionResult> OnGetAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await LoadAsync(user);
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadAsync(user);
                return Page();
            }

            // 1. Cập nhật Số điện thoại
            var phoneNumber = await _userManager.GetPhoneNumberAsync(user);
            if (Input.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, Input.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    StatusMessage = "Lỗi bất ngờ khi cập nhật số điện thoại.";
                    return RedirectToPage();
                }
            }

            // 2. Cập nhật Họ tên (FullName)
            if (Input.FullName != user.FullName)
            {
                user.FullName = Input.FullName;
                await _userManager.UpdateAsync(user);
            }

            // 3. Xử lý Upload Ảnh đại diện (Avatar)
            if (Input.AvatarFile != null && Input.AvatarFile.Length > 0)
            {
                // Tạo tên file duy nhất
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(Input.AvatarFile.FileName);

                // Đường dẫn thư mục lưu: wwwroot/uploads/avatars
                string uploadFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "avatars");

                // Tạo thư mục nếu chưa có
                if (!Directory.Exists(uploadFolder)) Directory.CreateDirectory(uploadFolder);

                string filePath = Path.Combine(uploadFolder, fileName);

                // Lưu file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await Input.AvatarFile.CopyToAsync(stream);
                }

                // Cập nhật đường dẫn vào DB
                user.AvatarUrl = "/uploads/avatars/" + fileName;
                await _userManager.UpdateAsync(user);
            }

            await _signInManager.RefreshSignInAsync(user);
            StatusMessage = "Hồ sơ của bạn đã được cập nhật thành công!";
            return RedirectToPage();
        }
    }
}