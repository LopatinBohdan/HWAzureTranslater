using HWAzureTranslater.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace HWAzureTranslater.Controllers
{
    public class HomeController : Controller
    {
        List<Language> languages;
        string key;
        string endPoint;
        string region;
        string origin;
        string translate;
        string from;
        string to;

        string keyVision;
        string endPointVision;
        string regionVision;
        ComputerVisionClient computerVision;

        public HomeController()
        {
            languages = JsonConvert.DeserializeObject<List<Language>>(System.IO.File.ReadAllText("lang.json"));
            key = "628fd4c431a64c98ae7a0fc1d507ca8f";
            endPoint = "https://api.cognitive.microsofttranslator.com/";
            region = "switzerlandnorth";

            keyVision = "18145f58fb0d44e5a01e60da9cadbdf5";
            endPointVision = "https://lopatincompvision.cognitiveservices.azure.com/";
            regionVision = "germanywestcentral";
            computerVision = new ComputerVisionClient(new ApiKeyServiceClientCredentials(keyVision)) { Endpoint = endPointVision };
        }

        public IActionResult Index()
        {
            ViewBag.Language = languages;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TranslateText()
        {
            origin = Request.Form["origin"];
            from = Request.Form["from"];
            to = Request.Form["to"];

            using (HttpClient httpClient=new HttpClient())
            {
                using (HttpRequestMessage requestMessage=new HttpRequestMessage())
                {
                    requestMessage.Method = HttpMethod.Post;
                    requestMessage.RequestUri = new Uri(endPoint + $"translate?api-version=3.0&from={from}&to={to}");
                    requestMessage.Headers.Add("Ocp-Apim-Subscription-Key", key);
                    requestMessage.Headers.Add("Ocp-Apim-Subscription-Region", region);

                    object[] body=new object[] { new { Text=origin} };

                    string request=JsonConvert.SerializeObject(body);
                    requestMessage.Content = new StringContent(request, Encoding.UTF8, "application/json");

                    HttpResponseMessage httpResponse=await httpClient.SendAsync(requestMessage).ConfigureAwait(false);
                    string json=httpResponse.Content.ReadAsStringAsync().Result;

                    MyRoot[] myRoots = JsonConvert.DeserializeObject<MyRoot[]>(json);

                    foreach (MyRoot item in myRoots)
                    {
                        foreach (TranslationForTranslate subitem in item.translations)
                        {
                            ViewBag.Translate = subitem.text;
                        }
                    }
                }
            }

                return View("Index");
        }

        [HttpPost]
        public async Task<IActionResult> TranslateImage()
        {
            string imgUrl = Request.Form["imgUrl"];
            to = Request.Form["imgTo"];

            List<VisualFeatureTypes?> featureTypes = Enum.GetValues(typeof(VisualFeatureTypes)).OfType<VisualFeatureTypes?>().ToList();
            
            //ImageAnalysis analysis=await computerVision.AnalyzeImageAsync(imgUrl, featureTypes);
            var result = await computerVision.RecognizePrintedTextAsync(true, imgUrl);

            foreach (var item in result.Regions) 
            {
                foreach (var subitem in item.Lines)
                {
                    foreach (var word in subitem.Words)
                    {
                        translate += word.Text + " ";
                    }
                }
            }

            using (HttpClient httpClient=new HttpClient())
            {
                using (HttpRequestMessage requestMessage=new HttpRequestMessage())
                {
                    requestMessage.Method = HttpMethod.Post;
                    requestMessage.RequestUri = new Uri(endPoint + $"translate?api-version=3.0&to={to}");
                    requestMessage.Headers.Add("Ocp-Apim-Subscription-Key", key);
                    requestMessage.Headers.Add("Ocp-Apim-Subscription-Region", region);

                    object[] body = new object[] { new { Text = translate } };

                    string json=JsonConvert.SerializeObject(body);

                    requestMessage.Content=new StringContent(json, Encoding.Unicode, "application/json");

                    HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage).ConfigureAwait(false);

                    string responseText = await responseMessage.Content.ReadAsStringAsync();


                    MyRoot[] myRoots = JsonConvert.DeserializeObject<MyRoot[]>(responseText);//responseText

                    foreach (MyRoot item in myRoots!)
                    {
                        foreach (TranslationForTranslate subitem in item.translations)
                        {
                            ViewBag.ImgTranslate=subitem.text;
                        }
                    }
                }
            }
                return View("Index");
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}