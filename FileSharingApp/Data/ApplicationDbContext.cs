using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileSharingApp.Models;

namespace FileSharingApp.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions options) : base(options)
        {

        }

        public DbSet<Uploads> Uploads { get; set; }
        public DbSet<Contact> Contacts { get; set; }

        public DbSet<FileSharingApp.Models.UploadViewModel> UploadViewModel { get; set; }
    }
}
