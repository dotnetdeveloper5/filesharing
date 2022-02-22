using FileSharingApp.Areas.Admin.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileSharingApp.Areas.Admin.Services
{
    public interface IUserService
    {
        IQueryable<AdminUserViewModel> GetAll();
        IQueryable<AdminUserViewModel> GetBlockedUsers();
        IQueryable<AdminUserViewModel> Search(string term);
        Task<OperationResult> ToggleBlockUser(string userId);
        Task<int> UserRegisterationCount();
        Task<int> UserRegisterationCount(int month);
        Task InitializeAsync();
    }
}
