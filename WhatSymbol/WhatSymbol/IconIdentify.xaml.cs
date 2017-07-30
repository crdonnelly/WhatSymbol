using Plugin.Media;
using Plugin.Media.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using WhatSymbol.Model;
using Newtonsoft.Json;

namespace WhatSymbol
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class IconIdentify : ContentPage
    {
        public IconIdentify()
        {
            InitializeComponent();
        }

        private async void loadCamera(object sender, EventArgs e)
        {
            await CrossMedia.Current.Initialize();

            if (!CrossMedia.Current.IsCameraAvailable || !CrossMedia.Current.IsTakePhotoSupported)
            {
                await DisplayAlert("No Camera", ":( No camera available.", "OK");
                return;
            }

            MediaFile file = await CrossMedia.Current.TakePhotoAsync(new StoreCameraMediaOptions
            {
                PhotoSize = PhotoSize.Medium,
                Directory = "Sample",
                Name = $"{DateTime.UtcNow}.jpg"
            });

            if (file == null)
                return;

            IconAnalysis.Source = ImageSource.FromStream(() =>
            {
                return file.GetStream();
            });

            await MakePredictionRequest(file);
        }

        static byte[] GetImageAsByteArray(MediaFile file)
        {
            var stream = file.GetStream();
            BinaryReader binaryReader = new BinaryReader(stream);
            return binaryReader.ReadBytes((int)stream.Length);
        }

        async Task MakePredictionRequest(MediaFile file)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Prediction-Key", "0a871b8eddf64254a961e3e6175096b8");

            string url = "https://southcentralus.api.cognitive.microsoft.com/customvision/v1.0/Prediction/a6c35636-8994-4c58-a6f0-7df78a8a1c93/image?iterationId=e4dd5d54-e6ad-4559-9a59-e00f265b00e8";

            HttpResponseMessage response;

            byte[] byteData = GetImageAsByteArray(file);

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseString = await response.Content.ReadAsStringAsync();

                    EvaluationModel responseModel = JsonConvert.DeserializeObject<EvaluationModel>(responseString);

                    double percent = 0.5;
                    string myString = "Unidentified";

                    List<Prediction> myTag = responseModel.Predictions;
                    foreach (Prediction p in myTag)
                    {
                        if(p.Probability > percent)
                        {
                            percent = p.Probability;
                            myString = p.Tag;
                        }
                    }

                    TagLabel.Text = myString;
                }

                file.Dispose();
            }
        }
    }
}