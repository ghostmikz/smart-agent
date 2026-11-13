var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddWindowsService(o => o.ServiceName = "Smart Agent Worker");
builder.Services.AddHostedService<WorkerHealthCheck>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
         policy.AllowAnyOrigin()
               .AllowAnyHeader()
               .AllowAnyMethod());
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseRouting();

app.MapStaticAssets();
app.MapRazorPages().WithStaticAssets();
app.MapGet("/health", () => "OK");
app.MapControllers();
app.Run();
