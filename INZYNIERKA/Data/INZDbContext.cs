using System.Reflection.Emit;
using INZYNIERKA.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;

namespace INZYNIERKA.Data
{
    public class INZDbContext : IdentityDbContext<User>
    {
        public DbSet<Tag> Tags { get; set; }
        public DbSet<UserTag> UserTags {  get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<UserFriend> UserFriends { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<UserGroup> UserGroups { get; set; }
        public DbSet<GroupMessage> GroupMessages { get; set; }


        public INZDbContext(DbContextOptions<INZDbContext> options) : base(options) { }

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

            builder.Entity<Notification>()
                .HasOne(n => n.Sender)
                .WithMany(u => u.SendedNotifications)
                .HasForeignKey(n => n.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Notification>()
                .HasOne(n => n.Receiver)
                .WithMany(u => u.ReceivedNotifications)
                .HasForeignKey(n => n.ReceiverId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<UserFriend>()
                .HasKey(uf => new { uf.UserId, uf.FriendId });

            builder.Entity<UserFriend>()
                .HasOne(uf => uf.User)
                .WithMany(u => u.SendedFriendRequests)
                .HasForeignKey(uf => uf.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserFriend>()
                .HasOne(uf => uf.Friend)
                .WithMany(u => u.ReceivedFriendRequests)
                .HasForeignKey(uf => uf.FriendId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(n => n.Sender)
                .WithMany(u => u.SendedMessages)
                .HasForeignKey(n => n.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(n => n.Receiver)
                .WithMany(u => u.ReceivedMessages)
                .HasForeignKey(n => n.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserGroup>()
                .HasKey(ug => new { ug.UserId, ug.ChatGroupId });

            builder.Entity<UserGroup>()
                .HasOne(ug => ug.User)
                .WithMany(u => u.JoinedGroups)
                .HasForeignKey(ug => ug.UserId);

            builder.Entity<UserGroup>()
                .HasOne(ug => ug.ChatGroup)
                .WithMany(g => g.Members)
                .HasForeignKey(ug => ug.ChatGroupId);

            builder.Entity<GroupMessage>()
                .HasOne(gm => gm.ChatGroup)
                .WithMany(g => g.Messages)
                .HasForeignKey(gm => gm.GroupId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GroupMessage>()
                .HasOne(gm => gm.Sender)
                .WithMany()
                .HasForeignKey(gm => gm.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
