using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PicNShop.Services
{
    internal interface IImageSearchService
    {
        /// Gets visually similar products from an image stream.
        Task<IList<ImageRes>> GetSimilarProductImagesAsync(Stream stream);

        /// Gets isually similar products from an image URL.
        Task<IList<ImageRes>> GetSimilarProductImagesAsync(string url);
    }
}
