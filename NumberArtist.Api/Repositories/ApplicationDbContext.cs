using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Core.Business.Objects;
using Core.Business.Objects.Models;

namespace NumberArtist.Api.Data;

public class ApplicationDbContext : IdentityDbContext<AppUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    // It is required to override this method when adding/removing migrations from class library
    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite();
    public DbSet<DxfFile> DxfFiles { get; set; }

    public DbSet<ReferenceDrawing> ReferenceDrawings { get; set; }
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
    }
}