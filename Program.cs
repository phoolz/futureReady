using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Apiary.Data;
using Apiary.Models;
using Apiary.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Add EF Core DbContext (SQL Server)
builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

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
    options.AccessDeniedPath = "/Error/AccessDenied";
    options.Cookie.Name = "ApiaryAuth";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

// Register IHttpContextAccessor so UserProvider can read the current user
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IUserProvider, HttpContextUserProvider>();
builder.Services.AddScoped<ITenantProvider, HttpContextTenantProvider>();

// Register application services
builder.Services.AddScoped<Apiary.Services.Schools.ISchoolService, Apiary.Services.Schools.SchoolService>();
builder.Services.AddScoped<Apiary.Services.Students.IStudentService, Apiary.Services.Students.StudentService>();
builder.Services.AddScoped<Apiary.Services.Students.IStudentAuthorizationService, Apiary.Services.Students.StudentAuthorizationService>();
builder.Services.AddScoped<Apiary.Services.Students.IStudentBulkUploadService, Apiary.Services.Students.StudentBulkUploadService>();
builder.Services.AddScoped<Apiary.Services.EmergencyContacts.IEmergencyContactService, Apiary.Services.EmergencyContacts.EmergencyContactService>();
builder.Services.AddScoped<Apiary.Services.StudentMedicalConditions.IStudentMedicalConditionService, Apiary.Services.StudentMedicalConditions.StudentMedicalConditionService>();
builder.Services.AddScoped<Apiary.Services.Companies.ICompanyService, Apiary.Services.Companies.CompanyService>();
builder.Services.AddScoped<Apiary.Services.Supervisors.ISupervisorService, Apiary.Services.Supervisors.SupervisorService>();
builder.Services.AddScoped<Apiary.Services.Placements.IPlacementService, Apiary.Services.Placements.PlacementService>();
builder.Services.AddScoped<Apiary.Services.PlacementStudents.IPlacementStudentService, Apiary.Services.PlacementStudents.PlacementStudentService>();
builder.Services.AddScoped<Apiary.Services.FormTokens.IFormTokenService, Apiary.Services.FormTokens.FormTokenService>();
builder.Services.AddScoped<Apiary.Services.EmployerForm.IEmployerFormService, Apiary.Services.EmployerForm.EmployerFormService>();
builder.Services.AddScoped<Apiary.Services.EmployerForm.IEmployerFormStateService, Apiary.Services.EmployerForm.EmployerFormStateService>();
builder.Services.AddScoped<Apiary.Services.ParentForm.IParentFormService, Apiary.Services.ParentForm.ParentFormService>();
builder.Services.AddScoped<Apiary.Services.ParentForm.IParentFormStateService, Apiary.Services.ParentForm.ParentFormStateService>();
builder.Services.AddScoped<Apiary.Services.LogbookEvaluations.ILogbookEvaluationService, Apiary.Services.LogbookEvaluations.LogbookEvaluationService>();
builder.Services.AddScoped<Apiary.Services.LogbookEntries.ILogbookEntryService, Apiary.Services.LogbookEntries.LogbookEntryService>();
builder.Services.AddScoped<Apiary.Services.LogbookTasks.ILogbookTaskService, Apiary.Services.LogbookTasks.LogbookTaskService>();
builder.Services.AddScoped<Apiary.Services.StudentAccountTokens.IStudentAccountTokenService, Apiary.Services.StudentAccountTokens.StudentAccountTokenService>();
builder.Services.AddScoped<Apiary.Services.StudentActivation.IStudentActivationService, Apiary.Services.StudentActivation.StudentActivationService>();

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

app.MapRazorComponents<Apiary.Components.App>()
    .AddInteractiveServerRenderMode();

app.Run();
