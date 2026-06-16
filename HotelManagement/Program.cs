using HotelManagement.Data;
using HotelManagement.Services;
using HotelManagement.Services.Admin;
using HotelManagement.Services.Receptionist;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

var databaseProvider = builder.Configuration["DatabaseProvider"] ?? "SqlServer";
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<HotelDbContext>(options =>
{
    if (databaseProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
    {
        var mySqlVersion = Version.Parse(builder.Configuration["MySql:Version"] ?? "8.0.0");
        options.UseMySql(connectionString, new MySqlServerVersion(mySqlVersion));
        return;
    }

    options.UseSqlServer(connectionString);
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AdminDashboardService>();
builder.Services.AddScoped<RoomTypeManagementService>();
builder.Services.AddScoped<RoomManagementService>();
builder.Services.AddScoped<ServiceManagementService>();
builder.Services.AddScoped<EmployeeManagementService>();
builder.Services.AddScoped<CustomerManagementService>();
builder.Services.AddScoped<BookingTrackingService>();
builder.Services.AddScoped<InvoiceTrackingService>();
builder.Services.AddScoped<RevenueReportService>();
builder.Services.AddScoped<ActivityLogService>();
builder.Services.AddScoped<ReceptionistDashboardService>();
builder.Services.AddScoped<ReceptionistBookingService>();
builder.Services.AddScoped<ReceptionistInvoiceService>();

// Add services to the container.
builder.Services.AddControllersWithViews();

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var isGoogleAuthenticationConfigured =
    !string.IsNullOrWhiteSpace(googleClientId)
    && !string.IsNullOrWhiteSpace(googleClientSecret);

var authenticationBuilder = builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    })
    .AddCookie("External");

if (isGoogleAuthenticationConfigured)
{
    authenticationBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.CallbackPath = "/signin-google";
        options.SignInScheme = "External";
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<HotelDbContext>();
    var configuredProvider = scope.ServiceProvider
        .GetRequiredService<IConfiguration>()["DatabaseProvider"] ?? "SqlServer";

    if (configuredProvider.Equals("MySql", StringComparison.OrdinalIgnoreCase))
    {
        context.Database.EnsureCreated();

        if (!TableExists(context, "Users"))
        {
            var databaseCreator = context.GetService<IRelationalDatabaseCreator>();
            databaseCreator.CreateTables();
        }
    }
    else
    {
        context.Database.Migrate();
    }

    SeedData.Initialize(context);
}
static bool TableExists(HotelDbContext context, string tableName)
{
    var connection = context.Database.GetDbConnection();
    var shouldClose = connection.State == System.Data.ConnectionState.Closed;

    if (shouldClose)
    {
        connection.Open();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM information_schema.tables
            WHERE table_schema = DATABASE()
              AND LOWER(table_name) = LOWER(@tableName)
            """;

        var parameter = command.CreateParameter();
        parameter.ParameterName = "@tableName";
        parameter.Value = tableName;
        command.Parameters.Add(parameter);

        var result = command.ExecuteScalar();
        return Convert.ToInt32(result) > 0;
    }
    finally
    {
        if (shouldClose)
        {
            connection.Close();
        }
    }
}


app.Run();
