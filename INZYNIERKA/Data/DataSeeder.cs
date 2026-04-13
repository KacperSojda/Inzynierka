using INZYNIERKA.Data;
using INZYNIERKA.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace INZYNIERKA.Seeders
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
                    new Tag {Name = "Filmy i Seriale"},
                    new Tag {Name = "Książki"},
                    new Tag {Name = "Programowanie"}
                );
                await context.SaveChangesAsync();
            }

            var allTags = await context.Tags.ToDictionaryAsync(t => t.Name);

            var userNames = new List<string> {"User1", "User2", "User3", "User4", "User5"};

            foreach (var name in userNames)
            {
                Console.WriteLine($"\n--- Start TWORZENIA UŻYTKOWNIKA {name}: ---\n");
                if (!await userManager.Users.AnyAsync(u => u.UserName == name))
                {
                    var user = new User
                    {
                        UserName = name,
                        Email = $"{name}@test.com",
                        PublicDescription = $"Jestem {name}. To konto wygenerowane automatycznie.",
                        PrivateDescription = $"Prywatny opis {name}.",
                        Avatar = ""
                    };

                    var result = await userManager.CreateAsync(user, "1234567890!");

                    if (!result.Succeeded)
                    {
                        var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                        Console.WriteLine($"\n--- BŁĄD TWORZENIA UŻYTKOWNIKA {name}: {errors} ---\n");
                    }
                }
            }

            var users = await userManager.Users.ToDictionaryAsync(u => u.UserName);

            if (!await context.UserTags.AnyAsync())
            {
                void AssignTag(string uName, string tName)
                {
                    if (users.ContainsKey(uName) && allTags.ContainsKey(tName))
                        context.UserTags.Add(
                            new UserTag 
                            {
                                UserId = users[uName].Id, 
                                TagId = allTags[tName].Id
                            }
                        );
                }
                AssignTag("User1", "Gry komputerowe"); 
                AssignTag("User1", "Sport");
                AssignTag("User2", "Muzyka");
                AssignTag("User3", "Filmy i Seriale");
                AssignTag("User4", "Sport"); 
                AssignTag("User4", "Filmy i Seriale");
                AssignTag("User5", "Sport");
                await context.SaveChangesAsync();
            }

            if (!await context.UserFriends.AnyAsync())
            {
                void AddFriendship(string u1Name, string u2Name)
                {
                    var u1Id = users[u1Name].Id;
                    var u2Id = users[u2Name].Id;

                    context.UserFriends.AddRange(
                        new UserFriend {UserId = u1Id, FriendId = u2Id, Status = FriendshipStatus.Accepted},
                        new UserFriend {UserId = u2Id, FriendId = u1Id, Status = FriendshipStatus.Accepted}
                    );
                }

                AddFriendship("User1", "User2");
                AddFriendship("User1", "User3");
                AddFriendship("User3", "User4");

                context.UserFriends.Add(
                    new UserFriend {
                        UserId = users["User5"].Id, 
                        FriendId = users["User4"].Id, 
                        Status = FriendshipStatus.Pending
                    }
                );
                context.Notifications.Add(new Notification
                {
                    SenderId = users["User5"].Id,
                    ReceiverId = users["User4"].Id,
                    Type = NotificationType.FriendRequest,
                    CreationDate = DateTime.UtcNow
                });

                await context.SaveChangesAsync();
            }

            if (!await context.Groups.AnyAsync())
            {
                async Task CreateGroupWithMembers(string groupName, string founderName, List<string> members, string tagName)
                {
                    var group = new Group
                    {
                        Name = groupName,
                        Description = $"Grupa: {groupName}"
                    };
                    context.Groups.Add(group);
                    await context.SaveChangesAsync();

                    if (allTags.ContainsKey(tagName))
                    {
                        context.GroupTags.Add(new GroupTag {GroupId = group.Id, TagId = allTags[tagName].Id});
                    }

                    context.UserGroups.Add(new UserGroup {ChatGroupId = group.Id, UserId = users[founderName].Id, Type = MemberType.Administrator});

                    foreach (var memberName in members)
                    {
                        if (memberName == founderName) continue;

                        context.UserGroups.Add(new UserGroup {ChatGroupId = group.Id, UserId = users[memberName].Id, Type = MemberType.Member});
                    }
                }

                await CreateGroupWithMembers("Gry komputerowe", "User1", new List<string>(), "Gry komputerowe");
                await CreateGroupWithMembers("Sport", "User1", new List<string> {"User4", "User5"}, "Sport");
                await CreateGroupWithMembers("Muzyka", "User4", new List<string> {"User2"}, "Muzyka");
                await context.SaveChangesAsync();
            }

            if (!await context.Messages.AnyAsync())
            {
                context.Messages.AddRange(
                    new Message {SenderId = users["User1"].Id, ReceiverId = users["User2"].Id, Content = "Cześć?", DateTime = DateTime.UtcNow.AddHours(-2) },
                    new Message {SenderId = users["User2"].Id, ReceiverId = users["User1"].Id, Content = "Hej, co tam?", DateTime = DateTime.UtcNow.AddHours(-1) },
                    new Message {SenderId = users["User1"].Id, ReceiverId = users["User2"].Id, Content = "Nic ciekawego, jak ci mija dzień?", DateTime = DateTime.UtcNow.AddMinutes(-30) }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.GroupMessages.AnyAsync())
            {
                var sportGroup = await context.Groups.FirstOrDefaultAsync(g => g.Name == "Sport");
                if (sportGroup != null)
                {
                    context.GroupMessages.Add(new GroupMessage
                    {
                        GroupId = sportGroup.Id,
                        SenderId = users["User1"].Id,
                        Content = "Witamy w grupie sportowej",
                        Timestamp = DateTime.UtcNow.AddDays(-1)
                    });
                }
                await context.SaveChangesAsync();
            }
        }
    }
}