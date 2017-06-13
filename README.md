# IoTIntelligentAlarm
Complex solution using various Azure services, which builds app Intelligent alarm solution. This solution allows users to track who entered their household or any other object. Alarm is collecting pictures of entrants and also additional sensoric data  (temperature/humidity). It send these data to cloud (using IoTHub). Data is processed using Azure Services, while the most important part is, that the processing consist also from Face Recognition API, thanks to which we are able to determine, whether the entrant is known or unknown person (burglar). Solution is proactively sending notifications after there is a new entry, to mobile device and also to bot. Solution allows also to browse historic data (pictures of entrants) thru mobile application and also thru the bot. For further details see architecture of solution below:

![alt tag](https://raw.githubusercontent.com/MarekLani/IoTIntelligentAlarm/master/Architecture.png)

Application consists from following parts:

### Alarm device

Originally alarm device was built as a UWP application which runs on Raspberry 3 device. There were sensors attached which measures temperature, humidity and distance ultrasonic sensor (door sensor). Alarm device consist also from web camera and display (used for displaying of temperature and humidity data). All the sensors are part of  [Grove Pi Raspberry Starter Kit](https://www.seeedstudio.com/GrovePi%2B-Starter-Kit-for-Raspberry-Pi-p-2240.html). Alarm device is sending temperature and humidity data continuously to IoTHub. Once the door opens (distance measured by distance sensor decreases) alarm starts to take pictures and uploads them to blob storage. Metadata is sent to IoT Hub. This sample can be found in IoTUWPDemo folder. 

Alarm uses two way communication with IoTHub. It sends messages and it also waits for the commands sent by IoTHub. There are two types of commands that can arrive to device. First turns on display, which then displays actual temperature and humidity. Second command is used to turn of alarm, when the person who entered gets recognized thru Face API.

As during presentations there were often problems with connecting Raspberry device to internet, I have decided to create simulated Alarm device. It simulates sensors thru set of sliders. This app can be found in *IoTUWPDemoSimulatorDevice* folder.   

### Data Ingress and initial filtering 

As stated for data ingress I have used Azure IoT Hub Service, which is able to ingress millions of messages per second, and thus gives a lot of space to scale this solution and support communication thousands of alarm devices. Besides receiving of messages I used IoT Hub to easily communicate to device (as stated in previous section). Initial data processing is done thru Stream Analytics Service, which inserts search sensor data for anomalies, resp. alert events (e.g. too high temperature) sensor data to DocumentDB, for further visualization needs. It also filter messages, which contain image reference (means that message is related to entry event) and writes these messages to event hub, where it gets picked up by Azure function. 

### Entry message processing function

As there is need to execute more complex logic then Stream Analytics allows, we have decided to do this weight lifting within Azure Function. This Azure Function is responsible for following:

- Calling Face API
- Notifying users about the entry events (new entry, entrant recognized, unknown entrant) (bot proactive message, notification to mobile device using notification hub)
- Sending alarm deactivation command thru IoTHub, in case person was recognized.
- Write entry data to DocumenDB for further visualization needs within mobile app or 

This function was developed using [Azure Functions Tools for VS2017](https://blogs.msdn.microsoft.com/webdev/2017/05/10/azure-function-tools-for-visual-studio-2017/) as a class library web app project. Code can be found in folder *Functions->Local Function*. Thanks to the Azure Functions Tools this function can be run locally or deployed to Azure easily from within the Visual Studio.

### Presentation layer

There are three different ways how the information are surfaced to user:

1. **Power BI,** which connects to DocumentDB (CosmosDB) and displays historic sensoric data. We are also pushing real-time streaming dataset thru Stream Analytics, which is then visualized using Power BI.
2. **Mobile Application** - allows user to list all the entrants photos for the last week and also notifies user when there is new entry activity. This application connects to mobile app backend, which is build as a Azure Mobile App (using .NET) and which connects to DocumentDB. You can find code of Mobile UWP application in *IoTDemoMobileApp* folder. Code of Mobile App backend can be found in *MobileBackendClassic* folder.
3. **Bot** - bot is implemented using Microsoft Bot Framework .NET SDK. It utilizes Language Understanding Intelligent Service (LUIS.ai) and allows users to show last entry, show last identified entrant, show who arrived to premises (house) today, and when certain person entered premises last. It also implements proactive messaging, which is used for notifying user about new entries.