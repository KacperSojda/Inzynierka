using INZYNIERKA.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace INZYNIERKA.Data
{
    public static class DataSeeder
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<INZDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            await context.Database.MigrateAsync();

            if (!await context.Tags.AnyAsync())
            {
                context.Tags.AddRange(
                    new Tag {Name = "Gry komputerowe"},
                    new Tag {Name = "Sport"},
                    new Tag {Name = "Muzyka"},
                    new Tag {Name = "Filmy"},
                    new Tag {Name = "Seriale"},
                    new Tag {Name = "Książki"},
                    new Tag {Name = "Programowanie"}
                );
                await context.SaveChangesAsync();
            }
        }
    }
}