using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;

namespace IotUWPDemo
{
    public class LocalFaceDetector
    {
        static FaceDetector  _faceDetector = null;
        public static async Task<bool> DetectFacesLocalyAsync(BitmapDecoder bitmapDecoder) {

            if (_faceDetector == null)
                _faceDetector = await FaceDetector.CreateAsync();

            SoftwareBitmap image =
                       await
                           bitmapDecoder.GetSoftwareBitmapAsync(bitmapDecoder.BitmapPixelFormat,
                               BitmapAlphaMode.Premultiplied);

            const BitmapPixelFormat faceDetectionPixelFormat = BitmapPixelFormat.Gray8;
            if (image.BitmapPixelFormat != faceDetectionPixelFormat)
            {
                image = SoftwareBitmap.Convert(image, faceDetectionPixelFormat);
            }
            IEnumerable<DetectedFace> detectedFaces = await _faceDetector.DetectFacesAsync(image);
            return detectedFaces.Any();
        }
    }
}
