using Microsoft.EntityFrameworkCore;
using FutureReady.Data;
using FutureReady.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add authentication (cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
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

// Add EF Core DbContext (SQL Server)
builder.Services.AddDbContext<FutureReady.Data.ApplicationDbContext>((serviceProvider, options) =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register application services
builder.Services.AddScoped<FutureReady.Services.Users.IUserService, FutureReady.Services.Users.UserService>();
builder.Services.AddScoped<FutureReady.Services.Schools.ISchoolService, FutureReady.Services.Schools.SchoolService>();
builder.Services.AddScoped<FutureReady.Services.Cohorts.ICohortService, FutureReady.Services.Cohorts.CohortService>();
builder.Services.AddScoped<FutureReady.Services.Authentication.IAuthenticationService, FutureReady.Services.Authentication.AuthenticationService>();
builder.Services.AddScoped<FutureReady.Services.Students.IStudentService, FutureReady.Services.Students.StudentService>();

var app = builder.Build();

// Seed database (run only if Schools table is empty)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<FutureReady.Data.ApplicationDbContext>();
        // If the Schools table exists and has any rows, skip seeding.
        // If the table doesn't exist (first run), the AnyAsync call will throw — fall through to run the seeder.
        var hasAny = await db.Schools.AnyAsync();
        if (!hasAny)
        {
            await FutureReady.Data.DatabaseSeeder.SeedAsync(services);
        }
    }
    catch (Exception)
    {
        // Likely the database/tables don't exist yet — run the seeder which will apply migrations and create the Admin school.
        await FutureReady.Data.DatabaseSeeder.SeedAsync(services);
    }
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

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
