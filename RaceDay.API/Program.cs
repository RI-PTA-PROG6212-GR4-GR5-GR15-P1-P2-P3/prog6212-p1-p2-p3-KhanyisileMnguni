using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using RaceDay.API.Data;
using RaceDay.API.Services;
using System.Reflection;
using System.Security.Claims;
using System.Text;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddSingleton<PasswordService>();
builder.Services.AddControllers();

// ---- JWT authentication ----
// Login returns a signed token carrying the user's ID and role. The client sends it as
// "Authorization: Bearer <token>" and the API rebuilds the user (and role) from it on every request.
var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
          ?? throw new InvalidOperationException("Missing 'Jwt' section in configuration.");
if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters long.");

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
   {
   options.MapInboundClaims = false;   // keep claim names exactly as issued
       options.Events = new JwtBearerEvents
       {
           OnAuthenticationFailed = context =>
           {
               Console.WriteLine(
                   $"JWT ERROR: {context.Exception.Message}");

               return Task.CompletedTask;
           }
       };

       options.TokenValidationParameters = new TokenValidationParameters
       {
           ValidateIssuer = true,
           ValidIssuer = jwt.Issuer,
           ValidateAudience = true,
           ValidAudience = jwt.Audience,
           ValidateIssuerSigningKey = true,
           IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
           ValidateLifetime = true,
           ClockSkew = TimeSpan.FromMinutes(1),
           NameClaimType = ClaimTypes.Name,
           RoleClaimType = ClaimTypes.Role
       };
   });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "RaceDay.API",
        Version = "v1",
        Description = "RESTful API for the RaceDay event management system. " +
                      "Register, log in, then click Authorize and paste the returned JWT token."
    });

    // Adds the Authorize button so protected endpoints can be tested straight from Swagger.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Paste the token returned by POST /api/auth/login (no 'Bearer ' prefix needed).",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
    });

    var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) options.IncludeXmlComments(xml);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

// Makes Program visible to the test project (WebApplicationFactory<Program>).
public partial class Program { }