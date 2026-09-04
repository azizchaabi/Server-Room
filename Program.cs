using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServerRoomMonitor.Data;
using ServerRoomMonitor.Services;
using ServerRoomMonitor.ML;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
options.Conventions.AuthorizeFolder("/");


options.Conventions.AllowAnonymousToPage("/Privacy");


});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
options.UseSqlServer(
builder.Configuration.GetConnectionString("DefaultConnection")));

// Email notification service.
builder.Services.AddScoped<EmailNotificationService>();

// Report PDF service.
builder.Services.AddScoped<ReportPdfService>();

builder.Services.AddScoped<PredictiveDataGeneratorService>();

builder.Services.AddScoped<PredictiveMaintenanceModelTrainer>();

builder.Services.AddScoped<PredictiveMaintenanceModelTuning>();

builder.Services.AddScoped<PredictiveMaintenancePredictionService>();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
{
options.SignIn.RequireConfirmedAccount = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Inspection reminder background service.
builder.Services.AddHostedService<InspectionReminderService>();

builder.Services.AddScoped<InspectionPdfService>();

var app = builder.Build();

// Create default roles.
using (var scope = app.Services.CreateScope())
{
var roleManager = scope.ServiceProvider
.GetRequiredService<RoleManager<IdentityRole>>();


string[] roles =
{
    "Admin",
    "Technician"
};

foreach (var role in roles)
{
    if (!await roleManager.RoleExistsAsync(role))
    {
        await roleManager.CreateAsync(
            new IdentityRole(role));
    }
}


}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
app.UseExceptionHandler("/Error");
app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapRazorPages()
.WithStaticAssets();

app.Run();
