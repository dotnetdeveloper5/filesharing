using AutoMapper;
using AutoMapper.QueryableExtensions;
using FileSharingApp.Data;
using FileSharingApp.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileSharingApp.Services
{
    public class UploadService : IUploadService
    {
        private readonly ApplicationDbContext _db;
        private readonly IMapper _mapper;

        public UploadService(ApplicationDbContext conext, IMapper mapper)
        {
            this._db = conext;
            this._mapper = mapper;
        }

        public IQueryable<UploadViewModel> GetBy(string userId)
        {
            var result = _db.Uploads.Where(u => u.UserId == userId).Select(u => new UploadViewModel
            {
                Id = u.Id,
                FileName = u.FileName,
                OriginalFileName = u.OriginalFileName,
                ContentType = u.ContentType,
                Size = u.Size,
                UploadDate = u.UploadDate,
                DownloadCount = u.DownloadCount
            });
            return result;
        }
        public async Task Create(InputUpload model)
        {
            var mappedObject = _mapper.Map<Uploads>(model);
            await _db.Uploads.AddAsync(mappedObject);
            //{
            //    OriginalFileName = model.FileName,
            //    FileName = model.FileName,
            //    ContentType = model.ContentType,
            //    Size = model.Size,
            //    UserId = model.UserId,
            //    UploadDate = DateTime.Now
            //});
            await _db.SaveChangesAsync();
        }

        public async Task Delete(string id, string userId)
        {
            var selectedUpload = await _db.Uploads.FirstOrDefaultAsync(u => u.Id == id && u.UserId == userId);
            if (selectedUpload != null)
            {
                _db.Uploads.Remove(selectedUpload);
                await _db.SaveChangesAsync();
            }
        }

        public async Task<UploadViewModel> Find(string id, string userId)
        {
            var selectedUpload = await _db.Uploads.FirstOrDefaultAsync(u => u.Id == id && u.UserId == userId);
            if(selectedUpload != null)
            {
                // AutoMapper
                return new UploadViewModel
                {
                    Id = selectedUpload.Id,
                    ContentType = selectedUpload.ContentType,
                    DownloadCount = selectedUpload.DownloadCount,
                    FileName = selectedUpload.FileName,
                    OriginalFileName = selectedUpload.OriginalFileName,
                    Size = selectedUpload.Size,
                    UploadDate = selectedUpload.UploadDate
                };
            }
            return null;
        }

        public IQueryable<UploadViewModel> GetAll()
        {
            return null;
        }

        public IQueryable<UploadViewModel> Search(string term)
        {
            var result = _db.Uploads.Where(u => u.OriginalFileName.Contains(term)).ProjectTo<UploadViewModel>(_mapper.ConfigurationProvider);
            //{
            //    Id = u.Id,
            //    FileName = u.FileName,
            //    OriginalFileName = u.OriginalFileName,
            //    ContentType = u.ContentType,
            //    Size = u.Size,
            //    UploadDate = u.UploadDate
            //});
            return result;
        }

        public async Task<UploadViewModel> FindAsync(string fileName)
        {
            var selectedUpload = await _db.Uploads.FindAsync(fileName);
            var mappedObject = _mapper.Map<UploadViewModel>(selectedUpload);
            if (mappedObject != null)
            {
                // AutoMapper
                return mappedObject;
            }
            return null;
        }
    }
}
