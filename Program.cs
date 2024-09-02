using static HWHash;
using System.IO.Ports;
using YamlDotNet.Serialization;
using System.Linq.Expressions;

class Program
{
    public class Config
    {
        public bool HighPrecision { get; set; }
        public bool HighPriority { get; set; }
        public int Delay { get; set; }
        public required List<string> Sensors { get; set; }
        public required string PortName { get; set; }
        public int BaudRate { get; set; }
    }

    static void Main(string[] args)
    {
        var configContent = File.ReadAllText(Directory.GetCurrentDirectory() + "\\config.yaml");
        var deserializer = new DeserializerBuilder().Build();
        Config config = deserializer.Deserialize<Config>(configContent);

        HWHash.HighPrecision = config.HighPrecision;
        HWHash.HighPriority = config.HighPriority;
        HWHash.SetDelay(config.Delay);
        bool launch = HWHash.Launch();

        bool SEND_STATUS = false;
        List<string> sensors = config.Sensors;

        SerialPort serialPort = new SerialPort(config.PortName, config.BaudRate);

        try
        {
            serialPort.Open();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
        }

        try
        {
            while (launch)
            {
                List<HWINFO_HASH_MINI> _HWHashOrderedListMini = GetOrderedListMini();

                if (_HWHashOrderedListMini == null || !_HWHashOrderedListMini.Any())
                {

                    Console.WriteLine(
                        "No sensor data available.\n" + 
                        "Please start HWiNFO and turn on Shared Memory before starting this program.\n" +
                        "Exiting...\n"
                    );

                    return;
                }

                else
                {
                    List<string> values = new List<string>();
                    List<HWINFO_HASH_MINI> commonSensorItems = _HWHashOrderedListMini.Where(item => sensors.Contains(item.NameCustom)).ToList();

                    foreach (var sensorItem in commonSensorItems)
                    {
                        string sensorValue = Math.Round(sensorItem.ValueNow).ToString();
                        values.Add(sensorValue);
                    }

                    string result = string.Join("-", values);

                    try
                    {
                        serialPort.WriteLine(result);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        serialPort.Close();
                    }

                    if (!SEND_STATUS)
                    {
                        Console.WriteLine($"Sending data to {config.PortName}...");
                        SEND_STATUS = true;
                    }

                    Thread.Sleep(config.Delay);
                }
            }
        }
        finally
        {
            Console.WriteLine("Exiting...");
            serialPort.Close();
        }
    }
}
