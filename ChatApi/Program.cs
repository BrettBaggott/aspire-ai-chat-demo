using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

builder.AddServiceDefaults();

builder.Services.AddSignalR();
builder.Services.AddSingleton<ChatStreamingCoordinator>();
builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();
builder.Services.AddSingleton<IConversationState, InMemoryConversationState>();
builder.Services.AddSingleton<ICancellationManager, InMemoryCancellationManager>();
builder.Services.AddSingleton<IKipperbitRunner, KipperbitRunner>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Map OpenAPI and Scalar
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();

app.MapChatApi();

app.Run();
