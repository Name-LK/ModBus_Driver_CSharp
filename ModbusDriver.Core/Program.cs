using DriverModbus.Driver;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

/*
O programa altera os valores dos dispositivos [Temperature-01, PumpStatus-01, Pressure-01]
Chama o get data passando os 3 dispositivos numa lista de strings
Chama o método GetTelemetry() que reotorna os ultimos dados capturados de todos os dispositivos
*/
class Program
{
    static async Task Main(string[] args)
    {
        using var driver = new DriverModBus();
        driver.Connect();

        try
        {
            var variableValues = new Dictionary<string, object>
            {
                { "Temperature-01", 38 },
                { "PumpStatus-01", true },
                { "Pressure-01", 123 }
            };

            driver.SetData(variableValues);

            var variableNames = new List<string> { "Temperature-01", "PumpStatus-01", "Pressure-01" };

            Console.WriteLine("\nChamando GetData()\n");
            JsonElement telemetryData = driver.GetData(variableNames);
            Console.WriteLine(telemetryData.GetRawText());

            Console.WriteLine("\nChamando GetTelemetry()");
            JsonElement data = await driver.GetTelemetry();
            Console.WriteLine(data.ToString());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            driver.Disconnect();
        }
    }
}