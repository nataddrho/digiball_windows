using System;
using System.Drawing;
using System.Threading.Tasks;
using System.IO;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using System.Threading;
using Windows.Storage.Streams;
using System.Collections.Generic;
using Windows.Devices.Sensors;
using Windows.UI.Xaml.Shapes;
using static System.Net.Mime.MediaTypeNames;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI;
using Windows.Devices.Enumeration;
using DigiBallScanner.Properties;


public static class Program
{   
    public static String filterShortMac = "";
    public static int lastShotNumber = -1;
    public static bool identifyScan = true;

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
                        identifyScan = false;
                    }
                    break;                
            }            
            i++;
        }  
        
        if (identifyScan) {
            Console.WriteLine(usage);
            Console.WriteLine("Scanning for all DigiBall devices only. Images will not be updated until restarted with a MAC address filter...");
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

    private static void DrawCircle(this Graphics g, Pen pen,
                                  float centerX, float centerY, float radius)
    {
        g.DrawEllipse(pen, centerX - radius, centerY - radius,
                      radius + radius, radius + radius);
    }

    private static void FillCircle(this Graphics g, Brush brush,
                                  float centerX, float centerY, float radius)
    {
        g.FillEllipse(brush, centerX - radius, centerY - radius,
                      radius + radius, radius + radius);
    }

    private static void drawImage(int angle, int tipPercent)
    {
        //Update cueball picture

        int ballDiameter = 345; //Size of blank cueball image in pixels, square
        int ballRadius = ballDiameter / 2;
        int tipRadius = 35;

        double ax = Math.Sin(Math.PI / 180 * angle);
        double ay = -Math.Cos(Math.PI / 180 * angle);
        double tipEstimationError = 0.15;
        double tipRadiusDime = 0.358;
        double tipRadiusCurvatureRatio = tipRadiusDime / 1.125;
        double est1 = tipPercent * (1 - tipEstimationError) / 100;
        double est2 = tipPercent * (1 + tipEstimationError) / 100;
        if (est2 > 0.6) est2 = 0.6;
        double r1 = ballRadius * est1;
        double r2 = ballRadius * est2;
        double drawOffset1 = r1 * tipRadiusCurvatureRatio;
        double drawOffset2 = r2 * tipRadiusCurvatureRatio;              
        double px1 = 0;
        double px2 = 0;

        double s1 = r1 + drawOffset1;
        if ((s1 - tipRadius) > r2)
        {
            px1 = r1 + tipRadius;
        }
        else
        {
            px1 = s1;
        }
        double s2 = r2 + drawOffset2;
        if ((s2 - tipRadius) > r2)
        {
            px2 = r2 + tipRadius;
        }
        else
        {
            px2 = s2;
        }

        // Get bitmap file location
        //string dir = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        //string cueball = System.IO.Path.Combine(dir, "blank.png");

        for (int j = 0; j < 2; j++)
        {
            
            //Bitmap image = new Bitmap(cueball);
            Bitmap image = new Bitmap(Resources.blank); //Create from resource


            // Create a graphics object from the image
            Graphics graphics = Graphics.FromImage(image);

            //Tip outline
            Pen pen = new Pen(System.Drawing.Color.Black);
            Brush brush = new SolidBrush(System.Drawing.Color.Black);
            pen.Width = 2 * tipRadius;
            float x0 = (float)(ballRadius + px1 * ax);
            float y0 = (float)(ballRadius + px1 * ay);
            float x1 = (float)(ballRadius + px2 * ax);
            float y1 = (float)(ballRadius + px2 * ay);
            graphics.DrawLine(pen, x0, y0, x1, y1);
            FillCircle(graphics, brush, x0, y0, tipRadius);
            FillCircle(graphics, brush, x1, y1, tipRadius);

            // Save the image to a file
            if (j == 0)
            {
                image.Save("digiball_tipOutline.png");
            }

            //Grid
            if (j > 0)
            {
                pen.Width = 1;
                for (int i = 0; i < 5; i++)
                {
                    DrawCircle(graphics, pen, ballRadius, ballRadius, (float)(ballRadius * 0.1 * (i + 1)));
                }
                float a = (float)(Math.Sqrt(3) / 2);
                float b = (float)0.5;
                graphics.DrawLine(pen, ballRadius, 0, ballRadius, ballDiameter);
                graphics.DrawLine(pen, 0, ballRadius, ballDiameter, ballRadius);
                graphics.DrawLine(pen, ballRadius * (1 + a), ballRadius * (1 + b), ballRadius * (1 - a), ballRadius * (1 - b));
                graphics.DrawLine(pen, ballRadius * (1 + a), ballRadius * (1 - b), ballRadius * (1 - a), ballRadius * (1 + b));
                graphics.DrawLine(pen, ballRadius * (1 + b), ballRadius * (1 + a), ballRadius * (1 - b), ballRadius * (1 - a));
                graphics.DrawLine(pen, ballRadius * (1 + b), ballRadius * (1 - a), ballRadius * (1 - b), ballRadius * (1 + a));

                // Save the image to a file       
                image.Save("digiball_tipOutlineGrid.png");
            }

            //Tip contact point        
            pen.Width = 6;
            pen.Color = System.Drawing.Color.Cyan;
            brush = new SolidBrush(System.Drawing.Color.Cyan);
            x0 = (float)(ballRadius + r1 * ax);
            y0 = (float)(ballRadius + r1 * ay);
            x1 = (float)(ballRadius + r2 * ax);
            y1 = (float)(ballRadius + r2 * ay);
            graphics.DrawLine(pen, x0, y0, x1, y1);
            FillCircle(graphics, brush, x0, y0, 3);
            FillCircle(graphics, brush, x1, y1, 3);

            // Save the image to a file
            if (j == 0)
            {
                image.Save("digiball_tipOutlineContact.png");
            } else
            {
                image.Save("digiball_tipOutlineGridContact.png");
            }

            //Guide
            pen.Color = System.Drawing.Color.Red;
            pen.Width = 4;
            graphics.DrawLine(pen, ballRadius, ballRadius, (float)(ballRadius * (1 + ax)), (float)(ballRadius * (1 + ay)));
            pen.Width = 6;
            pen.Color = System.Drawing.Color.Cyan;
            graphics.DrawLine(pen, x0, y0, x1, y1);
            FillCircle(graphics, brush, x0, y0, 3);
            FillCircle(graphics, brush, x1, y1, 3);

            // Save the image to a file
            if (j == 0)
            {
                image.Save("digiball_tipOutlineContactAngle.png");
            } else
            {
                image.Save("digiball_tipOutlineGridContactAngle.png");
            }

            // Dispose of the graphics object and image
            graphics.Dispose();
            image.Dispose();
        }        
    }

    private static async void Watcher_Received(
        BluetoothLEAdvertisementWatcher watcher,
        BluetoothLEAdvertisementReceivedEventArgs eventArgs)
    {
        var device = await BluetoothLEDevice.FromBluetoothAddressAsync(eventArgs.BluetoothAddress);       

        if (device != null)
        {           
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
                
                String shortMac = BitConverter.ToString(data).Replace("-", string.Empty).Substring(0, 6);
                
                String manufacturerConsoleString = string.Format("{0}:{1:X}:{2}",
                    shortMac,
                    device.BluetoothAddress,
                    BitConverter.ToString(data));

                if (identifyScan)
                {
                    Console.WriteLine(manufacturerConsoleString);
                }
                else if (filterShortMac==shortMac)
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

                            Console.WriteLine("MAC: {0}, Shot Number: {1}, Seconds: {2}, Angle: {3}, Tip Percent: {4}", shortMac, shotNumber, secondsMotionless, angle, tipPercent);

                            if (dataReady && lastShotNumber!=shotNumber)
                            {                             
                                lastShotNumber = shotNumber;
                                drawImage(angle, tipPercent);
                            }


                        }
                    }
                }






            }
        }


    }
}