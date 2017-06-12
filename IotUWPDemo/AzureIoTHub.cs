using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using IotUWPDemo;

static class AzureIoTHub
{
    //
    // Note: this connection string is specific to the device "Raspberry1". To configure other devices,
    // see information on iothub-explorer at http://aka.ms/iothubgetstartedVSCS
    //
    const string deviceConnectionString = "IoTHubDeviceConnectioNString";

    //
    // To monitor messages sent to device "Raspberry1" use iothub-explorer as follows:
    //    iothub-explorer HostName=marekiotdemohub.azure-devices.net;SharedAccessKeyName=service;SharedAccessKey=WQvNjhuARjVLELFsIFmbQdJCXKJraNCOiuAZc0itEcg= monitor-events "Raspberry1"
    //

    // Refer to http://aka.ms/azure-iot-hub-vs-cs-wiki for more information on Connected Service for Azure IoT Hub

    public static async Task SendDeviceToCloudMessageAsync(string data)
    {
        var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Amqp);
        var message = new Message(Encoding.ASCII.GetBytes(data));

        await deviceClient.SendEventAsync(message);
    }

    public static async Task<string> ReceiveCloudToDeviceMessageAsync()
    {
        try
        {
            var deviceClient = DeviceClient.CreateFromConnectionString(deviceConnectionString, TransportType.Amqp);


            var receivedMessage = await deviceClient.ReceiveAsync();

            if (receivedMessage != null)
            {
                var messageData = Encoding.ASCII.GetString(receivedMessage.GetBytes());
                await deviceClient.CompleteAsync(receivedMessage);
                return messageData;
            }
        }
        catch(Exception e)
        {
            return e.Message;
        }

        return null;
        
    }
}
