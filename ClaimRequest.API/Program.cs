using ClaimRequest.DAL.Data.Entities;
using ClaimRequest.API.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using ClaimRequest.DAL.Repositories.Interfaces;
using ClaimRequest.DAL.Repositories.Implements;
using ClaimRequest.BLL.Services.Interfaces;
using ClaimRequest.BLL.Services.Implements;
using ClaimRequest.API.Middlewares;
using System.Text.Json.Serialization;

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
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection"),
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

// Dependency Injection for Repositories and Services
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IStaffService, StaffService>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IEmailService, EmailService>();

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
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])),
        };
    });

// Update the Kestrel configuration
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenAnyIP(5000); // HTTP
    serverOptions.ListenAnyIP(5001, listenOptions =>
    {
        // In development/docker, we'll use HTTP instead of HTTPS
        listenOptions.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1AndHttp2;
    });
});

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
//app.UseMiddleware<ExceptionHandlerMiddleware>();
// ===============================================

//app.UseHttpsRedirection();

app.UseCors(options =>
{
    options.AllowAnyOrigin();
    options.AllowAnyMethod();
    options.AllowAnyHeader();
});

app.UseAuthorization();

app.MapControllers();

app.Run();
