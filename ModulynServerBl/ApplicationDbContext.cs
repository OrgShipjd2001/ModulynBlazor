using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Modulyn.Server.Bl.IdentityGroups;

namespace Modulyn.Server.Bl
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
            //Database.Migrate();
        }

        public DbSet<ApplicationGroup> Groups => Set<ApplicationGroup>();
        public DbSet<ApplicationUserGroup> UserGroups => Set<ApplicationUserGroup>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<ApplicationGroup>(b =>
            {
                b.HasIndex(x => x.Name).IsUnique();
            });

            builder.Entity<ApplicationUserGroup>(b =>
            {
                b.HasKey(x => new { x.UserId, x.GroupId });

                b.HasOne(x => x.User)
                    .WithMany(u => u.UserGroups)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                b.HasOne(x => x.Group)
                    .WithMany(g => g.UserGroups)
                    .HasForeignKey(x => x.GroupId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }

    }
}
