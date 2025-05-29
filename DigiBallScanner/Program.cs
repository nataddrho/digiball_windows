//Nathan Rhoades LLC

using System;
using System.Linq;
using System.Drawing;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Storage.Streams;
using DigiBallScanner.Properties;

public class ConsoleWriter
{
    public void writeManufDataInfo(BluetoothLEManufacturerData manufData)
    {
        if (manufData.CompanyId == 0x03DE) //Nathan Rhoades LLC
        {
            var data = new byte[manufData.Data.Length];
            using (var reader = DataReader.FromBuffer(manufData.Data))
            {
                reader.ReadBytes(data);
            }
            String shortMac = BitConverter.ToString(data).Replace("-", string.Empty).Substring(0, 6);
            if (data.Length == 24)
            {
                int deviceType = data[3] & 0xF;
                if (deviceType == 1)
                {

                    int chargeStatus = (data[7] >> 6) & 3;
                    String[] chargeStatusDesc = { "", "(charging)", "(charging error)", "(full charge)" };
                    bool shipMode = ((data[7] >> 5) & 1) == 1;
                    String chargeMode = "(ship-mode)";
                    if (!shipMode)
                    {
                        chargeMode = chargeStatusDesc[chargeStatus];
                    }
                    int ballType = (data[3] >> 4) & 0xF;
                    String[] ballTypeDesc = { "pool", "carom", "carom-yellow", "snooker", "english", "russian" };
                    String ballDesc = "unknown";
                    if (ballType < ballTypeDesc.Length)
                    {
                        ballDesc = ballTypeDesc[ballType];
                    }
                    String timeStamp = DateTime.Now.ToString("hh:mm:ss");
                    String manufConsoleString = string.Format("{0} {1} {2} {3}",
                        timeStamp, shortMac, ballDesc, chargeMode);
                    Console.WriteLine(manufConsoleString);
                }
            }
        }
    }
}


//BLE Writer is for factory usage purposes of setting the ball type. Not needed for consumer usage.
class BLEWriterUnpaired
{
    private BluetoothLEAdvertisementWatcher watcher;
    private ulong targetBluetoothAddress;
    private TaskCompletionSource<ulong> foundDeviceTcs;
    private ConsoleWriter consoleWriter = new ConsoleWriter();

    public async Task WriteToUnpairedDeviceAsync(string targetName, Guid serviceUuid, Guid characteristicUuid, byte[] value)
    {
        foundDeviceTcs = new TaskCompletionSource<ulong>();
        watcher = new BluetoothLEAdvertisementWatcher
        {
            ScanningMode = BluetoothLEScanningMode.Active
        };

        watcher.Received += (w, btAdv) =>
        {                        
            foreach (var manufData in btAdv.Advertisement.ManufacturerData)
            {
                consoleWriter.writeManufDataInfo(manufData);
            }            

            var localName = btAdv.Advertisement.LocalName;
            if (!string.IsNullOrEmpty(localName) && localName.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                targetBluetoothAddress = btAdv.BluetoothAddress;
                foundDeviceTcs.TrySetResult(targetBluetoothAddress);
                watcher.Stop();
            }
        };
                
        watcher.Start();

        var timeout = Task.Delay(10000); // 10s timeout
        var completedTask = await Task.WhenAny(foundDeviceTcs.Task, timeout);
        if (completedTask == timeout)
        {            
            return;
        }

        ulong address = await foundDeviceTcs.Task;
        var bleDevice = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
        if (bleDevice == null)
        {
            Console.WriteLine("Failed to connect to BLE device.");
            return;
        }

        var servicesResult = await bleDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
        if (servicesResult.Status != GattCommunicationStatus.Success)
        {
            Console.WriteLine("Failed to get GATT services.");
            return;
        }

        var service = servicesResult.Services.FirstOrDefault(s => s.Uuid == serviceUuid);
        if (service == null)
        {
            Console.WriteLine("Service not found.");
            return;
        }

        var characteristicsResult = await service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
        if (characteristicsResult.Status != GattCommunicationStatus.Success)
        {
            Console.WriteLine("Failed to get GATT characteristics.");
            return;
        }

        var characteristic = characteristicsResult.Characteristics.FirstOrDefault(c => c.Uuid == characteristicUuid);
        if (characteristic == null)
        {
            Console.WriteLine("Characteristic not found.");
            return;
        }

        var writer = new DataWriter();
        writer.WriteBytes(value);

        var status = await characteristic.WriteValueAsync(writer.DetachBuffer(), GattWriteOption.WriteWithResponse);
        if (status == GattCommunicationStatus.Success)
        {
            Console.WriteLine("Update successful!");
        }
        else
        {
            Console.WriteLine("Write failed with status: " + status);
        }
    }
}

public static class Program
{
    private static ConsoleWriter consoleWriter = new ConsoleWriter();
    public static String[] filterShortMac = {"",""};
    public static int[] lastShotNumber = { -1, -1 };
    public static int[] runningShotNumber = { 0, 0 };
    public static bool identifyScan = true;    
    public static byte connectAndConfigureByte = 0;
    public static bool scanAll = false;
    public static int recvCount = 0;    
    public static int players = 0;    
    public static String ballTypeArgDesc = "unknown";

    static async Task Main(string[] args)
    {      
        String appDataPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        String usage = "Version 1.4\nUsage:   DigiBallScanner.exe x y\nx and y: Mac Address filter: Least significant 3 bytes (hex) of DigiBall MAC address.\nNone:    Scans all visible devices";
        Console.WriteLine("DigiBall Console for Windows - Generates realtime ball graphics for streaming software.\n");
        Console.WriteLine("Output images will be generated in:");
        Console.WriteLine(string.Format("{0}\n", appDataPath));

        int i = 0;
        foreach (string arg in args)
        {   
            if (arg == "help" || arg == "-help" || arg == "--help" || arg == "-h")
            {
                Console.WriteLine(usage);
                return;
            }           
            else if (arg == "--change-to-pool") //Hidden argument option
            {
                connectAndConfigureByte = 0xB0;
                ballTypeArgDesc = "pool";
                break;
            }
            else if (arg == "--change-to-carom") //Hidden argument option
            {
                connectAndConfigureByte = 0xB1;
                ballTypeArgDesc = "carom";
                break;
            }
            else if (arg == "--change-to-carom-yellow") //Hidden argument option
            {
                connectAndConfigureByte = 0xB2;
                ballTypeArgDesc = "carom-yellow";
                break;
            }
            else if (arg == "--change-to-snooker") //Hidden argument option
            {
                connectAndConfigureByte = 0xB3;
                ballTypeArgDesc = "snooker";
                break;
            }
            else if (arg == "--change-to-english") //Hidden argument option
            {
                connectAndConfigureByte = 0xB4;
                ballTypeArgDesc = "english";
                break;
            }
            else if (arg == "--change-to-russian") //Hidden argument option
            {
                connectAndConfigureByte = 0xB5;
                ballTypeArgDesc = "russian";
                break;
            }
            else if (arg == "all" || arg == "ALL")
            {
                identifyScan = true;
                scanAll = true;
                break;
            }                        
            else if (arg.Length != 6)
            {
                Console.WriteLine(usage);
                return;
            }
            else
            {
                if (players > 2)
                {
                    Console.WriteLine("Maximum of two devices are allowed.");
                    Console.WriteLine(usage);
                    return;
                }
                else
                {
                    filterShortMac[players] = arg.ToUpper();
                    identifyScan = false;
                    players++;
                }
            }          
        }

        // For factory usage only
        if (connectAndConfigureByte>0)
        {
            var bleWriter = new BLEWriterUnpaired();

            //DigiBall configuration identifiers
            Guid serviceUuid = Guid.Parse("00001523-1812-efde-1523-785feabcd123");
            Guid characteristicUuid = Guid.Parse("00001525-1812-efde-1523-785feabcd123");

            byte[] valueToWrite = new byte[] { connectAndConfigureByte };

            Console.WriteLine(String.Format("Scanning for all DigiBalls and will change ball type to {0} when connectable...", ballTypeArgDesc));

            while (1 == 1)
            {
                await bleWriter.WriteToUnpairedDeviceAsync("DigiBall", serviceUuid, characteristicUuid, valueToWrite);
            }

        }
        else
        {
            if (identifyScan)
            {
                Console.WriteLine(usage);
                if (scanAll)
                {
                    Console.WriteLine("Scanning for all visible BLE devices. Images will not be updated until restarted with a MAC address filter...");
                }
                else
                {
                    Console.WriteLine("Scanning for all DigiBall devices only. Images will not be updated until restarted with a MAC address filter...");
                }
            }
            else
            {
                for (i = 0; i < players; i++)
                {
                    Console.WriteLine(String.Format("Device {0}, scanning for DigiBall with MAC address {1}...", i + 1, filterShortMac[i]));
                }
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
            while (1 == 1)
            {
                watcher.Start();
                await Task.Delay(5000);
            }
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

    private static void drawImage(int player, bool yellow, double tipPercentMultiplier, int shotNumber, int spinRPM, int angle, int tipPercent, double speedMPH, bool showDeviation)
    {
        //Update cueball picture

        int ballDiameter = 345; //Size of blank cueball image in pixels, square
        int ballRadius = ballDiameter / 2;
        int tipRadius = (int)(Convert.ToDouble(ballDiameter)*0.5*11.8/(57.15*tipPercentMultiplier));
        String clock = degrees2clock(angle);

        String stats = "";      

        int TipPercentFives = (int)(Math.Round((Convert.ToDouble(tipPercent) / 5)) * 5); //Multiple of 5
                
        stats = String.Format("{0}\n{1} pfc\n{2} rps", clock, TipPercentFives, spinRPM / 60);

        double ax = Math.Sin(Math.PI / 180 * angle);
        double ay = -Math.Cos(Math.PI / 180 * angle);
        double tipEstimationError = showDeviation ? 0.15 : 0;
        double tipRadiusDime = 0.358;
        double tipRadiusCurvatureRatio = tipRadiusDime / 1.125;
        double est1 = tipPercent * (1 - tipEstimationError) / 100;
        double est2 = tipPercent * (1 + tipEstimationError) / 100;
        if (est2 > 0.55) est2 = 0.55;
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
            Bitmap backgroundImage;
            if (yellow)
            {
                backgroundImage = Resources.blank_yellow;
            } else
            {
                backgroundImage = Resources.blank;
            }

            Bitmap cueballImage = new Bitmap(backgroundImage); //Create from resource            

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
                cueballImage.Save(String.Format("digiball{0}_tipOutline.png",player));
            }

            //Grid
            if (j > 0)
            {
                pen.Width = 1;
                for (int i = 0; i < 6; i++)
                {
                    DrawCircle(graphics, pen, ballRadius, ballRadius, (float)(ballRadius * 0.1 * (i + 1)));
                }

                for (int i = 0; i<6; i++)
                {
                    double x = 0.6 * ballRadius * Math.Cos(2 * Math.PI * Convert.ToDouble(i) / 12);
                    double y = 0.6 * ballRadius * Math.Sin(2 * Math.PI * Convert.ToDouble(i) / 12);
                    graphics.DrawLine(pen, ballRadius - (int)x, ballRadius - (int)y, ballRadius + (int)x, ballRadius + (int)y);
                }

                // Save the image to a file       
                cueballImage.Save(String.Format("digiball{0}_tipOutlineGrid.png",player));
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
                cueballImage.Save(String.Format("digiball{0}_tipOutlineContact.png",player));
            } else
            {
                cueballImage.Save(String.Format("digiball{0}_tipOutlineGridContact.png",player));
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
                cueballImage.Save(String.Format("digiball{0}_tipOutlineContactAngle.png",player));
            } else
            {
                cueballImage.Save(String.Format("digiball{0}_tipOutlineGridContactAngle.png",player));
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
            graphics.DrawString(stats, new Font("Tahoma", 42), Brushes.Black, rectF2);
            graphics.DrawString(stats, new Font("Tahoma", 42), Brushes.White, rectF1);

            statsImage.Save(String.Format("digiball{0}_stats.png",player));

            // Dispose of the graphics object and image
            graphics.Dispose();
            statsImage.Dispose();
        }        
    }

    private static async void Watcher_Received(
        BluetoothLEAdvertisementWatcher watcher,
        BluetoothLEAdvertisementReceivedEventArgs eventArgs)
    {
        try
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

                    if (identifyScan)
                    {
                        consoleWriter.writeManufDataInfo(manufacturerData);
                    }
                    else
                    {
                        var data = new byte[manufacturerData.Data.Length];
                        using (var reader = DataReader.FromBuffer(manufacturerData.Data))
                        {
                            reader.ReadBytes(data);
                        }

                        String shortMac = BitConverter.ToString(data).Replace("-", string.Empty).Substring(0, 6);

                        int player = 0;
                        if (filterShortMac[0] == shortMac)
                        {
                            player = 1;
                        } else if (filterShortMac[1] == shortMac)
                        {
                            player = 2;
                        }
                        if (player > 0)
                        {
                            if (data.Length == 24)
                            {
                                int deviceType = data[3]&0xF;
                                int ballType = (data[3] >> 4) & 0xF;
                                
                                double tipPercentMultiplier = 1.0;
                                bool ballIsYellow = false;
                                String ballDescription = "pool";

                                switch (ballType)
                                {
                                    case 0:
                                        tipPercentMultiplier = 1.0;
                                        break;
                                    case 1:
                                        tipPercentMultiplier = 2.438 / 2.25;
                                        ballDescription = "carom";
                                        break;
                                    case 2:
                                        tipPercentMultiplier = 2.438 / 2.25;
                                        ballDescription = "carom yellow";
                                        ballIsYellow = true;
                                        break;
                                    case 3:
                                        tipPercentMultiplier = 2.063 / 2.25;
                                        ballDescription = "snooker";
                                        break;
                                    case 4:
                                        tipPercentMultiplier = 2 / 2.25;
                                        ballDescription = "english";
                                        break;
                                    case 5:
                                        tipPercentMultiplier = 2.668 / 2.25;
                                        ballDescription = "russian";
                                        break;
                                    default:
                                        tipPercentMultiplier = 1.0;
                                        break;
                                }
                                                       

                                if (deviceType == 1)
                                {
                                    String ply = "";
                                    if (players > 1)
                                    {
                                        ply = String.Format("Player{0}", player);
                                    }
                                    bool dataReady = (data[17] >> 6) == 1;
                                    int shotNumber = data[6] & 0x3F;                                    
                                    int secondsMotionless = (data[7] & 0x03) * 256 + data[8];                                    
                                    int tipPercent = data[11];
                                    int speedFactor = data[12];
                                    tipPercent = (int)Math.Round(Convert.ToDouble(tipPercent) * tipPercentMultiplier);
                                    if (tipPercent > 60) tipPercent = 60;
                                    int spinHorzDPS = BitConverter.ToInt16(new byte[] { data[14], data[13] }, 0);
                                    int spinVertDPS = BitConverter.ToInt16(new byte[] { data[16], data[15] }, 0);
                                    int angle = Convert.ToInt32(180 / Math.PI * Math.Atan2(spinHorzDPS, spinVertDPS));
                                    double spinMagDPS = Math.Sqrt(Math.Pow(spinHorzDPS, 2) + Math.Pow(spinVertDPS, 2));
                                    double speedMPH = 0.06 * speedFactor;                                    
                                    int spinRPM = (int)Math.Round(60 / 360.0 * spinMagDPS);
                                    double spinRPS = (double)spinRPM / 60;
                                    
                                    Console.WriteLine("{0} {1} {2}: ({3}) MAC: {4}, Shot Number: {5}, Seconds: {6}, Angle(deg): {7}, Tip Percent: {8}, Spin(rps): {9:F1}",
                                        timeStamp, recvCount, ply, ballDescription, shortMac, shotNumber, secondsMotionless, angle, tipPercent, spinRPS);

                                    if (dataReady && lastShotNumber[player - 1] != shotNumber)
                                    {
                                        lastShotNumber[player - 1] = shotNumber;
                                        runningShotNumber[player - 1]++;
                                        drawImage(player, ballIsYellow, tipPercentMultiplier, runningShotNumber[player - 1], spinRPM, angle, tipPercent, speedMPH, false);
                                    }                                                                       
                                }
                            }
                        }
                    }

                }
            }
        } catch (Exception e)
        {
            /**
            // Get stack trace for the exception with source file information
            var st = new StackTrace(e, true);
            // Get the top stack frame
            var frame = st.GetFrame(0);
            // Get the line number from the stack frame
            var line = frame.GetFileLineNumber();
            Console.WriteLine(e.Message);
            Console.WriteLine("Line: {0}", line);
            **/
        }
    }
}