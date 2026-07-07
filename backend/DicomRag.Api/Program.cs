using DicomRag.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddScoped<DicomExtractionService>();
builder.Services.AddHttpClient<OllamaService>();
builder.Services.AddHttpClient<VectorStoreService>();

// Dev-only CORS for the Vite React app. Lock this down before deploying anywhere
// that isn't your local machine.
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactDev", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("ReactDev");
//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
