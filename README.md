

# DigiBallScanner
Windows command-line tool for scanning DigiBall devices with Bluetooth LE, and generating images (.png) of tip contact position when new shots are detected. The images can be used as overlays for streaming software such as OBS.

### Requirements:

Windows 10, BT 4.0 adapter

### Installation:

Download and run the installation file DigiBallScanner/innosetup/Output/DigiBallScannerSetup.exe

### Configuring Bluetooth LE adapter

 - Go to Start and type "devmgmt.msc" to open the Device Manager.
 - Find Bluetooth and expand.
 - Verify that one entry reads "Microsoft Bluetooth LE Enumerator". If you do not see this then your adapter does not support BLE. A BLE USB dongle can be purchased from a variety of vendors.
 - You may need to right-click and disable the classic Bluetooth adapter "Intel(R) Wireless Bluetooth(R)" if it causes a conflict.

### Example of usage:

#### Scan all mode 

**DigiBallScanner.exe**

```
Usage: xxxxxx
xxxxxx: Least significant 3 bytes (hex) of DigiBall MAC address.
Scanning for all DigiBall devices only. Images will not be updated until restarted with a MAC address filter...
A2DB54:EA37A7A2DB54:A2-DB-54-01-39-95-1F-C0-01-00-F8-3C-00-00-FC-01-DB-5B-04-04-04-03-02-01
A2DB54:EA37A7A2DB54:A2-DB-54-01-39-95-1F-C0-01-00-F8-3C-00-00-FC-01-DB-5B-04-04-04-03-02-01
A2DB54:EA37A7A2DB54:A2-DB-54-01-39-93-1F-C0-01-00-F8-3C-00-00-FC-01-DB-DB-00-00-00-00-00-00
6EE780:FCA0D56EE780:6E-E7-80-01-39-95-03-D8-01-00-F8-17-00-00-59-00-31-78-02-02-05-08-42-4F
6EE780:FCA0D56EE780:6E-E7-80-01-39-95-03-D8-01-00-F8-17-00-00-59-00-31-78-02-02-05-08-42-4F
6EE780:FCA0D56EE780:6E-E7-80-01-39-96-03-D8-01-00-F8-17-00-00-59-00-31-78-02-02-05-08-42-4F
6EE780:FCA0D56EE780:6E-E7-80-01-39-96-03-D8-01-00-F8-17-00-00-59-00-31-78-02-02-05-08-42-4F
```

 - Will show all devices found plus the hex representation of the GAP advertisement packet. Scan responses are not processed.
 - No images are updated in this mode.


#### Scan for specific device 

**DigiBallScanner.exe 6EE780**

```
MAC: 6EE780, Shot Number: 3, Seconds: 9, Angle: 61, Tip Percent: 23
MAC: 6EE780, Shot Number: 3, Seconds: 9, Angle: 61, Tip Percent: 23
MAC: 6EE780, Shot Number: 3, Seconds: 9, Angle: 61, Tip Percent: 23
MAC: 6EE780, Shot Number: 3, Seconds: 9, Angle: 61, Tip Percent: 23
MAC: 6EE780, Shot Number: 4, Seconds: 0, Angle: 61, Tip Percent: 23
MAC: 6EE780, Shot Number: 4, Seconds: 0, Angle: 61, Tip Percent: 23
MAC: 6EE780, Shot Number: 4, Seconds: 0, Angle: 61, Tip Percent: 23
MAC: 6EE780, Shot Number: 4, Seconds: 0, Angle: 61, Tip Percent: 23
MAC: 6EE780, Shot Number: 4, Seconds: 0, Angle: -60, Tip Percent: 11
MAC: 6EE780, Shot Number: 4, Seconds: 0, Angle: -60, Tip Percent: 11
MAC: 6EE780, Shot Number: 4, Seconds: 0, Angle: -60, Tip Percent: 11
MAC: 6EE780, Shot Number: 4, Seconds: 0, Angle: -60, Tip Percent: 11

```

 - Least 3 significant bytes of the MAC address were given as the only argument. This can be easily accomplished by appending to the shortcut target.
 - Images will be updated in the installation folder. By default this is in C:\Users\{username}\AppData\Local\Programs\DigiBallScanner

#### List of images generated (see DigiBallScanner/example folder)
 - digiball_tipOutline.png
 - digiball_tipOutlineContact.png
 - digiball_tipOutlineContactAngle.png
 - digiball_tipOutlineGrid.png
 - digiball_tipOutlineGridContact.png 
 - digiball_tipOutlineGridContactAngle.png

