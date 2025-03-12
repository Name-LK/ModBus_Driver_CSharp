using System.Text.Json;

namespace ModbusDriver.Core.Interfaces
{
    public interface IModBus
    {
        void Connect();
        void Disconnect();
        void EnsureConnected();
        Task<JsonElement?> GetTelemetry(); // Falta implementar
        object GetData(string variable);
        void SetData(string variable, string data);
    }

}