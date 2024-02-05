using Newtonsoft.Json;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using TelegramChatGPT.Interfaces;

namespace TelegramChatGPT.Implementation
{
    internal static class Utils
    {
        public static async Task<Stream> GetStreamFromUrlAsync(Uri url, CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return await Task.FromCanceled<Stream>(cancellationToken).ConfigureAwait(false);
            }

            using (var httpClient = new HttpClient())
            {
                var bytes = await httpClient.GetByteArrayAsync(url, cancellationToken).ConfigureAwait(false);
                return new MemoryStream(bytes);
            }
        }

        public static async Task<string> EncodeImageToBase64(Uri imageUrl,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return await Task.FromCanceled<string>(cancellationToken).ConfigureAwait(false);
            }

            using (var httpClient = new HttpClient())
            {
                var imageBytes = await httpClient.GetByteArrayAsync(imageUrl, cancellationToken).ConfigureAwait(false);
                return Convert.ToBase64String(imageBytes);
            }
        }

        private static async Task<T?> GetJsonResponse<T>(HttpRequestMessage request,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return await Task.FromCanceled<T>(cancellationToken).ConfigureAwait(false);
            }

            using (var client = new HttpClient())
            using (var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                {
                    return default;
                }

                string result = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                return JsonConvert.DeserializeObject<T>(result);
            }
        }

        public static async Task<T?> DoRequest<T>(object payload, string endpoint, string apiKey,
            CancellationToken cancellationToken = default)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return await Task.FromCanceled<T>(cancellationToken).ConfigureAwait(false);
            }

            HttpRequestMessage CreateRequest()
            {
                string jsonPayload = JsonConvert.SerializeObject(payload);
                var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
                { Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json") };

                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                return request;
            }

            using (var request = CreateRequest())
            {
                return await GetJsonResponse<T>(request, cancellationToken).ConfigureAwait(false);
            }
        }

        public static int MessageIdToInt(MessageId s)
        {
            return int.Parse(s.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        public static long StrToLong(string s)
        {
            return long.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }
    }
}