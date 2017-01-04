using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.Bot.Connector;
using PicNShop.Services;

namespace PicNShop.Controllers
{
    [BotAuthentication]
    public class MessagesController : ApiController
    {
        private readonly IImageSearchService imageService = new BingImageSearchService();

        public async Task<HttpResponseMessage> Post([FromBody]Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                var connector = new ConnectorClient(new Uri(activity.ServiceUrl));
                string msgResponse = null;
                bool ifReplied = false;
                try
                {
                    var images = await this.GetSimilarProducts(activity, connector);
                    if (images.Any() && images!=null)
                    {
                        Activity reply = activity.CreateReply("Yay! I found some visually similar products along with their buying site link.");
                        reply.Type = ActivityTypes.Message;
                        reply.AttachmentLayout = "carousel";
                        reply.Attachments = this.BuildImageAttachments(images.Take(5));
                        await connector.Conversations.ReplyToActivityAsync(reply);
                        ifReplied = true;
                    }
                    else
                    {
                        msgResponse = "Alas! I couldn't find visually similar products. Please try with another image/image URL.";
                    }
                }
                catch (ArgumentException e)
                {
                    msgResponse = "I am PicNShop- an intelligent bot! " +
                        "Send me an image/image URL and I will suggest visually similar products along with their buying site link.";
                    Trace.TraceError(e.ToString());
                }
                catch (Exception e)
                {
                    msgResponse = "Oops! Something went wrong. Try again later.";
                    Trace.TraceError(e.ToString());
                }

                if (!ifReplied)
                {
                    Activity reply = activity.CreateReply(msgResponse);
                    await connector.Conversations.ReplyToActivityAsync(reply);
                }
            }
            else
            {
                await this.HandleSystemMessage(activity);
            }

            var response = this.Request.CreateResponse(HttpStatusCode.OK);
            return response;
        }

        private async Task<IList<ImageRes>> GetSimilarProducts(Activity activity, ConnectorClient connector)
        {
            var imageAttachment = activity.Attachments?.FirstOrDefault(a => a.ContentType.Contains("image"));
            if (imageAttachment != null)
            {
                using (var stream = await GetImageStream(connector, imageAttachment))
                {
                    return await this.imageService.GetSimilarProductImagesAsync(stream);
                }
            }

            string url;
            if (TryParseAnchorTag(activity.Text, out url))
            {
                return await this.imageService.GetSimilarProductImagesAsync(url);
            }
            if (Uri.IsWellFormedUriString(activity.Text, UriKind.Absolute))
            {
                return await this.imageService.GetSimilarProductImagesAsync(activity.Text);
            }

            throw new ArgumentException("Not a valid image/image URL.");
        }

        private IList<Attachment> BuildImageAttachments(IEnumerable<ImageRes> images)
        {
            var attachments = new List<Attachment>();
            foreach (var image in images)
            {
                var pAttachment = new Attachment { ContentType = "application/vnd.microsoft.card.hero" };

                var pCard = new HeroCard
                {
                    Title = image.ImgName,
                    Subtitle = image.HPageDisplayURL,
                    Images = new List<CardImage>()
                };
                
                var cardImg = new CardImage { Url = image.ThumbnailURL };
                pCard.Images.Add(cardImg);

                pCard.Buttons = new List<CardAction>();
                var pButtonBuy = new CardAction();
                var pButtonSearch = new CardAction();

                pButtonBuy.Title = "Buy";
                pButtonBuy.Type = "openUrl";
                pButtonBuy.Value = image.HPageURL;

                pButtonSearch.Title = "Search more";
                pButtonSearch.Type = "openUrl";
                pButtonSearch.Value = image.WebURL;

                pCard.Buttons.Add(pButtonBuy);
                pCard.Buttons.Add(pButtonSearch);
                pAttachment.Content = pCard;

                attachments.Add(pAttachment);
            }

            return attachments;
        }

        private static async Task<Stream> GetImageStream(ConnectorClient connector, Attachment imageAttachment)
        {
            using (var httpClient = new HttpClient())
            {
                var uri = new Uri(imageAttachment.ContentUrl);

                //for skype
                if (uri.Host.EndsWith("skype.com") && uri.Scheme == "https")
                {
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(connector));
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));
                }
                else
                {
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(imageAttachment.ContentType));
                }

                return await httpClient.GetStreamAsync(uri);
            }
        }

        /// Gets the href value in an anchor element.
        ///  Skype transforms raw urls to html. Here we extract the href value from the url
        private static bool TryParseAnchorTag(string text, out string url)
        {
            var regex = new Regex("^<a href=\"(?<href>[^\"]*)\">[^<]*</a>$", RegexOptions.IgnoreCase);
            url = regex.Matches(text).OfType<Match>().Select(m => m.Groups["href"].Value).FirstOrDefault();
            return url != null;
        }

        /// Gets JwT token 
        private static async Task<string> GetTokenAsync(ConnectorClient connector)
        {
            var credentials = connector.Credentials as MicrosoftAppCredentials;
            if (credentials != null)
            {
                return await credentials.GetTokenAsync();
            }

            return null;
        }

        private async Task<Activity> HandleSystemMessage(Activity activity)
        {
            switch (activity.Type)
            {
                case ActivityTypes.DeleteUserData:
                    break;
                case ActivityTypes.ConversationUpdate:
                    if (activity.MembersAdded.Any(m => m.Id == activity.Recipient.Id))
                    {
                        var Uconnector = new ConnectorClient(new Uri(activity.ServiceUrl));

                        var Uresponse = activity.CreateReply();
                        Uresponse.Text = "Welcome New User!";

                        await Uconnector.Conversations.ReplyToActivityAsync(Uresponse);
                    }
                    break;
                case ActivityTypes.ContactRelationUpdate:
                    break;
                case ActivityTypes.Typing:
                    {
                        var Uconnector = new ConnectorClient(new Uri(activity.ServiceUrl));

                        var Uresponse = activity.CreateReply();
                        Uresponse.Text = "I am more of a visual bot. Please send an image/image URL.";

                        await Uconnector.Conversations.ReplyToActivityAsync(Uresponse);
                    }
                    break;
                case ActivityTypes.Ping:
                    {
                        var connector = new ConnectorClient(new Uri(activity.ServiceUrl));

                        var response = activity.CreateReply();
                        response.Text = "Ping successful!";

                        await connector.Conversations.ReplyToActivityAsync(response);
                    }
                    break;
            }
            return null;
        }
    }
}