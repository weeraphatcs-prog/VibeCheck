using KahootClone.Components;
using KahootClone.Hubs;
using KahootClone.Models;
using KahootClone.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();
builder.Services.AddSingleton<IGameService, GameService>();
builder.Services.AddSingleton<TimerService>();

var app = builder.Build();

// Seed demo quiz
{
    var svc = app.Services.GetRequiredService<IGameService>();
    svc.CreateQuiz("Demo Quiz", new[]
    {
        new QuizQuestionDto("What is 2 + 2?", new List<QuizOption>
        {
            new("3", false), new("4", true), new("5", false), new("6", false),
        }, TimeLimitSec: 15),
        new QuizQuestionDto("Capital of France?", new List<QuizOption>
        {
            new("London", false), new("Berlin", false), new("Paris", true), new("Madrid", false),
        }, TimeLimitSec: 20),
        new QuizQuestionDto("Which language runs natively in browsers?", new List<QuizOption>
        {
            new("Python", false), new("Java", false), new("C#", false), new("JavaScript", true),
        }, TimeLimitSec: 20),
    });
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapHub<GameHub>("/gamehub");

app.Run();
