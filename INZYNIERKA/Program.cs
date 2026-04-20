using INZYNIERKA.Data;
using INZYNIERKA.Hubs;
using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Services;
using INZYNIERKA.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddDbContext<INZDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("INZYNIERKA.Data")));

builder.Services.AddIdentity<User, IdentityRole>(options =>
{
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
    options.User.RequireUniqueEmail = false;
    options.SignIn.RequireConfirmedAccount = false;
    options.SignIn.RequireConfirmedEmail = false;
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
    .AddEntityFrameworkStores<INZDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddSession();

builder.Services.AddSignalR();

builder.Services.AddHttpClient<IGeminiService, GeminiService>();

builder.Services.AddScoped<IFriendshipService, FriendshipService>();

builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddScoped<ITagService, TagService>();

builder.Services.AddTransient<IFileService, FileService>();

builder.Services.AddScoped<IProfileService, ProfileService>();

builder.Services.AddScoped<IMatchmakingService, MatchmakingService>();

builder.Services.AddScoped<IAiMatchmakingService, AiMatchmakingService>();

builder.Services.AddScoped<IGroupService, GroupService>();

builder.Services.AddScoped<IGroupMemberService, GroupMemberService>();

builder.Services.AddScoped<IChatService, ChatService>();

builder.Services.AddScoped<IChatAiService, ChatAiService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

try
{
    await DataSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    Console.WriteLine($"Wyst¹pi³ b³¹d podczas seedowania bazy danych: {ex.Message}");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseSession();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ChatHub>("/chathub");

app.MapHub<GroupChatHub>("/groupchathub");

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<INZDbContext>();
        context.Database.Migrate(); // Automatycznie stworzy tabele w chmurze
        Console.WriteLine("Migracje bazy danych zosta³y pomyœlnie na³o¿one.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Wyst¹pi³ b³¹d podczas nak³adania migracji: {ex.Message}");
    }
}

app.Run();