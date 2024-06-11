using System;
using System.Threading.Tasks;
using System.IO;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using System.Threading;
using Windows.Storage.Streams;
using System.Collections.Generic;
using Windows.Devices.Sensors;
using Windows.UI.Xaml.Shapes;


public class Variables
{
    static int balls = 0;
}

class Program
{   
    public static String filterShortMac = "";
    public static int lastShotNumber = -1;

    static async Task Main(string[] args)
    {
        
        String usage = "Usage: xxxxxx \nxxxxxx: Least significant 3 bytes (hex) of DigiBall MAC address.";
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
                        filterShortMac = arg.ToUpper();                        
                    }
                    break;                
            }            
            i++;
        }  
        
        if (i==0)
        {
            Console.WriteLine(usage);
            return;
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
                String manufacturerConsoleString = string.Format("{0:X}:{1}",
                    device.BluetoothAddress,
                    BitConverter.ToString(data));

                //Console.WriteLine(manufacturerConsoleString);

                //Parse data
                // Print the company ID + the raw data in hex format
                String shortMac = BitConverter.ToString(data).Replace("-", string.Empty).Substring(0, 6);
                if (filterShortMac==shortMac)
                {
                    if (data.Length == 24) {
                        int deviceType = data[3];
                        if (deviceType == 1)
                        {
                            bool dataReady = (data[17] >> 6) == 1;
                            int shotNumber = data[6] & 0x3F;
                            bool highGAccelAvailable = (data[7] >> 4)==1;
                            int secondsMotionless = (data[7] & 0x03) * 256 + data[8];
                            int tipPercent = highGAccelAvailable ? data[11] : 0;
                            int spinHorzDPS = BitConverter.ToInt16(new byte[] { data[14], data[13] }, 0);
                            int spinVertDPS = BitConverter.ToInt16(new byte[] { data[16], data[15] }, 0);                           

                            int angle = Convert.ToInt32(180 / Math.PI * Math.Atan2(spinHorzDPS, spinVertDPS));

                            if (dataReady && lastShotNumber!=shotNumber)
                            {                             
                                lastShotNumber = shotNumber;

                                if (highGAccelAvailable)
                                {
                                    Console.WriteLine("MAC: {0}, Shot Number: {1}, Angle: {2}, Tip Percent: {3}", shortMac, shotNumber, angle, tipPercent);

                                    //Update HTML Display               
                                    string html1 = @"<!DOCTYPE html>
<html>	
	<body>
		<iframe style=""border: none; position: fixed; left: 0; right: 0; top: 0; bottom: 0; width: 100%; height: 100%;"" 
src=""_digiball_insert.htm?angle=";
                                    string html2 = @"&tipPercent=";
                                    string html3 = @"""></iframe>

	</body>
</html>";

                                    string html = html1 + angle + html2 + tipPercent + html3;


                                    File.WriteAllText("display.htm", html);

                                } else
                                {
                                    Console.WriteLine("MAC: {0}, Shot Number: {1}, Angle: {2}, Tip Percent: N/A", shortMac, shotNumber, angle);
                                    Console.WriteLine("Display not updated.");
                                }

                            }


                        }
                    }
                }






            }
        }


    }
}