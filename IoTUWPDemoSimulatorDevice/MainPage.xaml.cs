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
using IoTUWPDemo;
using Windows.UI.Core;
using Windows.System.Threading;
using Windows.ApplicationModel.Core;
using Windows.Media.Capture;
using Windows.System.Display;
using Windows.Graphics.Display;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IotUWPDemo
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        ViewModel vm;
        MediaCapture _mediaCapture;
        bool _isPreviewing;
        DisplayRequest _displayRequest = new DisplayRequest();

        public MainPage()
        {
            this.InitializeComponent();
            vm = new ViewModel();
            this.DataContext = vm;
        }
        // private readonly IBuildGroveDevices _deviceFactory = DeviceFactory.Build;
        private Timer periodicTimer;
        private String measures = "";

        private bool notSent = false;
        private DateTime timeEntered = DateTime.MinValue;

       

        /// <summary>
        /// Non Raspberry simulator
        /// </summary>
        /// <param name="state"></param>
        private async void TimerCallBack(object state)
        {
            try
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
    async () =>
    {
        var res = await AzureIoTHub.ReceiveCloudToDeviceMessageAsync();

        try
        {
            if (res != null)
            {
                dynamic d = JObject.Parse(res);

                if (d.powerDisplay != null)
                    RaspberrySettings.PowerDisplay = (bool)d.powerDisplay;

                if (d.personIdentified != null)
                {
                    timeEntered = DateTime.MinValue;
                    notSent = false;
                    vm.Buzzer = false;
                }

            }
        }
        catch (Exception e)
        {
            Debug.WriteLine(e.Message);
        }


        measures = $"temp: {vm.Temp.ToString("F2")} C\nhumidity: {vm.Hum.ToString("F2")}";

        if (RaspberrySettings.PowerDisplay)
        {
            vm.DisplayText = measures;
            vm.DisplayBackground = new SolidColorBrush(Colors.LightGreen);
        }
        else
        {
            vm.DisplayText = "";

            vm.DisplayBackground = new SolidColorBrush(Colors.White);
        }


        var dist = vm.Distance;

        //groveBuzzer.ChangeState(SensorStatus.Off);
        if (dist < 50 && dist > 2)
        {
            if (timeEntered == DateTime.MinValue)
            {
                timeEntered = DateTime.Now;
            }
        }

        //if timeEntered is set, do capturing
        if (timeEntered != DateTime.MinValue)
        {
            //DO Cammera capture
            var imageResult = await PhotoManager.TakeAndPersistPhoto();
            //IotupLoadData
            EntryData eData = new EntryData() { facesDetected = imageResult.faceDetected, imageUri = imageResult.imageUri, personIdentified = true, timeStamp = DateTime.Now };

            //We have someone inside, check if he identified within 30s time windows, if not ring buzzer
            if ((DateTime.Now - timeEntered).Seconds > 20 && !notSent)
            {
                vm.Buzzer = true;
                //We need to send information about unauthorized access to NotHub, PersonIdentified = false
                eData.personIdentified = false;
                notSent = true;
            }

            //IotupLoadData
            await AzureIoTHub.SendDeviceToCloudMessageAsync(JsonConvert.SerializeObject(eData));
        }

        //IotupLoadData
        SensorData data;
        data = new SensorData { id = Guid.NewGuid(), temp = vm.Temp, humidity = vm.Hum, timeStamp = DateTime.Now };
        await AzureIoTHub.SendDeviceToCloudMessageAsync(JsonConvert.SerializeObject(data));
    });
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            //this.DataContext = vm;

            //calibrate cammera
            await StartPreviewAsync();
            await CleanupCameraAsync();
            ThreadPoolTimer timer = ThreadPoolTimer.CreatePeriodicTimer(this.TimerCallBack, TimeSpan.FromSeconds(5));
            
        }

        private async Task StartPreviewAsync()
        {
            try
            {

                _mediaCapture = new MediaCapture();
                await _mediaCapture.InitializeAsync();

                _displayRequest.RequestActive();
                DisplayInformation.AutoRotationPreferences = DisplayOrientations.Landscape;
            }
            catch (UnauthorizedAccessException)
            {
                // This will be thrown if the user denied access to the camera in privacy settings
                //ShowMessageToUser("The app was denied access to the camera");
                return;
            }

            try
            {
                PreviewControl.Source = _mediaCapture;
                await _mediaCapture.StartPreviewAsync();
                _isPreviewing = true;
            }
            catch (System.IO.FileLoadException)
            {
                //_mediaCapture.Capturede .CaptureDeviceExclusiveControlStatusChanged += _mediaCapture_CaptureDeviceExclusiveControlStatusChanged;
            }

        }

        private async Task CleanupCameraAsync()
        {
            if (_mediaCapture != null)
            {
                if (_isPreviewing)
                {
                    await _mediaCapture.StopPreviewAsync();
                }

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    PreviewControl.Source = null;
                    if (_displayRequest != null)
                    {
                        _displayRequest.RequestRelease();
                    }

                    _mediaCapture.Dispose();
                    _mediaCapture = null;
                });
            }

        }
    }
}
