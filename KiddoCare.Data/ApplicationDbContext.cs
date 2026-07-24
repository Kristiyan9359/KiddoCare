namespace KiddoCare.Data;

using KiddoCare.Data.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
{
    public virtual DbSet<KindergartenGroup> KindergartenGroups { get; set; } = null!;

    public virtual DbSet<Child> Children { get; set; } = null!;
}