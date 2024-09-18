using HuquqApi.Model;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace HuquqApi
{
    public static class ServiceRegistration
    {
        public static void Register(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddControllers();

            services.AddCors(options =>
            {
                options.AddPolicy("AllowAllOrigins",
                    builder => builder.AllowAnyOrigin()
                                      .AllowAnyMethod()
                                      .AllowAnyHeader());
            });

            services.AddDbContext<HuquqDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["JWT:ValidIssuer"],
                    ValidAudience = configuration["JWT:ValidAudience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Secret"]))
                };
                options.Events = new JwtBearerEvents
                {
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = 401;
                        context.Response.ContentType = "application/json";
                        var result = System.Text.Json.JsonSerializer.Serialize(new { message = "Yetkilendirme başarısız." });
                        return context.Response.WriteAsync(result);
                    }
                };
            });



            services.AddSingleton<ChatService>();

            services.AddIdentity<User, IdentityRole>(opt => {
                opt.Password.RequireNonAlphanumeric = true;
                opt.Password.RequiredLength = 6;
                opt.Password.RequireUppercase = true;
                opt.Password.RequireLowercase = true;
                opt.Password.RequireDigit = true;
                opt.User.RequireUniqueEmail = true;
                
            }).AddEntityFrameworkStores<HuquqDbContext>().AddDefaultTokenProviders()



                .AddEntityFrameworkStores<HuquqDbContext>()
                .AddDefaultTokenProviders();

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }
    }
}
