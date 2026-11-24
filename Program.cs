using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;

Console.WriteLine("================================");
Console.WriteLine("   API STRESS TEST");
Console.WriteLine("================================");

//var url = "https://localhost:44308/api/v1/ConsultaExcesos/ConsultaExcesos";
var url = "https://localhost:44349/api/v1/PensionesPagadas/PensionesPagadas";

var token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJJZFVzdWFyaW8iOiJTTkRQUk9EVUNUTyIsIk5vbWJyZSI6IkdSVVBPIFBST0RVQ1RPIiwiRXN0YWRvIjoiVkkiLCJSdXRDbGllbnRlIjoiMDAwMDAwMDc1MDQ3OTEiLCJSdXRDbGllbnRlRFYiOiI2IiwibmJmIjoxNzk0OTQxMTU1LCJleHAiOjE3OTQ5NDI5NTUsImlhdCI6MTc5NDk0MTE1NSwiaXNzIjoiU29uZGEgUy5BLiIsImF1ZCI6Imh0dHA6Ly93d3cuc29uZGEuY29tIn0.qeYphSK0hes4H8nxeV7cwLPUyzCgs-j75FyOlE1oad8";
var handler = new HttpClientHandler();

// Como usamos localhost con certificado de desarrollo
handler.ServerCertificateCustomValidationCallback =
    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

using var client = new HttpClient(handler);

client.Timeout = TimeSpan.FromSeconds(30);

int solicitudesConcurrentes = 50; // Aqui pones la cantidad de request que necesitas

int exitosas = 0;
int fallidas = 0;

var tiempos = new List<long>();

async Task EnviarSolicitud(int numero)
{
    //var json = """
    //{
    //    "RutCliente": null,
    //    "RutClienteDV": null
    //}
    //""";
    var json = """
    {
        "perInicio": "01/2023",
        "perFinal": "10/2023"
    }
    """;

    var content = new StringContent(
        json,
        Encoding.UTF8,
        "application/json");

    var sw = Stopwatch.StartNew();

    try
    {
        var response = await client.PostAsync(url, content);

        sw.Stop();

        lock (tiempos)
        {
            tiempos.Add(sw.ElapsedMilliseconds);
        }

        if (response.IsSuccessStatusCode)
        {
            Interlocked.Increment(ref exitosas);
            var respuesta = await response.Content.ReadAsStringAsync();

            Console.WriteLine(
                $"Solicitud {numero} -> {(int)response.StatusCode} ({sw.ElapsedMilliseconds} ms)");

            //Console.WriteLine(respuesta); esta linea da mas detalle de la respuesta ejemplo detalle del sttaus obtenido
        }
        else
        {
            Interlocked.Increment(ref fallidas);

            Console.WriteLine(
                $"Solicitud {numero} -> ERROR {(int)response.StatusCode}");
        }
    }
    catch (Exception ex)
    {
        sw.Stop();

        Interlocked.Increment(ref fallidas);

        Console.WriteLine(
            $"Solicitud {numero} -> EXCEPCIÓN {ex.Message}");
    }
}

client.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);

Console.WriteLine("Cliente creado correctamente.");

Console.WriteLine();
Console.WriteLine($"Iniciando {solicitudesConcurrentes} solicitudes concurrentes...");
Console.WriteLine();

var cronometroGeneral = Stopwatch.StartNew();

var tareas = new List<Task>();

for (int i = 1; i <= solicitudesConcurrentes; i++)
{
    tareas.Add(EnviarSolicitud(i));
}

await Task.WhenAll(tareas);

cronometroGeneral.Stop();

Console.WriteLine();
Console.WriteLine("=========================================");
Console.WriteLine("      RESUMEN DE LA PRUEBA");
Console.WriteLine("=========================================");

Console.WriteLine($"Solicitudes concurrentes : {solicitudesConcurrentes}");

Console.WriteLine($"Exitosas : {exitosas}");

Console.WriteLine($"Fallidas : {fallidas}");

Console.WriteLine($"Tiempo promedio : {tiempos.Average():F2} ms");

Console.WriteLine($"Tiempo mínimo : {tiempos.Min()} ms");

Console.WriteLine($"Tiempo máximo : {tiempos.Max()} ms");

Console.WriteLine($"Duración total : {cronometroGeneral.Elapsed}");
