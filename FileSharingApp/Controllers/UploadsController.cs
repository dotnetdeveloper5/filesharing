using FileSharingApp.Data;
using FileSharingApp.Models;
using FileSharingApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace FileSharingApp.Controllers
{
    [Authorize]
    public class UploadsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment env;
        private readonly IUploadService uploadService;

        public UploadsController(ApplicationDbContext context, IWebHostEnvironment env, IUploadService uploadService)
        {
            this._db = context;
            this.env = env;
            this.uploadService = uploadService;
        }
        public IActionResult Index()
        {
            var result = _db.Uploads.Where(u => u.UserId == UserId).Select(u => new UploadViewModel 
            { 
                Id = u.Id,
                FileName = u.FileName,
                OriginalFileName = u.OriginalFileName,
                ContentType = u.ContentType,
                Size = u.Size,
                UploadDate = u.UploadDate,
                DownloadCount = u.DownloadCount
            });
            return View(result);
        }

        private string UserId
        {
            get
            {
                return User.FindFirstValue(ClaimTypes.NameIdentifier);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Browse(int requiredPage = 1)
        {
            const int pageSize = 2;
            decimal rowsCount = await _db.Uploads.CountAsync();
            var PagesCount = Math.Ceiling(rowsCount / pageSize);
            requiredPage = requiredPage <= 0 ? 1 : requiredPage;
            int skipCount = (requiredPage - 1) * pageSize;
            if(requiredPage > PagesCount)
            {
                requiredPage = 1;
            }
            var model = await _db.Uploads.OrderByDescending(u => u.UploadDate).OrderByDescending(u => u.DownloadCount).Select(u => new UploadViewModel
            {
                Id = u.Id,
                FileName = u.FileName,
                OriginalFileName = u.OriginalFileName,
                ContentType = u.ContentType,
                Size = u.Size,
                UploadDate = u.UploadDate,
                DownloadCount = u.DownloadCount
            }).Skip(skipCount).Take(pageSize).ToListAsync();
            ViewBag.CurrentPage = requiredPage;
            ViewBag.PagesCount = PagesCount;
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Download(string fileName)
        {
            var selectedFile = await uploadService.Find(fileName);
            if(selectedFile == null)
            {
                return NotFound();
            }
            var path = "~/Uploads/" + selectedFile.FileName;
            selectedFile.DownloadCount++;
            _db.Update(selectedFile);
            await _db.SaveChangesAsync();
            Response.Headers.Add("Expires", DateTime.Now.AddDays(-3).ToLongDateString());
            Response.Headers.Add("Cache-Control", "no-cache");
            return File(path, selectedFile.ContentType, selectedFile.OriginalFileName);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult Results(string term)
        {
            var result = uploadService.Search(term);
            return View(result);
        }

        [HttpPost]
        public async Task<IActionResult> Create(InputFile model)
        {
            var newName = Guid.NewGuid().ToString();
            var extension = Path.GetExtension(model.File.FileName);
            var fileName = string.Concat(newName, extension);
            var root = env.WebRootPath;
            var path = Path.Combine(root, "Uploads", fileName);
            using (var fs = System.IO.File.Create(path))
            {
                await model.File.CopyToAsync(fs);
            }
            await uploadService.Create(new InputUpload
            {
                OriginalFileName = model.File.FileName,
                FileName = fileName,
                ContentType = model.File.ContentType,
                Size = model.File.Length,
                UserId = UserId
            });
            return RedirectToAction("Index");
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string id)
        {
            var selectedUpload = await uploadService.Find(id, UserId);
            if(selectedUpload == null)
            {
                return NotFound();
            }
            return View(selectedUpload);
        }

        [HttpPost]
        [ActionName("Delete")]
        public async Task<IActionResult> DeleteConfirmation(string id)
        {
            var selectedUpload = await uploadService.Find(id, UserId);
            if (selectedUpload == null)
            {
                return NotFound();
            }
            await uploadService.Delete(id, UserId);
            return RedirectToAction("Index");
        }
    }
}


