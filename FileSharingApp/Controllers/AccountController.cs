using AutoMapper;
using FileSharingApp.Data;
using FileSharingApp.Helpers.Mail;
using FileSharingApp.Models;
using FileSharingApp.Resources.Views.Shared;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FileSharingApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> signInManager;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IMapper _mapper;
        private readonly IStringLocalizer<_Layout> _stringLocalizer;
        private readonly IMailHelper _mailHelper;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager, IMapper mapper, IStringLocalizer<_Layout> stringLocalizer, IMailHelper mailHelper)
        {
            this.signInManager = signInManager;
            this.userManager = userManager;
            this._mapper = mapper;
            this._stringLocalizer = stringLocalizer;
            this._mailHelper = mailHelper;
        }

        [HttpGet]
        public IActionResult ExternalLogin(string provider)
        {
            var properties = signInManager.ConfigureExternalAuthenticationProperties(provider, "/Account/ExternalResponse");
            return Challenge(properties, provider);
        }

        [HttpGet]
        public async Task<IActionResult> ExternalResponse()
        {
            var info = await signInManager.GetExternalLoginInfoAsync();
            if(info == null)
            {
                TempData["Message"] = "Login Failed";
                return RedirectToAction("Login");
            }
            var loginResult = await signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false);
            if (!loginResult.Succeeded)
            {
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var firstName = info.Principal.FindFirstValue(ClaimTypes.GivenName);
                var lastName = info.Principal.FindFirstValue(ClaimTypes.Surname);
                // Create Local Account
                var userToCreate = new ApplicationUser
                {
                    Email = email,
                    UserName = email,
                    FirstName = firstName,
                    LastName = lastName,
                    EmailConfirmed = true
                };

                var createResult = await userManager.CreateAsync(userToCreate);
                if (createResult.Succeeded)
                {
                    var exLoginResult = await userManager.AddLoginAsync(userToCreate, info);
                    if (exLoginResult.Succeeded)
                    {
                        await signInManager.SignInAsync(userToCreate, false, info.LoginProvider);
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        await userManager.DeleteAsync(userToCreate);
                    }
                }
                return RedirectToAction("Login");
            }
            return RedirectToAction("Index", "Home");
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModels model, string returnUrl)
        {
            if (ModelState.IsValid)
            {
                var result = await signInManager.PasswordSignInAsync(model.Email, model.Password, true, true);
                if (result.Succeeded)
                {
                    if (!string.IsNullOrEmpty(returnUrl))
                    {
                        return LocalRedirect(returnUrl);
                    }
                    return RedirectToAction("Create", "Uploads");
                }else if (result.IsNotAllowed)
                {
                    TempData["Error"] = _stringLocalizer["RequireEmailConfirmation"]?.Value;
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModels model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };
                var result = await userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    //Create Link
                    var token = userManager.GenerateEmailConfirmationTokenAsync(user);
                    var url = Url.Action("ConfirmEmail", "Account", new { token = token, userId = user.Id }, Request.Scheme);
                    //Send Email
                    StringBuilder body = new StringBuilder();
                    body.AppendLine("File Sharing Project: Email Confirmation");
                    body.AppendFormat("to confirm your email, you should click here <a href='{0}'></a>", url);
                    _mailHelper.SendMail(new InputEmailMessage
                    {
                        Body = body.ToString(),
                        Email = model.Email,
                        Subject = "Email Confirmation"
                    });

                    return RedirectToAction("RequireEmailConfirm");
                    //await signInManager.SignInAsync(user, false);
                    //return RedirectToAction("Create", "Uploads");
                }
                foreach(var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                };
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult RequireEmailConfirm()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Info()
        {
            var currentUser = await userManager.GetUserAsync(User);
            if(currentUser != null)
            {
                var model = _mapper.Map<UserViewModel>(currentUser);
                return View(model);
            }
            return NotFound();
        }

        [HttpPost]
        public async Task<IActionResult> Info(UserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var currentUser = await userManager.GetUserAsync(User);
                if (currentUser != null)
                {
                    currentUser.FirstName = model.FirstName;
                    currentUser.LastName = model.LastName;

                    var result = await userManager.UpdateAsync(currentUser);
                    if (result.Succeeded)
                    {
                        TempData["Success"] = _stringLocalizer["SuccessMessage"]?.Value;
                        return RedirectToAction("Info");
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                if (ModelState.IsValid)
                {
                    var result = await userManager.ChangePasswordAsync(currentUser, model.CurrentPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        TempData["Success"] = _stringLocalizer["ChangePasswordMessage"]?.Value;
                        await signInManager.SignOutAsync();
                        return RedirectToAction("Login");
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            else
            {
                return NotFound();
            }
            return View("Info", _mapper.Map<UserViewModel>(currentUser));
        }

        [HttpPost]
        public async Task<IActionResult> AddPassword(AddPasswordViewModel model)
        {
            var currentUser = await userManager.GetUserAsync(User);
            if (currentUser != null)
            {
                if (ModelState.IsValid)
                {
                    var result = await userManager.AddPasswordAsync(currentUser, model.Password);
                    if (result.Succeeded)
                    {
                        TempData["Success"] = _stringLocalizer["AddPasswordMessage"]?.Value;
                        await signInManager.SignOutAsync();
                        return RedirectToAction("Login");
                    }
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            else
            {
                return NotFound();
            }
            return View("Info", _mapper.Map<UserViewModel>(currentUser));
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(ConfirmEmailViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await userManager.FindByIdAsync(model.UserId);
                if(user != null)
                {
                    if (!user.EmailConfirmed)
                    {
                        var result = await userManager.ConfirmEmailAsync(user, model.Token);
                        if (result.Succeeded)
                        {
                            TempData["Success"] = "Your Email confirmed successfully!";
                            return RedirectToAction("Login");
                        }
                    }
                    else
                    {
                        TempData["Success"] = "Your Email already confirmed!";
                    }
                }
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgetPassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgetPassword(ForgetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existedUser = await userManager.FindByEmailAsync(model.Email);
                if(existedUser != null)
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(existedUser);
                    var url = Url.Action("ResetPassword", "Account", new { token, model.Email }, Request.Scheme);

                    StringBuilder body = new StringBuilder();
                    body.AppendLine("File sharing Application: Reset Password");
                    body.AppendLine("We are sending an email, because we recieved a reset password request to your account");
                    body.AppendFormat("to reset new password, click this link <a href='{0}'>click here</a>", url);

                    _mailHelper.SendMail(new InputEmailMessage
                    {
                        Body = body.ToString(),
                        Email = model.Email,
                        Subject = "Reset Password"
                    });
                }
                TempData["Success"] = "If your email matching an existing account in our system, you should recieve an email";
            }
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ResetPassword(VerifyResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existedUser = await userManager.FindByEmailAsync(model.Email);
                if(existedUser != null)
                {
                    var isValid = await userManager.VerifyUserTokenAsync(existedUser, TokenOptions.DefaultProvider, "Reset Password", model.Token);
                    if (isValid)
                    {
                        return View();
                    }
                    else
                    {
                        TempData["Error"] = "Token is invalid";
                    }
                }
            }
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existedUser = await userManager.FindByEmailAsync(model.Email);
                if (existedUser != null)
                {
                    var result = await userManager.ResetPasswordAsync(existedUser, model.Token, model.NewPassword);
                    if (result.Succeeded)
                    {
                        TempData["Success"] = "Reset Password Completed Successfully!";
                        return RedirectToAction("Login");
                    }
                    foreach(var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                }
            }
            return View(model);
        }
    }
}
