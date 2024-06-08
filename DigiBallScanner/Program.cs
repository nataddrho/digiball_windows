using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using System.Threading;
using Windows.Storage.Streams;
using System.Collections.Generic;

public class Variables
{
    static int balls = 0;
}

class Program
{

    public static int balls = 0;
    public static String[] macAddresses = { "", "" };

    static async Task Main(string[] args)
    {
        
        String usage = "Usage: xxxxxx yyyyyy\nxxxxxx: Least significant 3 bytes (hex) of DigiBall MAC address.\nyyyyyy (Optional): Least significant 3 bytes (hex) of DigiBall MAC address of second player's ball.";
        int i = 0;
        foreach (string arg in args)
        {
            switch (i)
            {
                case 0:
                    if (arg == "help" || arg == "-help" || arg == "--help" || arg =="-h")
                    {
                        Console.WriteLine(usage);
                        return;
                    } else if (arg.Length!=6)
                    {
                        Console.WriteLine(usage);
                        return;
                    } else
                    {
                        balls = 1;
                        
                    }
                    break;
                case 1:
                    if (arg.Length != 6)
                    {
                        Console.WriteLine(usage);
                        return;
                    }
                    else
                    {
                        balls = 2;
                    }
                    break;                
            }            
            i++;
        }

        

        var watcher = new BluetoothLEAdvertisementWatcher();
        watcher = new BluetoothLEAdvertisementWatcher()
        {
            ScanningMode = BluetoothLEScanningMode.Passive
        };

        var manufacturerData = new BluetoothLEManufacturerData();
        manufacturerData.CompanyId = 0x03DE; //Nathan Rhoades LLC       
        watcher.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);

        watcher.Received += Watcher_Received;
        watcher.Start();

        await Task.Delay(Timeout.Infinite);
    }

    private static async void Watcher_Received(
        BluetoothLEAdvertisementWatcher watcher,
        BluetoothLEAdvertisementReceivedEventArgs eventArgs)
    {
        var device = await BluetoothLEDevice.FromBluetoothAddressAsync(eventArgs.BluetoothAddress);       

        if (device != null)
        {
            //Console.WriteLine("{0:X}", device.BluetoothAddress);
            var manufacturerSections = eventArgs.Advertisement.ManufacturerData;        
            if (manufacturerSections.Count > 0)
            {
                // Only print the first one of the list
                var manufacturerData = manufacturerSections[0];
                var data = new byte[manufacturerData.Data.Length];
                using (var reader = DataReader.FromBuffer(manufacturerData.Data))
                {
                    reader.ReadBytes(data);
                }
                // Print the company ID + the raw data in hex format
                String manufacturerDataString = string.Format("{0:X}:{1}",
                    device.BluetoothAddress,
                    BitConverter.ToString(data));

                Console.WriteLine(manufacturerDataString);
            }
        }

    }
}