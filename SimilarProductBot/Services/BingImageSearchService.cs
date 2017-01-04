using System.Net.Http.Headers;
using System.Web;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace PicNShop.Services
{

        public class BingImageSearchService : IImageSearchService
        {
            private static readonly string ApiKey = "a3e2be4a5b034d1cbf6198a7ed54a8c8";

            private static readonly string BingApiUrl = "https://api.cognitive.microsoft.com/bing/v5.0/images/search?modulesRequested=SimilarProducts&mkt=en-us&form=BCSPRD";

            public async Task<IList<ImageRes>> GetSimilarProductImagesAsync(string url)
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ApiKey);

                    string apiUrl = BingApiUrl + $"&imgUrl={HttpUtility.UrlEncode(url)}";

                    var text = await httpClient.GetStringAsync(apiUrl);
                    var response = JsonConvert.DeserializeObject<BingImgResp>(text);

                    return response
                        ?.visuallySimilarProducts
                        ?.Select(i => new ImageRes
                        {
                            HPageDisplayURL = i.hostPageDisplayUrl,
                            HPageURL = i.hostPageUrl,
                            ImgName = i.name,
                            ThumbnailURL = i.thumbnailUrl,
                            WebURL = i.webSearchUrl
                        })
                        .ToList();
                }
            }

            public async Task<IList<ImageRes>> GetSimilarProductImagesAsync(Stream stream)
            {
                using (var httpClient = new HttpClient())
                {
                    httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", ApiKey);

                    var strContent = new StreamContent(stream);
                    strContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data") { FileName = "Any-Name-Works" };

                    var content = new MultipartFormDataContent();
                    content.Add(strContent);

                    var postResponse = await httpClient.PostAsync(BingApiUrl, content);
                    var text = await postResponse.Content.ReadAsStringAsync();
                    var response = JsonConvert.DeserializeObject<BingImgResp>(text);

                    return response
                        ?.visuallySimilarProducts
                        ?.Select(i => new ImageRes
                        {
                            HPageDisplayURL = i.hostPageDisplayUrl,
                            HPageURL = i.hostPageUrl,
                            ImgName = i.name,
                            ThumbnailURL = i.thumbnailUrl,
                            WebURL = i.webSearchUrl
                        })
                        .ToList();
                }
            }
        }
    }
