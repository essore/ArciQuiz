using Core.Services;
using Infrasctructure.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;
using Web.Components;
using Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

var adminCredentials = builder.Configuration.GetSection(AdminCredentials.SectionName).Get<AdminCredentials>() ?? new AdminCredentials();
if (string.IsNullOrWhiteSpace(adminCredentials.Username) || string.IsNullOrWhiteSpace(adminCredentials.Password))
    throw new InvalidOperationException("Configurare Admin:Username e Admin:Password in appsettings.Local.json o nelle variabili d'ambiente.");

// Disabilita EventLog di Windows per evitare requisiti di privilegi di amministrazione a runtime
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

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
builder.Services.AddSingleton(adminCredentials);
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/login";
        options.Cookie.Name = "ArciQuiz.Admin";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (AdminAccessService.RequiresAuthentication(context.Request.Path)
        && !(context.User.Identity?.IsAuthenticated ?? false))
    {
        await context.ChallengeAsync();
        return;
    }

    await next();
});
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapGet("/login", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context).RequestToken;
    return Results.Content(LoginPage(token, null), "text/html; charset=utf-8");
});
app.MapPost("/login", async (HttpContext context, IAntiforgery antiforgery, AdminCredentials credentials) =>
{
    await antiforgery.ValidateRequestAsync(context);
    var form = await context.Request.ReadFormAsync();
    if (!AdminAccessService.IsValid(credentials, form["username"], form["password"]))
    {
        var token = antiforgery.GetAndStoreTokens(context).RequestToken;
        return Results.Content(LoginPage(token, "Credenziali non valide."), "text/html; charset=utf-8", statusCode: StatusCodes.Status401Unauthorized);
    }

    var claims = new[] { new Claim(ClaimTypes.Name, credentials.Username) };
    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
    return Results.Redirect("/admin");
});
app.MapGet("/logout", (HttpContext context, IAntiforgery antiforgery) =>
{
    var token = antiforgery.GetAndStoreTokens(context).RequestToken;
    return Results.Content(LogoutPage(token), "text/html; charset=utf-8");
});
app.MapPost("/logout", async (HttpContext context, IAntiforgery antiforgery) =>
{
    await antiforgery.ValidateRequestAsync(context);
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
});
app.MapGet("/api/domande/export", async (IDbContextFactory<ArciQuizDbContext> dbFactory) =>
{
    await using var db = await dbFactory.CreateDbContextAsync();
    var domande = await db.Domande
        .AsNoTracking()
        .Where(domanda => !domanda.FlgDeleted)
        .OrderBy(domanda => domanda.Id)
        .ToListAsync();

    var contenuto = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(DomandeCsvService.Export(domande))).ToArray();
    return Results.File(contenuto, "text/csv; charset=utf-8", "domande.csv");
});
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


app.Run();

static string LoginPage(string? antiforgeryToken, string? error)
{
    var errorMessage = string.IsNullOrWhiteSpace(error) ? string.Empty : $"<p role=\"alert\">{System.Net.WebUtility.HtmlEncode(error)}</p>";
    return $"""
        <!DOCTYPE html><html lang="it"><head><meta charset="utf-8"><title>Accesso amministratore</title></head>
        <body><main><h1>Accesso amministratore</h1>{errorMessage}
        <form method="post" action="/login"><input type="hidden" name="__RequestVerificationToken" value="{System.Net.WebUtility.HtmlEncode(antiforgeryToken)}">
        <label>Utente <input name="username" autocomplete="username" required></label><br>
        <label>Password <input type="password" name="password" autocomplete="current-password" required></label><br>
        <button type="submit">Accedi</button></form></main></body></html>
        """;
}

static string LogoutPage(string? antiforgeryToken) => $"""
    <!DOCTYPE html><html lang="it"><head><meta charset="utf-8"><title>Esci</title></head>
    <body><main><h1>Esci dall'area amministrativa</h1><form method="post" action="/logout">
    <input type="hidden" name="__RequestVerificationToken" value="{System.Net.WebUtility.HtmlEncode(antiforgeryToken)}">
    <button type="submit">Esci</button></form></main></body></html>
    """;
