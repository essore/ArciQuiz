using Infrasctructure.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Web.Components;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);
var databaseFileName = builder.Configuration["Database:FileName"] ?? "arciquiz.db";
var dataDirectory = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "ArciQuiz");
Directory.CreateDirectory(dataDirectory);

var connectionString = new SqliteConnectionStringBuilder
{
    DataSource = Path.Combine(dataDirectory, databaseFileName)
}.ToString();

builder.Services.AddDbContextFactory<ArciQuizDbContext>(opt =>
{
    opt.UseSqlite(connectionString);
});

// Add services to the container.
builder.Services.AddSingleton<IGameStateService, GameStateService>();
builder.Services.AddSingleton<ILanAddressService, LanAddressService>();
builder.Services.AddSingleton<IQrCodeService, QrCodeService>();


builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();




// Applica lo schema persistente prima di usare il database.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ArciQuizDbContext>();
    db.Database.Migrate();

    if (app.Environment.IsDevelopment())
    {
        ArciQuizDbInitializer.Seed(db);
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
else
{

    // === Apertura automatica browser ===
    //app.Lifetime.ApplicationStarted.Register(() =>
    //{
    //    try
    //    {
    //        // Usa l'URL effettivamente in ascolto
    //        // (è lo stesso che usa Visual Studio/launchSettings)
    //        var url = app.Urls.FirstOrDefault() ?? "http://localhost:5000";

    //        Process.Start(new ProcessStartInfo
    //        {
    //            FileName = url,
    //            UseShellExecute = true
    //        });
    //    }
    //    catch
    //    {
    //        // Se non riesce ad aprire il browser, non blocchiamo l'app
    //    }
    //});
    // ================================

}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();
