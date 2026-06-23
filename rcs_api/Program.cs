using rcs_api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<MissionQueueService>();
builder.Services.AddSingleton<RosbridgeClientService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<RosbridgeClientService>());

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();
