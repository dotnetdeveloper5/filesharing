using AutoMapper;
using AutoMapper.QueryableExtensions;
using FileSharingApp.Areas.Admin.Models;
using FileSharingApp.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FileSharingApp.Areas.Admin.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly RoleManager<IdentityRole> roleManager;

        public UserService(ApplicationDbContext context, IMapper mapper, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            this._context = context;
            this._mapper = mapper;
            this.userManager = userManager;
            this.roleManager = roleManager;
        }
        public async Task<OperationResult> ToggleBlockUser(string userId)
        {
            var existedUser = await _context.Users.FindAsync(userId);
            if (existedUser == null)
            {
                return OperationResult.NotFound();
            }
            existedUser.IsBlocked = !existedUser.IsBlocked;
            _context.Update(existedUser);
            await _context.SaveChangesAsync();
            return OperationResult.Succeeded();
        }

        public IQueryable<AdminUserViewModel> GetAll()
        {
            var result = _context.Users.ProjectTo<AdminUserViewModel>(_mapper.ConfigurationProvider);
            return result;
        }

        public IQueryable<AdminUserViewModel> GetBlockedUsers()
        {
            var result = _context.Users.Where(u => u.IsBlocked).ProjectTo<AdminUserViewModel>(_mapper.ConfigurationProvider);
            return result;
        }

        public IQueryable<AdminUserViewModel> Search(string term)
        {
            var result = _context.Users.Where(u => u.Email == term || u.FirstName.Contains(term) || (u.FirstName + " " + u.LastName).Contains(term)).ProjectTo<AdminUserViewModel>(_mapper.ConfigurationProvider);
            return result;
        }

        public async Task<int> UserRegisterationCount()
        {
            var count = await _context.Users.CountAsync();
            return count;
        }

        public async Task<int> UserRegisterationCount(int month)
        {
            var year = DateTime.Today.Year;
            var result = await _context.Users.CountAsync(u => u.CreatedDate.Month == month && u.CreatedDate.Year == year);
            return result;
        }

        public async Task InitializeAsync()
        {
            if(!await roleManager.RoleExistsAsync(UserRoles.Admin))
            {
                await roleManager.CreateAsync(new IdentityRole(UserRoles.Admin));
            }
            var adminEmail = "admin@a.com";
            if(await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var user = new ApplicationUser
                {
                    Email = adminEmail,
                    UserName = adminEmail,
                    EmailConfirmed = true,
                    PhoneNumber = "5777657646536",
                    PhoneNumberConfirmed = true
                };
                await userManager.CreateAsync(user, "P@ssw0rd");
                await userManager.AddToRoleAsync(user, UserRoles.Admin);
            }
        }
    }
}
