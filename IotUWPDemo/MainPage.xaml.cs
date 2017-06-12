using GrovePi;
using GrovePi.Sensors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net;
using System.Text;
using Newtonsoft.Json.Linq;
using Windows.UI;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IotUWPDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }
        private readonly IBuildGroveDevices _deviceFactory = DeviceFactory.Build;
        private Timer periodicTimer;
        private String measures = "";

        private bool notSent = false;
        private DateTime timeEntered = DateTime.MinValue;

        //OLD Implementation
        //private async void TimerCallBack(object state)
        //{
        //    try
        //    {
        //        IBuzzer groveBuzzer = DeviceFactory.Build.Buzzer(Pin.DigitalPin6);

        //        var res = await AzureIoTHub.ReceiveCloudToDeviceMessageAsync();
        //        if (res != null)
        //        {
        //            dynamic d = JObject.Parse(res);

        //            if (d.powerDisplay != null)
        //                RaspberrySettings.PowerDisplay = (bool)d.powerDisplay;

        //            if (d.personIdentified != null)
        //                groveBuzzer.ChangeState(SensorStatus.Off);

        //        }

        //        var dhttSensor = DeviceFactory.Build.DHTTemperatureAndHumiditySensor(Pin.DigitalPin3, DHTModel.Dht11);
        //        dhttSensor.Measure();
        //        measures = $"temp: {dhttSensor.TemperatureInCelsius.ToString("F2")} C\nhumidity: {dhttSensor.Humidity.ToString("F2")}";

        //        if (RaspberrySettings.PowerDisplay)
        //            DeviceFactory.Build.RgbLcdDisplay().SetText(measures).SetBacklightRgb(0, 255, 255);
        //        else
        //            DeviceFactory.Build.RgbLcdDisplay().SetText("").SetBacklightRgb(0, 0, 0);

        //        var sonicSensor = DeviceFactory.Build.UltraSonicSensor(Pin.DigitalPin8);
        //        var dist = sonicSensor.MeasureInCentimeters();

        //        //groveBuzzer.ChangeState(SensorStatus.Off);
        //        if (dist < 50 && dist > 2)
        //        {
        //            //DO Cammera capture
        //            var imageResult = await PhotoManager.TakeAndPersistPhoto();
        //            //IotupLoadData
        //            EntryData eData = new EntryData() { facesDetected = imageResult.faceDetected, imageUri = imageResult.imageUri, personIdentified = true, timeStamp = DateTime.Now  };


        //            if (timeEntered > DateTime.MinValue)
        //            {
        //                //We have someone inside, check if he identified within 20s time windows, if not ring buzzer
        //                if ((DateTime.Now - timeEntered).Seconds > 20)
        //                {
        //                    groveBuzzer.ChangeState(SensorStatus.On);
        //                    if (notSent)
        //                    {
        //                        //We need to send information about unauthorized access to NotHub, PersonIdentified = false
        //                        eData.personIdentified = false;
        //                        notSent = true;
        //                    }
        //                }
        //            }
        //            else
        //            {
        //                timeEntered = DateTime.Now;
        //                notSent = false;
        //            }

        //            //IotupLoadData
        //            await AzureIoTHub.SendDeviceToCloudMessageAsync(JsonConvert.SerializeObject(eData));
        //        }

        //        //IotupLoadData
        //        SensorData data;
        //        data = new SensorData { id = Guid.NewGuid(), temp = dhttSensor.TemperatureInCelsius, humidity = dhttSensor.Humidity, timeStamp = DateTime.Now };
        //        await AzureIoTHub.SendDeviceToCloudMessageAsync(JsonConvert.SerializeObject(data));

        //    }
        //    catch(Exception e)
        //    {
        //        Debug.WriteLine(e.Message);
        //    }
        //}

        /// <summary>
        /// Non Raspberry simulator
        /// </summary>
        /// <param name="state"></param>
        private async void TimerCallBack(object state)
        {
            try
            {

                //Receive message from IoT Hub if any
                var res = await AzureIoTHub.ReceiveCloudToDeviceMessageAsync();
                if (res != null)
                {
                    dynamic d = JObject.Parse(res);

                    if (d.powerDisplay != null)
                        RaspberrySettings.PowerDisplay = (bool)d.powerDisplay;

                    if (d.personIdentified != null)
                        Buzzer.IsOn = false;

                }

               
                measures = $"temp: {Temp.Value.ToString("F2")} C\nhumidity: {Hum.Value.ToString("F2")}";

                if (RaspberrySettings.PowerDisplay)
                {
                    Display.Text = measures;
                    Display.Background = new SolidColorBrush(Colors.LightGreen);
                }
                else
                {
                    Display.Text = "";
                    Display.Background = new SolidColorBrush(Colors.White);
                }


                var dist = Distance.Value;

                //groveBuzzer.ChangeState(SensorStatus.Off);
                if (dist < 50 && dist > 2)
                {
                    //DO Cammera capture
                    var imageResult = await PhotoManager.TakeAndPersistPhoto();
                    //IotupLoadData
                    EntryData eData = new EntryData() { facesDetected = imageResult.faceDetected, imageUri = imageResult.imageUri, personIdentified = true, timeStamp = DateTime.Now };


                    if (timeEntered > DateTime.MinValue)
                    {
                        //We have someone inside, check if he identified within 20s time windows, if not ring buzzer
                        if ((DateTime.Now - timeEntered).Seconds > 20)
                        {
                            Buzzer.IsOn = true;
                            if (notSent)
                            {
                                //We need to send information about unauthorized access to NotHub, PersonIdentified = false
                                eData.personIdentified = false;
                                notSent = true;
                            }
                        }
                    }
                    else
                    {
                        timeEntered = DateTime.Now;
                        notSent = false;
                    }

                    //IotupLoadData
                    await AzureIoTHub.SendDeviceToCloudMessageAsync(JsonConvert.SerializeObject(eData));
                }

                //IotupLoadData
                SensorData data;
                data = new SensorData { id = Guid.NewGuid(), temp = Temp.Value, humidity = Hum.Value, timeStamp = DateTime.Now };
                await AzureIoTHub.SendDeviceToCloudMessageAsync(JsonConvert.SerializeObject(data));

            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            periodicTimer = new Timer(this.TimerCallBack, null, 0, 2000);
            //LED
            //var led = _deviceFactory.Led(Pin.DigitalPin4);
            //while (true)
            //{
            //    led.ChangeState(SensorStatus.On);
            //    Task.Delay(500).Wait();
            //    led.ChangeState(SensorStatus.Off);
            //    Task.Delay(500).Wait();
            //}

            //while (true)
            //{
            //    
            //    await Task.Delay(10000);
            //}
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
          
        }

    }
}
