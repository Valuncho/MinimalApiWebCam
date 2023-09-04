using Serilog;
using Emgu.CV;

VideoCapture captureStream = null;
DateTime? lastCapture = null;
Task Warm()
{
    return Task.Run(() =>
    {
        Log.Information("Inicializando");
        captureStream = new VideoCapture(0);
        captureStream.Start();
        Log.Information("Captura Iniciada");
    });
}
async Task<byte[]> GetFrame()
{
    Log.Information("Captura requerida");
    var frame = captureStream.QuerySmallFrame();
    Log.Debug("Frame Capturado");
    lastCapture = DateTime.Now;
    var tempFile = "temp.jpg";
    frame.Save(tempFile);
    Log.Debug("Archivo temporal guardado");
    var content = await File.ReadAllBytesAsync(tempFile);
    File.Delete(tempFile);
    Log.Debug("Archivo temporal eliminado");
    return content;
}

var builder = WebApplication.CreateBuilder(args);
Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().ReadFrom.Configuration(builder.Configuration).CreateLogger();
builder.Host.UseSerilog(Log.Logger);

var app = builder.Build();
app.UseSerilogRequestLogging();

_ = Warm();
app.Map("/", () => new
{
    LastCapture = lastCapture
});
app.Map("/Frame", async () =>
{
    var content = await GetFrame();
    return Results.File(content, "image/jpeg");
});

app.Run();