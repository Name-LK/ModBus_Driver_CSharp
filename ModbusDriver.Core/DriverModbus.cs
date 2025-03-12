using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text.Json;
using ModbusDriver.Core.Interfaces;
using NModbus;

namespace DriverModbus.Driver
{
    public class DriverModBus : IDisposable
    {

        private const string Host = "127.0.0.1";
        private const int Port = 502;

        private readonly Dictionary<string, VariableConfig> _variables = new()
        {
            { "Temperature-01", new VariableConfig("5", "int") },
            { "PumpStatus-01", new VariableConfig("6", "bool") },
            { "Pressure-01", new VariableConfig("9", "int") },
            { "Temperature-02", new VariableConfig("6", "int") },
            { "PumpStatus-02", new VariableConfig("6", "bool") },
            { "Pressure-02", new VariableConfig("6", "int") }
        };

        private TcpClient? _client;
        private IModbusMaster? _master;


        public void Connect()
        {
            try
            {
                Console.WriteLine($"\nConnecting to Modbus device at {Host}:{Port}\n");
                _client = new TcpClient(Host, Port);
                var factory = new ModbusFactory();
                _master = factory.CreateMaster(_client);
                Console.WriteLine("\nDevice Connected Successfully.\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
            }
        }


        public void Disconnect()
        {
            if (_client != null && _client.Connected)
            {
                _client.Close();
            }
            Console.WriteLine("\nDisconnected.\n");
        }


        private void EnsureConnected()
        {
            if (_client == null || !_client.Connected)
            {
                Console.WriteLine("Device is not connected. Reconnecting...");
                Disconnect();
                Connect();
            }

            if (_master == null)
            {
                throw new InvalidOperationException("Modbus master is not initialized.");
            }
        }

        public void SetData(Dictionary<string, object> variableValues)
        {
            EnsureConnected();

            foreach (var entry in variableValues)
            {
                string variableName = entry.Key;
                object value = entry.Value;

                if (!_variables.TryGetValue(variableName, out var variableConfig))
                {
                    throw new ArgumentException($"Variable '{variableName}' not found in configuration.");
                }

                ushort registerAddress = ushort.Parse(variableConfig.Register);
                string type = variableConfig.Type;

                try
                {
                    switch (type.ToLower())
                    {
                        case "bool":
                            bool boolValue = Convert.ToBoolean(value);
                            _master.WriteSingleCoil(1, registerAddress, boolValue);
                            Console.WriteLine($"Escrito {boolValue} no endereço {registerAddress} (Coil)");
                            break;

                        case "int":
                            int intValue = Convert.ToInt32(value);
                            _master.WriteSingleRegister(1, registerAddress, (ushort)intValue);
                            Console.WriteLine($"Escrito {intValue} no endereço {registerAddress} (Register)");
                            break;

                        default:
                            throw new ArgumentException($"Unsupported type: {type}");
                    }
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Erro ao escrever o valor para a variável '{variableName}': {ex.Message}");
                }
            }
        }

        public JsonElement GetData(IEnumerable<string> variableNames)
        {
            EnsureConnected();

            var resultArray = new List<JsonElement>();

            foreach (var variableName in variableNames)
            {
                if (!_variables.TryGetValue(variableName, out var variableConfig))
                {
                    throw new ArgumentException($"Variable '{variableName}' not found in configuration.");
                }

                ushort registerAddress = ushort.Parse(variableConfig.Register);
                string type = variableConfig.Type;

                object value = type switch
                {
                    "bool" => _master.ReadCoils(1, registerAddress, 1)[0],
                    "int" => _master.ReadHoldingRegisters(1, registerAddress, 1)[0],
                    _ => throw new ArgumentException($"Unsupported type: {type}")
                };


                var jsonObject = JsonSerializer.SerializeToElement(new
                {
                    VariableName = variableName,
                    Type = type,
                    Value = value
                });

                resultArray.Add(jsonObject);
            }


            return JsonSerializer.SerializeToElement(resultArray);
        }


        public void Dispose()
        {
            //Disconnect();
            _client.Close();
            _client?.Dispose();
        }

        public Task<JsonElement> GetTelemetry()
        {
            EnsureConnected();
            var data = GetData(_variables.Keys);
            return Task.FromResult(data);

        }
    }


    public class VariableConfig
    {
        public string Register { get; set; }
        public string Type { get; set; }

        public VariableConfig(string register, string type)
        {
            Register = register;
            Type = type;
        }
    }
}