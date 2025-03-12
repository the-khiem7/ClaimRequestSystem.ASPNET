using System.Text;
using System.Text.Json.Serialization;
using ClaimRequest.API.Extensions;
using ClaimRequest.API.Middlewares;
using ClaimRequest.BLL.Services.Implements;
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

// Add services to the container.

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
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

// Add DbContext connect to Postgres
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

// Add services to the container.
//builder.Services.AddAutoMapper(typeof(AutoMapperProfile).Assembly);
// tat ca cac service implement tu Profile cuar AutoMapperProfile se duoc tu dong add vao
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// Add IUnitOfWork and UnitOfWork
builder.Services.AddScoped<IUnitOfWork<ClaimRequestDbContext>, UnitOfWork<ClaimRequestDbContext>>();

// Add this line before registering your services
builder.Services.AddHttpContextAccessor();

// Registing some utils class
builder.Services.AddSingleton<JwtUtil>();


// Dependency Injection for Repositories and Services
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOtpService, OtpService>();
builder.Services.AddScoped<ICloudinaryService, CloudinaryService>();

//Serilize enum to string
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});



//Serilize enum to string
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});


// disable the default ModelStateInvalidFilter => to use the custom ExceptionHandlerMiddleware
// neu dinh chuong khong doc duoc loi tu swagger => comment lai doan code phia duoi
// ===============================================
//builder.Services.Configure<ApiBehaviorOptions>(options =>
//{
//    options.SuppressModelStateInvalidFilter = true;
//});
// ===============================================

// Add authentication
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
        
        // Add this to automatically prepend "Bearer " to the token
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Headers["Authorization"].FirstOrDefault();
                
                // If token exists but doesn't start with "Bearer "
                if (!string.IsNullOrEmpty(accessToken) && !accessToken.StartsWith("Bearer "))
                {
                    // Add "Bearer " prefix
                    context.Request.Headers["Authorization"] = "Bearer " + accessToken;
                }
                
                return Task.CompletedTask;
            },
            
            // Add custom response for unauthorized requests
            OnChallenge = async context =>
            {
                // Skip the default logic
                context.HandleResponse();
                
                // Set the response status code
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                
                // Create a custom response
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
                
                // Write the response
                await context.Response.WriteAsJsonAsync(response);
            }
        };
    });

// Add authorization with policies for different roles and operations
builder.Services.AddAuthorization(options =>
{
    options.AddClaimRequestPolicies();
});

// Update the Kestrel configuration
//builder.WebHost.ConfigureKestrel(serverOptions =>
//{
//    serverOptions.ListenAnyIP(5000); // HTTP
//    serverOptions.ListenAnyIP(5001, listenOptions =>
//    {
//        // In development/docker, we'll use HTTP instead of HTTPS
//        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
//    });
//});

var app = builder.Build();


// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Only apply migrations if explicitly enabled in configuration
    if (builder.Configuration.GetValue<bool>("ApplyMigrations", false))
    {
        app.ApplyMigrations();
    }

    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add the ExceptionHandlerMiddleware to the pipeline
// comment lai doan code phia duoi neu chuong khong doc duoc loi tu swagger
// ===============================================
app.UseMiddleware<ExceptionHandlerMiddleware>(); //comment lai de bat loi 500 
// ===============================================

app.UseHttpsRedirection();

app.UseCors(options =>
{
     options.SetIsOriginAllowed(origin => 
        origin.StartsWith("http://localhost:") || origin.StartsWith("https://localhost:"))
           .AllowAnyMethod()
           .AllowAnyHeader()
           .AllowCredentials();
});

app.UseAuthorization();

app.MapControllers();

app.Run();
