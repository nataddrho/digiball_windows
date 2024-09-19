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
using Windows.Devices.Printers;
using Windows.Globalization;


public static class Program
{   
    public static String filterShortMac = "";
    public static int lastShotNumber = -1;
    public static bool identifyScan = true;
    public static bool scanAll = false;
    public static int recvCount = 0;
    public static double tipPercentMultiplier = 1.0;

    static async Task Main(string[] args)
    {      
        String appDataPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        String usage = "Usage: DigiBallScanner.exe x y\nx:       Mac Address filter: Least significant 3 bytes (hex) of DigiBall MAC address.\nx=all:   Scans all visible devices\ny=pool:  Uses pool ball diameter (default)\ny=carom: Uses carom ball diameter";
        Console.WriteLine("DigiBall Console for Windows - Generates realtime ball graphics for streaming software.\n");
        Console.WriteLine("Output images will be generated in:");
        Console.WriteLine(string.Format("{0}\n", appDataPath));

        int i = 0;
        foreach (string arg in args)
        {
            switch (i)
            {
                case 0:
                    if (arg == "help" || arg == "-help" || arg == "--help" || arg == "-h")
                    {
                        Console.WriteLine(usage);
                        return;
                    }
                    else if (arg == "all" || arg == "ALL")
                    {
                        identifyScan = true;
                        scanAll = true;
                    }
                    else if (arg.Length != 6)
                    {
                        Console.WriteLine(usage);
                        return;
                    }
                    else
                    {
                        filterShortMac = arg.ToUpper();
                        identifyScan = false;
                    }
                    break;

                case 1:
                    if (arg == "pool")
                    {
                        tipPercentMultiplier = 1.0;
                    }
                    else if (arg == "carom")
                    {
                        Console.WriteLine("Carom ball diameter used.");
                        tipPercentMultiplier = 61.5 / 57.15; //mm
                    }
                    break;
            }
            i++;
        }

        if (identifyScan) {
            Console.WriteLine(usage);
            if (scanAll)
            {
                Console.WriteLine("Scanning for all visible BLE devices. Images will not be updated until restarted with a MAC address filter...");
            }
            else
            {
                Console.WriteLine("Scanning for all DigiBall devices only. Images will not be updated until restarted with a MAC address filter...");
            }
        } else
        {
            Console.WriteLine(String.Format("Scanning for DigiBall with MAC address {0}...", filterShortMac));
        }

        var watcher = new BluetoothLEAdvertisementWatcher();
        watcher = new BluetoothLEAdvertisementWatcher()
        {
            ScanningMode = BluetoothLEScanningMode.Passive
        };

        var manufacturerData = new BluetoothLEManufacturerData();
        manufacturerData.CompanyId = 0x03DE; //Nathan Rhoades LLC       
        if (!scanAll)
        {
            watcher.AdvertisementFilter.Advertisement.ManufacturerData.Add(manufacturerData);
        }

        watcher.Received += Watcher_Received;
        //Scan and restart every 5 seconds
        while (1 == 1) {
            watcher.Start();
            await Task.Delay(5000);
        }
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

    private static String degrees2clock(int angle)
    {
        // Convert degrees into hours and minutes (o'clocks)
        double a = Convert.ToDouble(angle);
        if (a < 0) a += 360;
        int hour = (int)Math.Floor(a * 12 / 360.0);
        double minute = a * 12 / 360.0 - Convert.ToDouble(hour);
        if (hour == 0) hour = 12;
        minute *= 60;        
        String s;
        if (minute < 10)
        {           
            s = String.Format("{0}:0{1}", hour, (int)minute);
        }
        else
        {
            s = String.Format("{0}:{1}", hour, (int)minute);
        }
        return s;
    }

    private static void drawImage(int rpm, int angle, int tipPercent, bool showDeviation)
    {
        //Update cueball picture

        int ballDiameter = 345; //Size of blank cueball image in pixels, square
        int ballRadius = ballDiameter / 2;
        int tipRadius = (int)(Convert.ToDouble(ballDiameter)*0.5*11.8/(57.15*tipPercentMultiplier));
        String clock = degrees2clock(angle);

        String stats = "";

        if (tipPercent>0)
        {            
            double speedEstimationMPH = 0.26775 * Convert.ToDouble(rpm) / Convert.ToDouble(tipPercent);
            if (speedEstimationMPH < 1) stats = "Soft";
            else if (speedEstimationMPH < 2) stats = "Slow";
            else if (speedEstimationMPH < 4) stats = "Medium";
            else if (speedEstimationMPH < 7) stats = "Fast";
            else stats = "Hard";
            stats += "\n";
        }

        int TipPercentFives = (int)(Math.Round((Convert.ToDouble(tipPercent) / 5)) * 5); //Multiple of 5

        stats = String.Format("{0}{1} rpm\n{2}\n{3} pfc", stats, rpm, clock, TipPercentFives);

        double ax = Math.Sin(Math.PI / 180 * angle);
        double ay = -Math.Cos(Math.PI / 180 * angle);
        double tipEstimationError = showDeviation ? 0.15 : 0;
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
            Bitmap cueballImage = new Bitmap(Resources.blank); //Create from resource

            // Create a graphics object from the image
            Graphics graphics = Graphics.FromImage(cueballImage);

            //Tip outline
            Pen pen = new Pen(System.Drawing.Color.Black);
            Brush brush = new SolidBrush(System.Drawing.Color.Black);
            pen.Width = 2 * tipRadius;
            float x0 = (float)(ballRadius + px1 * ax);
            float y0 = (float)(ballRadius + px1 * ay);
            float x1 = (float)(ballRadius + px2 * ax);
            float y1 = (float)(ballRadius + px2 * ay);
            if (showDeviation)
            {
                graphics.DrawLine(pen, x0, y0, x1, y1);
                FillCircle(graphics, brush, x0, y0, tipRadius);
                FillCircle(graphics, brush, x1, y1, tipRadius);
            } else
            {
                FillCircle(graphics, brush, x0, y0, tipRadius);
            }

            // Save the image to a file
            if (j == 0)
            {
                cueballImage.Save("digiball_tipOutline.png");
            }

            //Grid
            if (j > 0)
            {
                pen.Width = 1;
                for (int i = 0; i < 5; i++)
                {
                    DrawCircle(graphics, pen, ballRadius, ballRadius, (float)(ballRadius * 0.1 * (i + 1)));
                }
                float a = (float)(Math.Sqrt(3) / 2)/2;
                float b = (float)0.5/2;
                graphics.DrawLine(pen, ballRadius, ballRadius/2, ballRadius, 3*ballRadius/2);
                graphics.DrawLine(pen, ballRadius / 2, ballRadius, 3 * ballRadius / 2, ballRadius);
                graphics.DrawLine(pen, ballRadius * (1 + a), ballRadius * (1 + b), ballRadius * (1 - a), ballRadius * (1 - b));
                graphics.DrawLine(pen, ballRadius * (1 + a), ballRadius * (1 - b), ballRadius * (1 - a), ballRadius * (1 + b));
                graphics.DrawLine(pen, ballRadius * (1 + b), ballRadius * (1 + a), ballRadius * (1 - b), ballRadius * (1 - a));
                graphics.DrawLine(pen, ballRadius * (1 + b), ballRadius * (1 - a), ballRadius * (1 - b), ballRadius * (1 + a));

                // Save the image to a file       
                cueballImage.Save("digiball_tipOutlineGrid.png");
            }

            //Tip contact point        
            pen.Width = 6;
            pen.Color = System.Drawing.Color.Cyan;
            brush = new SolidBrush(System.Drawing.Color.Cyan);
            x0 = (float)(ballRadius + r1 * ax);
            y0 = (float)(ballRadius + r1 * ay);
            x1 = (float)(ballRadius + r2 * ax);
            y1 = (float)(ballRadius + r2 * ay);
            if (showDeviation)
            {
                graphics.DrawLine(pen, x0, y0, x1, y1);
                FillCircle(graphics, brush, x0, y0, 3);
                FillCircle(graphics, brush, x1, y1, 3);
            } else
            {
                FillCircle(graphics, brush, x0, y0, 3);
            }

            // Save the image to a file
            if (j == 0)
            {
                cueballImage.Save("digiball_tipOutlineContact.png");
            } else
            {
                cueballImage.Save("digiball_tipOutlineGridContact.png");
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
                cueballImage.Save("digiball_tipOutlineContactAngle.png");
            } else
            {
                cueballImage.Save("digiball_tipOutlineGridContactAngle.png");
            }

            // Dispose of the graphics object and image
            graphics.Dispose();
            cueballImage.Dispose();

            // Generate stats image
            Bitmap statsImage = new Bitmap(ballDiameter, ballDiameter, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            // Create a graphics object from the image
            graphics = Graphics.FromImage(statsImage);

            RectangleF rectF1 = new RectangleF(0, 0, statsImage.Width, statsImage.Height);
            RectangleF rectF2 = new RectangleF(2, 2, statsImage.Width-2, statsImage.Height-2);

            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            graphics.DrawString(stats, new Font("Tahoma", 48), Brushes.Black, rectF2);
            graphics.DrawString(stats, new Font("Tahoma", 48), Brushes.White, rectF1);

            statsImage.Save("digiball_stats.png");

            // Dispose of the graphics object and image
            graphics.Dispose();
            statsImage.Dispose();
        }        
    }

    private static async void Watcher_Received(
        BluetoothLEAdvertisementWatcher watcher,
        BluetoothLEAdvertisementReceivedEventArgs eventArgs)
    {
        var device = await BluetoothLEDevice.FromBluetoothAddressAsync(eventArgs.BluetoothAddress);
        String timeStamp = DateTime.Now.ToString("hh:mm:ss");
        
        if (device != null)
        {
            recvCount++;
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
                
                String manufacturerConsoleString = string.Format("{0} {1} {2} {3:X} {4}",
                    timeStamp,
                    recvCount,
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
                            tipPercent = (int)Math.Round(Convert.ToDouble(tipPercent) * tipPercentMultiplier);
                            if (tipPercent > 60) tipPercent = 60;
                            int spinHorzDPS = BitConverter.ToInt16(new byte[] { data[14], data[13] }, 0);
                            int spinVertDPS = BitConverter.ToInt16(new byte[] { data[16], data[15] }, 0);

                            int angle = Convert.ToInt32(180 / Math.PI * Math.Atan2(spinHorzDPS, spinVertDPS));

                            double spinMagDPS = Math.Sqrt(Math.Pow(spinHorzDPS, 2) + Math.Pow(spinVertDPS, 2));
                            int rpm = (int)Math.Round(60 / 360.0 * spinMagDPS); 
                             

                            Console.WriteLine("{0} {1}: MAC: {2}, Shot Number: {3}, Seconds: {4}, Angle: {5}, Tip Percent: {6}", 
                                timeStamp, recvCount, shortMac, shotNumber, secondsMotionless, angle, tipPercent);
                            
                            if (dataReady && lastShotNumber!=shotNumber)
                            {                             
                                lastShotNumber = shotNumber;                                
                                drawImage(rpm, angle, tipPercent, false);
                            }


                        }
                    }
                }

            }
        }


    }
}