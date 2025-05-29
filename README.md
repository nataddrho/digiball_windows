

# DigiBallScanner
Windows command-line tool for scanning DigiBall devices with Bluetooth LE, and generating images (.png) of tip contact position and statistics when new shots are detected. The images can be used as overlays for streaming software such as OBS. Adding a video delay of 6s (or more) to the main feed will cause the generated graphics to appear slightly before the player shoots. This may be desirable.

### Versions:
 - Version 1.0 - Initial Release
 - Version 1.1 - Changed RPM to RPS for consistency with iOS/Android/DigiCast app
 - Version 1.2 - Ball type and color is read directly from ball. Argument no longer needed.
 - Version 1.3 - Added hidden arguments for changing ball type, factory usage only.
 - Version 1.4 - Simplified scanner messages.

### Requirements:

Windows 10, BT 4.0 adapter

### Installation:

Download and run the installation file DigiBallScanner/innosetup/Output/DigiBallScannerSetup.exe

### What is Installed:

 - DigiBallScanner shortcut: Runs executable directly.
 - DigiBallScannerCmdLine shortcut: Opens command prompt in the installation directory.

### Configuring Bluetooth LE adapter

 - Go to Start and type "devmgmt.msc" to open the Device Manager.
 - Find Bluetooth and expand.
 - Verify that one entry reads "Microsoft Bluetooth LE Enumerator". If you do not see this then your adapter does not support BLE. A BLE USB dongle can be purchased from a variety of vendors.
 - You may need to right-click and disable the classic Bluetooth adapter "Intel(R) Wireless Bluetooth(R)" if it causes a conflict.

### Example of usage:

DigiBallScanner uses command line arguments. For convenience you can edit the shortcut with the appropriate arguments.

#### Scan for all DigiBall devices 

**DigiBallScanner.exe**

```
DigiBall Console for Windows - Generates realtime ball graphics for streaming software.

Output images will be generated in:
C:\Users\usnrho\AppData\Local\Programs\DigiBallScanner

Usage: DigiBallScanner.exe x y
x:       MAC Address filter: Least significant 3 bytes (hex) of DigiBall MAC address.
y:       Optional second MAC address for second player's ball.
x=all:   Scans all visible devices
metric:  Optional: Use metric units
Scanning for all DigiBall devices only. Images will not be updated until restarted with a MAC address filter...
```

 - Will show all DigiBall devices found including the hex representation of the GAP advertisement packet. Scan responses are not processed.
 - No images are updated in this mode.

#### Scan for all BLE devices in local area 

**DigiBallScanner.exe all**

```
...
05:08:04 383 100625 6C69E6DB9623 10-06-25-1E-3D-7C-2D-FC
05:08:05 384 100732 75A313C51163 10-07-32-1F-6F-C4-56-B1-28
05:08:05 385 100517 496DD525FD88 10-05-17-18-9C-F1-F6
05:08:05 386 100713 5AA28AC761CC 10-07-13-1F-EE-D7-6B-EA-48
05:08:05 387 100625 6C69E6DB9623 10-06-25-1E-3D-7C-2D-FC
05:08:05 388 100732 75A313C51163 10-07-32-1F-6F-C4-56-B1-28
05:08:06 389 12022C F26C5DF3D629 12-02-2C-01-07-11-06-2B-14-DF-EB-31-FA-CE-D6-36-F3-D6-6E-0B-76-01-D2
```

 - Will show all BLE devices found including the hex representation of the GAP advertisement packet. Scan responses are not processed.
 - No images are updated in this mode.
 - Used for testing BLE adapter.

#### Scan for specific DigiBall 

**DigiBallScanner.exe 6EE780**

 - Least 3 significant bytes of the MAC address were given as the only argument. This can be easily accomplished by appending to the shortcut target.
 - Images will be updated in the installation folder. By default this is in C:\Users\{username}\AppData\Local\Programs\DigiBallScanner
  
 #### Scan for two DigiBalls (two cueballs are usually used for carom games)

**DigiBallScanner.exe 485FF2 A7E27B**

 - Two MAC addresses as arguments.
 - Images will be updated in the installation folder. By default this is in C:\Users\{username}\AppData\Local\Programs\DigiBallScanner
 - Images for player 1's cue ball start with "digiball1_" and images for player 2's cue ball starts with "digiball2_".

#### List of images generated
 - digiball1_tipOutline.png
 - digiball1_tipOutlineContact.png
 - digiball1_tipOutlineContactAngle.png
 - digiball1_tipOutlineGrid.png
 - digiball1_tipOutlineGridContact.png 
 - digiball1_tipOutlineGridContactAngle.png
 - digiball1_stats.png
 - digiball2_tipOutline.png
 - digiball2_tipOutlineContact.png
 - digiball2_tipOutlineContactAngle.png
 - digiball2_tipOutlineGrid.png
 - digiball2_tipOutlineGridContact.png 
 - digiball2_tipOutlineGridContactAngle.png
 - digiball2_stats.png

### Videos:

https://www.youtube.com/watch?v=FP21fO4wFxw&list=PLkjwxZDtCvmhSuGhGhC9gms-NWCeAy0Ar&pp=gAQBiAQB

