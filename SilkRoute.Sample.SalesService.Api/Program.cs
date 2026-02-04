using SilkRoute.Public.Extensions;
using SilkRoute.Public.InputFormatters;
using SilkRoute.Public.Options;
using SilkRoute.Sample.Contracts.MicroserviceClients;

var builder = WebApplication.CreateBuilder(args);

var billingClientOptions = new MicroserviceClientOptions
{
    HttpClientConfiguration = client =>
    {
        client.BaseAddress = new Uri("https://localhost:7072");
    }
};

builder.Services.AddMicroserviceClient<IBillingMicroserviceClient>(billingClientOptions);

builder.Services
    .AddControllers(options =>
    {
        options.InputFormatters.Insert(0, new BinaryBodyInputFormatter());
    })
    .AddNewtonsoftJson();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();



