using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace IotUWPDemo
{
    public class PhotoManager
    {

        private static string storageConnectionString = "storageConnectionString";
        private static string storageContainer = "container";

        private static MediaCapture camera = null;

        public static async Task<(string imageUri, bool faceDetected)> TakeAndPersistPhoto()
        {
            if(camera == null)
            {
                await InitializeCameraAsync();
            }
            return await Capture();
        }

        private static async Task InitializeCameraAsync()
        {
            DeviceInformationCollection videoDevices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture);

            if (!videoDevices.Any())
            {
                Debug.WriteLine("No cameras found.");
            }

            var device = videoDevices.First();
            MediaCapture mediaCapture = new MediaCapture();

            MediaCaptureInitializationSettings mediaInitSettings = new MediaCaptureInitializationSettings
            {
                VideoDeviceId = device.Id
            };

            await mediaCapture.InitializeAsync(mediaInitSettings);
            await SetMaxResolution(mediaCapture);
            camera = mediaCapture;
        }

        private static async Task SetMaxResolution(MediaCapture mediaCapture)
        {
            IReadOnlyList<IMediaEncodingProperties> res =
                mediaCapture.VideoDeviceController.GetAvailableMediaStreamProperties(MediaStreamType.VideoPreview);
            uint maxResolution = 0;
            int indexMaxResolution = 0;

            if (res.Count >= 1)
            {
                for (int i = 0; i < res.Count; i++)
                {
                    VideoEncodingProperties vp = (VideoEncodingProperties)res[i];

                    if (vp.Width <= maxResolution) continue;
                    indexMaxResolution = i;
                    maxResolution = vp.Width;
                }
                await
                    mediaCapture.VideoDeviceController.SetMediaStreamPropertiesAsync(MediaStreamType.VideoPreview,
                        res[indexMaxResolution]);
            }
        }

        private static async Task<Tuple<BitmapDecoder, IRandomAccessStream>> GetPhotoStreamAsync(
            MediaCapture mediaCapture)
        {
            InMemoryRandomAccessStream photoStream = new InMemoryRandomAccessStream();
            await mediaCapture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), photoStream);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(photoStream);
            return new Tuple<BitmapDecoder, IRandomAccessStream>(decoder, photoStream.CloneStream());
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>returns blob name + check whether photo consist faces </returns>
        private static async Task<(string imageUri, bool faceDetected)> Capture()
        {
            bool facesDetected = false;

            Debug.WriteLine($"Processing camera");
            BitmapDecoder bitmapDecoder;
            IRandomAccessStream imageStream;
            try
            {
                Tuple<BitmapDecoder, IRandomAccessStream> photoData = await GetPhotoStreamAsync(camera);
                bitmapDecoder = photoData.Item1;

                facesDetected  = await LocalFaceDetector.DetectFacesLocalyAsync(bitmapDecoder);

                imageStream = photoData.Item2;

                Debug.WriteLine($"Got stream from camera");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Camera  failed with message: {ex.Message}");
                return ("",false);
            }
#pragma warning disable 4014

            var imageUri =  await UploadImageToBlobAsync(ProcessImage(bitmapDecoder,imageStream));
            if (imageUri == null)
                return ("", false);
            else
            {
                return ( imageUri, facesDetected );
            }
#pragma warning restore 4014
        }


        private static async Task<string> UploadImageToBlobAsync(Stream imageStream)
        {
            if (imageStream == null)
                return null;

            var blobName = Guid.NewGuid().ToString() +"_"+ DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString()+ DateTime.Now.Minute.ToString() + ".jpg";
            Debug.WriteLine("Image uploaded.");

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = blobClient.GetContainerReference(storageContainer);
            await container.CreateIfNotExistsAsync();
            CloudBlockBlob blockBlob = container.GetBlockBlobReference(blobName);
            imageStream.Seek(0, SeekOrigin.Begin);
            await blockBlob.UploadFromStreamAsync(imageStream);

            return blobName;
        }

        private static Stream ProcessImage(BitmapDecoder bitmapDecoder, IRandomAccessStream imageStream)
        {
            try
            {
                MemoryStream imageMemoryStream = new MemoryStream();
                //var image = new ImageProcessorCore.Image(imageStream.AsStreamForRead());
                //image.SaveAsJpeg(imageMemoryStream, 80);

                //return imageMemoryStream;
                return imageStream.AsStreamForRead();
            }
            catch(Exception e)
            {
                Debug.WriteLine($"Exception when processingImage: {e.Message}");
                return null;
            }
    }
    }
}
