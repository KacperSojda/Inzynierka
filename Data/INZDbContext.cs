using INZYNIERKA.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace INZYNIERKA.Data
{
    public class INZDbContext : IdentityDbContext<User>
    {
        public DbSet<Tag> Tags { get; set; }
        public DbSet<UserTag> UserTags {  get; set; }
        public DbSet<Notification> notifications { get; set; }
        public INZDbContext(DbContextOptions options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<UserTag>()
                .HasKey(ut => new {ut.UserId, ut.TagId});

            builder.Entity<UserTag>()
                .HasOne(ut => ut.User)
                .WithMany(u => u.UserTags)
                .HasForeignKey(ut => ut.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserTag>()
                .HasOne(ut => ut.Tag)
                .WithMany(t => t.UserTags)
                .HasForeignKey(ut => ut.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
