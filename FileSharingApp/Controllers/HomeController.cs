using FileSharingApp.Data;
using FileSharingApp.Helpers.Mail;
using FileSharingApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace FileSharingApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IMailHelper _mailHelper;

        public ApplicationDbContext _db { get; }

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, IMailHelper mailHelper)
        {
            _logger = logger;
            _db = context;
            _mailHelper = mailHelper;
        }

        public IActionResult SetCulture(string lang)
        {
            if (!string.IsNullOrEmpty(lang))
            {
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(lang)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });
            }
            return RedirectToAction("Index");
        }

        public IActionResult Index()
        {
            var highestDownloads = _db.Uploads.OrderByDescending(u => u.DownloadCount).Select(u => new UploadViewModel
            {
                Id = u.Id,
                FileName = u.FileName,
                OriginalFileName = u.OriginalFileName,
                ContentType = u.ContentType,
                Size = u.Size,
                UploadDate = u.UploadDate,
                DownloadCount = u.DownloadCount
            }).Take(3);
            ViewBag.Popular = highestDownloads;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Info()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Contact()
        {
            return View();
        }

        [HttpGet]
        public IActionResult About()
        {
            return View();
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Contact(ContactViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Save
                _db.Contacts.Add(new Data.Contact
                {
                    Email = model.Email,
                    Message = model.Message,
                    Name = model.Name,
                    Subject = model.Subject,
                    UserId = UserId
                });
                await _db.SaveChangesAsync();
                TempData["Message"] = "Message has been sent successfully!";

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("<h1> File Sharing - Unread Message </h2>");
                sb.AppendFormat("Name: {0}", model.Name);
                sb.AppendFormat("Email: {0}", model.Email);
                sb.AppendLine();
                sb.AppendFormat("Subject: {0}", model.Subject);
                sb.AppendFormat("Message: {0}", model.Message);

                _mailHelper.SendMail(new InputEmailMessage
                {
                    Subject = "You have unraed message!",
                    Email = "info@site.com",
                    Body = sb.ToString()
                });


                return RedirectToAction("Contact");
            }
            return View(model);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
