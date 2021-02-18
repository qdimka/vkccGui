using System;
using System.Diagnostics;
using System.Threading.Tasks;
using VkNet;

namespace VkCCGui.Services
{
    public class VccResult
    {
        protected VccResult() {}

        public string ShortUrl { get; private set; }

        public string Error { get; private set; }

        public string Url { get; private set; }

        public bool IsSuccess => !string.IsNullOrEmpty(ShortUrl);

        public static VccResult Fail(string url, string error) => new VccResult()
        {
            Url = url, Error = error
        };
        
        public static VccResult Ok(string url) => new VccResult()
        {
            ShortUrl = url
        };
    }
    
    public class VkccService
    {
        private readonly VkApi _api;
        
        public VkccService(VkApi api)
        {
            _api = api;
        }

        public async Task<VccResult> GetShortLink(string url)
        {
            if (string.IsNullOrEmpty(url))
                return VccResult.Fail(url, "Невалидный url.");

            try
            {
                var result = await _api.Utils.GetShortLinkAsync(new Uri(url), false);

                return VccResult.Ok(result.ShortUrl.ToString());
            }
            catch (Exception e)
            {
                return VccResult.Fail(url, e.Message);
            }
        }
    }
}