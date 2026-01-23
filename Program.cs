using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using FutureReady.Data;
using FutureReady.Models;
using FutureReady.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add EF Core DbContext (SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configure application cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Authentication/Login";
    options.LogoutPath = "/Authentication/Logout";
    options.Cookie.Name = "FutureReadyAuth";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// Register IHttpContextAccessor so UserProvider can read the current user
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserProvider, HttpContextUserProvider>();
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();

// Register application services
builder.Services.AddScoped<FutureReady.Services.Schools.ISchoolService, FutureReady.Services.Schools.SchoolService>();
builder.Services.AddScoped<FutureReady.Services.Students.IStudentService, FutureReady.Services.Students.StudentService>();
builder.Services.AddScoped<FutureReady.Services.EmergencyContacts.IEmergencyContactService, FutureReady.Services.EmergencyContacts.EmergencyContactService>();
builder.Services.AddScoped<FutureReady.Services.StudentMedicalConditions.IStudentMedicalConditionService, FutureReady.Services.StudentMedicalConditions.StudentMedicalConditionService>();
builder.Services.AddScoped<FutureReady.Services.Companies.ICompanyService, FutureReady.Services.Companies.CompanyService>();
builder.Services.AddScoped<FutureReady.Services.Supervisors.ISupervisorService, FutureReady.Services.Supervisors.SupervisorService>();
builder.Services.AddScoped<FutureReady.Services.Placements.IPlacementService, FutureReady.Services.Placements.PlacementService>();
builder.Services.AddScoped<FutureReady.Services.FormTokens.IFormTokenService, FutureReady.Services.FormTokens.FormTokenService>();
builder.Services.AddScoped<FutureReady.Services.EmployerForm.IEmployerFormService, FutureReady.Services.EmployerForm.EmployerFormService>();
builder.Services.AddScoped<FutureReady.Services.EmployerForm.IEmployerFormStateService, FutureReady.Services.EmployerForm.EmployerFormStateService>();
builder.Services.AddScoped<FutureReady.Services.ParentForm.IParentFormService, FutureReady.Services.ParentForm.ParentFormService>();
builder.Services.AddScoped<FutureReady.Services.ParentForm.IParentFormStateService, FutureReady.Services.ParentForm.ParentFormStateService>();
builder.Services.AddScoped<FutureReady.Services.LogbookEvaluations.ILogbookEvaluationService, FutureReady.Services.LogbookEvaluations.LogbookEvaluationService>();
builder.Services.AddScoped<FutureReady.Services.StudentWorkHistories.IStudentWorkHistoryService, FutureReady.Services.StudentWorkHistories.StudentWorkHistoryService>();
builder.Services.AddScoped<FutureReady.Services.LogbookEntries.ILogbookEntryService, FutureReady.Services.LogbookEntries.LogbookEntryService>();
builder.Services.AddScoped<FutureReady.Services.LogbookTasks.ILogbookTaskService, FutureReady.Services.LogbookTasks.LogbookTaskService>();

var app = builder.Build();

// Seed database (always run - seeder is idempotent)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await DatabaseSeeder.SeedAsync(services);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Authentication must come before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapRazorComponents<FutureReady.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
