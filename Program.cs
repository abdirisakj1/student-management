using Tourism_Management.Models;
using Tourism_Management.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection("MongoSettings"));
var mongoSettings = builder.Configuration.GetSection("MongoSettings").Get<MongoSettings>() ?? new MongoSettings();

builder.Services.AddSingleton(mongoSettings);
builder.Services.AddSingleton<ITourismUserService, TourismUserService>();
builder.Services.AddControllersWithViews();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// If you don't run with an HTTPS listener, this warning can appear:
// "Failed to determine the https port for redirect." 
// Either set HTTPS endpoint (launchSettings / Kestrel config) or disable this line.
// app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Users}/{action=Index}/{id?}");

app.Run();
