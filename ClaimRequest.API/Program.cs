using System.Text;
using System.Text.Json.Serialization;
using ClaimRequest.API.Extensions;
using ClaimRequest.API.Middlewares;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.BLL.Services.Implements.VNPayService.Services;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.BLL.Utils;
using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.DAL.Data.MetaDatas;
using ClaimRequest.DAL.Repositories.Implements;
using ClaimRequest.DAL.Repositories.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Register services, database, authentication, and Swagger
ConfigureServices();
ConfigureDatabase();
ConfigureAuthentication();
ConfigureSwagger();

var app = builder.Build();

// Configure the HTTP request pipeline
ConfigureMiddleware();

app.Run();

// Method to configure services
void ConfigureServices()
{
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        });

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

    // Register Unit of Work pattern
    builder.Services.AddScoped<IUnitOfWork<ClaimRequestDbContext>, UnitOfWork<ClaimRequestDbContext>>();

    // Register utility services
    builder.Services.AddSingleton<JwtUtil>();
    builder.Services.AddSingleton<OtpUtil>();

    // Register application services
    RegisterApplicationServices();

    // Uncomment to suppress automatic model state validation
    // builder.Services.Configure<ApiBehaviorOptions>(options =>
    // {
    //     options.SuppressModelStateInvalidFilter = true;
    // });

    // ----- FOR DOCKER COMPOSE => ENABLE THIS -----
    //  This would configure Kestrel to listen on specific ports(5000 for HTTP and 5001 for HTTP / 2).
    //  It appears this was commented out to use default ports instead.

    // builder.WebHost.ConfigureKestrel(serverOptions =>
    // {
    //     serverOptions.ListenAnyIP(5000); // HTTP
    //     serverOptions.ListenAnyIP(5001, listenOptions =>
    //     {
    //         // In development/docker, we'll use HTTP instead of HTTPS
    //         listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    //     });
    // });
}

// Method to register application services
void RegisterApplicationServices()
{
    builder.Services.AddScoped<IClaimService, ClaimService>();
    builder.Services.AddScoped<IStaffService, StaffService>();
    builder.Services.AddScoped<IProjectService, ProjectService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IOtpService, OtpService>();
    builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddHostedService<PasswordReminderService>();
    builder.Services.AddHostedService<PendingReminderService>();
    builder.Services.AddScoped<IVnPayService, VnPayService>();
    builder.Services.AddScoped<IWebNavigatorService, WebNavigatorService>(); // Register WebNavigatorService
    builder.Services.AddScoped<IGenericRepository<Claim>, GenericRepository<Claim>>();
    builder.Services.AddScoped<IGenericRepository<Payment>, GenericRepository<Payment>>();
    builder.Services.AddScoped<IRefreshTokensService, RefreshTokensService>();
    builder.Services.AddScoped<IPasswordReminderService, PasswordReminderService>();
    builder.Services.AddScoped<IPendingReminderService, PendingReminderService>();
    builder.Services.AddScoped<IJwtUtil, JwtUtil>();
}

// Method to configure the database
void ConfigureDatabase()
{
    builder.Services.AddDbContext<ClaimRequestDbContext>(options =>
    {
        options.UseNpgsql(builder.Configuration.GetConnectionString("SupaBaseConnection"),
            npgsqlOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });
    });
}

// Method to configure authentication
void ConfigureAuthentication()
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidIssuer = builder.Configuration.GetSection("Jwt:Issuer").Get<string>(),
                ValidAudience = builder.Configuration.GetSection("Jwt:Audience").Get<string>(),
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
            };

            // Automatically prepend "Bearer " to the token if missing
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Headers["Authorization"].FirstOrDefault();

                    if (!string.IsNullOrEmpty(accessToken) && !accessToken.StartsWith("Bearer "))
                    {
                        context.Request.Headers["Authorization"] = "Bearer " + accessToken;
                    }

                    return Task.CompletedTask;
                },

                // Custom response for unauthorized requests
                OnChallenge = async context =>
                {
                    context.HandleResponse();
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    context.Response.ContentType = "application/json";

                    var response = new ApiResponse<object>
                    {
                        StatusCode = StatusCodes.Status401Unauthorized,
                        Message = "Unauthorized access",
                        Reason = "Authentication failed. Please provide a valid token.",
                        IsSuccess = false,
                        Data = new
                        {
                            Path = context.Request.Path,
                            Method = context.Request.Method,
                            Timestamp = DateTime.UtcNow
                        }
                    };

                    await context.Response.WriteAsJsonAsync(response);
                }
            };
        });

    // Add authorization policies
    builder.Services.AddAuthorization(options =>
    {
        options.AddClaimRequestPolicies();
    });
}

// Method to configure Swagger
void ConfigureSwagger()
{
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "ClaimRequest.API",
            Version = "v1",
            Description = "A Claim Request System Project"
        });

        options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, new OpenApiSecurityScheme
        {
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = JwtBearerDefaults.AuthenticationScheme,
            Description = "JWT Authorization header using the Bearer scheme. Example: "
        });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = JwtBearerDefaults.AuthenticationScheme
                    },
                    Scheme = "Oauth2",
                    Name = JwtBearerDefaults.AuthenticationScheme,
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                },
                new List<string>()
            }
        });
    });
}

// Method to configure the HTTP request pipeline
void ConfigureMiddleware()
{
    // Enable Swagger in development environment
    // if (app.Environment.IsDevelopment())
    // {
    app.UseSwagger();
    app.UseSwaggerUI();
    // }

    // Add custom exception handling middleware
    app.UseMiddleware<ExceptionHandlerMiddleware>();

    // Add custom middleware to allow only password reset requests
    app.UseMiddleware<ResetPasswordOnlyMiddleware>();

    // FOR DOCKER COMPOSE => OFF THIS
    app.UseHttpsRedirection();

    // Configure CORS to allow requests from localhost and vercel.app
    app.UseCors(options =>
    {
        options.SetIsOriginAllowed(origin =>
           origin.StartsWith("http://localhost:") ||
           origin.StartsWith("https://localhost:") ||
           origin.EndsWith(".vercel.app"))
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });

    app.UseAuthorization();

    app.MapControllers();
}
