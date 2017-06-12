using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Blob;
using Windows.System;
using Windows.Web.Http;
using Newtonsoft.Json;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace IoTDemoMobileApp
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

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            CollectionViewSource cvs = new CollectionViewSource();
            DateTime time = DateTime.Now.Date.AddDays(-6);
            while (time <= DateTime.Now.Date)
            {
                PivotItem pi = new PivotItem();
                pi.Header = time.DayOfWeek.ToString();
                
               
                List<Entry> entries;
                using (var client = new HttpClient())
                {
                    var s = $"[MobileBackendbaseUrl]/api/Entries/Get/{time.Month}-{time.Day}-{time.Year}"; 
                    var response =
                        await client.GetStringAsync(new Uri(s));
                    entries = JsonConvert.DeserializeObject<List<Entry>>(response);
                    Grid g = new Grid();
                    ListView lw = new ListView();
                    lw.ItemTemplate = ListViewItemTemplate;
                    lw.ItemsSource = entries;
                    g.Children.Add(lw);
                 
                    pi.Content = g;
                    PagePivot.Items.Add(pi);
                }

                time = time.AddDays(+1);


            }
            PagePivot.SelectedIndex = 6 ; 
            
            await LoadImages();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            await LoadImages();
        }

        
        private async Task LoadImages()
        {

            var account = new CloudStorageAccount(new StorageCredentials("storageAccount", "storageKey"), true);
            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference("container");
            var result = await container.ListBlobsSegmentedAsync("", true, BlobListingDetails.None, 500,null,null,null);
            var blobs = result.Results;
            var images = new List<Image>();
            foreach(var blob in blobs)
            {
                images.Add(new Image { name = blob.StorageUri.PrimaryUri.ToString(), timestamp = DateTime.Now });
            }
            //TodaysEntrants.ItemsSource = images;
        }

        class Image
        {
            public string name { get; set; }
            public DateTime timestamp { get; set; }
        }

        private async void TodaysEntrants_ItemClick(object sender, ItemClickEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri((e.ClickedItem as Image).name));
        }
    }
}
