using INZYNIERKA.Domain.Models;
using INZYNIERKA.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INZYNIERKA.Services.Services
{
    public static class InzynierkaServiceCollectionExtensions
    {
        public static IServiceCollection AddInzynierkaCommunication<TUser>(this IServiceCollection services) where TUser : User
        {
            services.AddScoped<IAccountService<TUser>, AccountService<TUser>>();
            services.AddScoped<IAiMatchmakingService<TUser>, AiMatchmakingService<TUser>>();
            services.AddScoped<IChatAiService<TUser>, ChatAiService<TUser>>();
            services.AddScoped<IChatService<TUser>, ChatService<TUser>>();
            services.AddScoped<IFriendshipService<TUser>, FriendshipService<TUser>>();
            services.AddScoped<IGroupMemberService<TUser>, GroupMemberService<TUser>>();
            services.AddScoped<IGroupService<TUser>, GroupService<TUser>>();
            services.AddScoped<IMatchmakingService<TUser>, MatchmakingService<TUser>>();
            services.AddScoped<INotificationService<TUser>, NotificationService<TUser>>();
            services.AddScoped<IProfileService<TUser>, ProfileService<TUser>>();
            services.AddScoped<ITagService<TUser>, TagService<TUser>>();
            services.AddScoped<IEmailService, EmailService>();
            services.AddScoped<IFileService, FileService>();
            services.AddScoped<IGeminiService, GeminiService>();

            return services;
        }
    }
}
