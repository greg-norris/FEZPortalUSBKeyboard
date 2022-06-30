using System.IO;
using System.Diagnostics;
using System.Threading;
using GHIElectronics.TinyCLR.Devices.UsbHost;
using GHIElectronics.TinyCLR.Devices.UsbHost.Descriptors;
using GHIElectronics.TinyCLR.Devices.Storage;
using GHIElectronics.TinyCLR.IO;
using GHIElectronics.TinyCLR.Pins;
using GHIElectronics.TinyCLR.Drivers.BasicGraphics;
using GHIElectronics.TinyCLR.Devices.Display;
using GHIElectronics.TinyCLR.Devices.Gpio;
using System.Drawing;

SolidBrush white = new SolidBrush(Color.Black);


GpioPin backlight = GpioController.GetDefault().OpenPin(SC20260.GpioPin.PA15);
backlight.SetDriveMode(GpioPinDriveMode.Output);
backlight.Write(GpioPinValue.High);

var displayController = DisplayController.GetDefault();

var column = 195;
var row = 72;

// Enter the proper display configurations
displayController.SetConfiguration(new ParallelDisplayControllerSettings
{
    Width = 480,
    Height = 272,
    DataFormat = DisplayDataFormat.Rgb565,
    Orientation = DisplayOrientation.Degrees0, //Rotate display.
    PixelClockRate = 10000000,
    PixelPolarity = false,
    DataEnablePolarity = false,
    DataEnableIsFixed = false,
    HorizontalFrontPorch = 2,
    HorizontalBackPorch = 2,
    HorizontalSyncPulseWidth = 41,
    HorizontalSyncPolarity = false,
    VerticalFrontPorch = 2,
    VerticalBackPorch = 2,
    VerticalSyncPulseWidth = 10,
    VerticalSyncPolarity = false,
});

displayController.Enable(); //This line turns on the display I/O and starts
                            //  refreshing the display. Native displays are
                            //  continually refreshed automatically after this
                            //  command is executed.

var screen = Graphics.FromHdc(displayController.Hdc);
var font = FEZPortalUSBKeyboard.Resources.GetFont(FEZPortalUSBKeyboard.Resources.FontResources.Compact);

var image = FEZPortalUSBKeyboard.Resources.GetBitmap(FEZPortalUSBKeyboard.Resources.BitmapResources.signIn);


screen.Clear();

screen.DrawImage(image, 0, 0);





//screen.DrawString("Hello world!", font, white, 210, 255);
screen.Flush();


var usbHostController = UsbHostController.GetDefault();

usbHostController.OnConnectionChangedEvent +=
UsbHostController_OnConnectionChangedEvent;

usbHostController.Enable();


Thread.Sleep(Timeout.Infinite);


void UsbHostController_OnConnectionChangedEvent (UsbHostController sender, DeviceConnectionEventArgs e){

        Debug.WriteLine("e.Id = " + e.Id + " \n");
        Debug.WriteLine("e.InterfaceIndex = " + e.InterfaceIndex + " \n");
        Debug.WriteLine("e.PortNumber = " + e.PortNumber);
        Debug.WriteLine("e.Type = " + ((object)(e.Type)). ToString() + " \n");
        Debug.WriteLine("e.VendorId = " + e.VendorId + " \n");
        Debug.WriteLine("e.ProductId = " + e.ProductId + " \n");

            switch (e.DeviceStatus)
            {
                case DeviceConnectionStatus.Connected:
                    switch (e.Type)
                    {
                        case BaseDevice.DeviceType.Keyboard:
                            var keyboard = new Keyboard(e.Id, e.InterfaceIndex);
                            keyboard.KeyUp += Keyboard_KeyUp;
                            keyboard.KeyDown += Keyboard_KeyDown;
                            break;

                        case BaseDevice.DeviceType.Mouse:
                            var mouse = new Mouse(e.Id, e.InterfaceIndex);
                            mouse.ButtonChanged += Mouse_ButtonChanged;
                            mouse.CursorMoved += Mouse_CursorMoved;
                            break;

                        case BaseDevice.DeviceType.Joystick:
                            var joystick = new Joystick(e.Id, e.InterfaceIndex);
                            joystick.CursorMoved += Joystick_CursorMoved;
                            joystick.HatSwitchPressed += Joystick_HatSwitchPressed;
                            joystick.ButtonChanged += Joystick_ButtonChanged;
                            break;

                        case BaseDevice.DeviceType.MassStorage:
                            var storageController = StorageController.FromName
                                (SC20260.StorageController.UsbHostMassStorage);
                            var driver = FileSystem.Mount(storageController.Hdc);
                            var driveInfo = new DriveInfo(driver.Name);

                            Debug.WriteLine("Free: " + driveInfo.TotalFreeSpace);
                            Debug.WriteLine("TotalSize: " + driveInfo.TotalSize);
                            Debug.WriteLine("VolumeLabel:" + driveInfo.VolumeLabel);
                            Debug.WriteLine("RootDirectory: " + driveInfo.RootDirectory);
                            Debug.WriteLine("DriveFormat: " + driveInfo.DriveFormat);

                            break;

                        default:
                            var rawDevice = new RawDevice(e.Id, e.InterfaceIndex, e.Type);
                            var devDesc = rawDevice.GetDeviceDescriptor();
                            var cfgDesc = rawDevice.GetConfigurationDescriptor(0);
                            var endpointData = new byte[7];

                            endpointData[0] = 7;        //Length in bytes of this descriptor.
                            endpointData[1] = 5;        //Descriptor type (endpoint).
                            endpointData[2] = 0x81;     //Input endpoint address.
                            endpointData[3] = 3;        //Transfer type is interrupt endpoint.
                            endpointData[4] = 8;        //Max packet size LSB.
                            endpointData[5] = 0;        //Max packet size MSB.
                            endpointData[6] = 10;       //Polling interval.

                            var endpoint = new Endpoint(endpointData, 0);

                            var pipe = rawDevice.OpenPipe(endpoint);
                            pipe.TransferTimeout = 10;

                            var data = new byte[8];
                            var read = pipe.Transfer(data);

                            if (read > 0)
                            {
                                Debug.WriteLine("Raw Device has new data "
                                    + data[0] + ", " + data[1] + ", " + data[2] + ", " + data[3]);
                            }

                            else if (read == 0)
                            {
                                Debug.WriteLine("No new data");
                            }

                            Thread.Sleep(500);
                            break;
                    }
                    break;

                case DeviceConnectionStatus.Disconnected:
                    Debug.WriteLine("Device Disconnected");
                    //Unmount filesystem if it was mounted.
                    break;

                case DeviceConnectionStatus.Bad:
                    Debug.WriteLine("Bad Device");
                    break;
            }
}

void Keyboard_KeyDown(Keyboard sender, Keyboard.KeyboardEventArgs args){
            Debug.WriteLine("Key pressed: " + ((object)args.Which).ToString());
            Debug.WriteLine("Key pressed ASCII: " +
                ((object)args.ASCII).ToString());
    var currentletter = ((object)args.ASCII).ToString();
    var currentletterNumber = ((object)args.Which).ToString();



    if (currentletterNumber == "40")
    {
        column = -15;
        row = row + 25;
    }
    else if (currentletterNumber == "43")
    {
        column = 175;
        row = 120;
    }
    else
    {
        screen.DrawString(((object)args.ASCII).ToString(), font, white, column, row);
    }

    

    screen.Flush();


    if (currentletter == "i" ^ currentletter == "I" ^ currentletter == "j" ^ currentletter == "l")
    {
        column = column + 10;
    }
    else if (currentletter == "r" ^ currentletter == "J")
    {
        column = column + 12;
    }
    else if (currentletter == "f" ^ currentletter == "s" ^ currentletter == "t")
    {
        column = column + 15;
    }
    else if (currentletter == "c" ^ currentletter == "S" ^ currentletter == "C" ^ currentletter == "L" ^ currentletter == "R" ^ currentletter == "T")
    {
        column = column + 17;
    }
    else if (currentletter == "w" ^ currentletter == "R")
    {
        column = column + 25;
    }
    else if (currentletter == "m" ^ currentletter == "M" ^ currentletter == "W")
    {
        column = column + 30;
    }
    else if (currentletter == "")
    {
        //column = column + 30;
    }
    else if (currentletterNumber == "44")
    {
        column = column + 15;
    }
    else
    {
        column = column + 20;
    }
        




    
    if (column > 470)
    {
        column = 0;
        row = row + 25;
    }

}
void Keyboard_KeyUp(Keyboard sender, Keyboard.KeyboardEventArgs args){
            Debug.WriteLine("Key released: " + ((object)args.Which).ToString());
            Debug.WriteLine("Key released ASCII: " + ((object)args.ASCII).ToString());
    
}

void Mouse_CursorMoved(Mouse sender, Mouse.CursorMovedEventArgs e){
            Debug.WriteLine("Mouse moved to: " + e.NewPosition.X + ", " + e.NewPosition.Y);
}

void Mouse_ButtonChanged(Mouse sender, Mouse.ButtonChangedEventArgs args){
            Debug.WriteLine("Mouse button changed: " + ((object)args.Which).ToString());
}


void Joystick_ButtonChanged(Joystick sender, Joystick.ButtonChangedEventArgs e){
            Debug.WriteLine("Joystick button changed  = " + ((object)(e.Which)).ToString());
}

void Joystick_HatSwitchPressed(Joystick sender, Joystick.HatSwitchPressedEventArgs e){
            Debug.WriteLine("Joystick direction  = " + ((object)(e.Direction)).ToString());
}

void Joystick_CursorMoved(Joystick sender, Joystick.CursorMovedEventArgs e){
            Debug.WriteLine("Joystick.move  = " + e.NewPosition.X + ", " + e.NewPosition.Y);
}


