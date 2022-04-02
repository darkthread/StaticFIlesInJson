using Guineapig.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions {
    FileProvider = new StaticFileJsonProvider(
        Path.Combine(app.Environment.ContentRootPath, "StaticFiles.json")),
    RequestPath = "/web-in-json"
});

app.MapGet("/", () => Results.Redirect("~/web-in-json/index.html"));

app.Run();
