using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace EvilZombMapsLoader.Helpers
{
    public static class ImageHelper
    {
        // HttpClient переиспользуем — это правильно
        private static readonly HttpClient _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10) // не зависает навсегда
        };

        public static async Task<BitmapImage> GetBitmapFromUrl(string url, CancellationToken token = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(url))
                    return null;

                if (url.Contains("?ava=1"))
                    url = url.Replace("?ava=1", "");

                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                using (var response = await _http.SendAsync(
                    request,
                    HttpCompletionOption.ResponseContentRead,
                    token))
                {
                    if (!response.IsSuccessStatusCode)
                        return null;

                    // Читаем байты вручную, но корректно для .NET Framework
                    var bytes = await response.Content.ReadAsByteArrayAsync();

                    return CreateBitmap(bytes);
                }
            }
            catch (TaskCanceledException)
            {
                // Отмена или таймаут — возврат null, но без зависаний
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static BitmapImage CreateBitmap(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
                return null;

            var image = new BitmapImage();

            using (var ms = new MemoryStream(bytes))
            {
                image.BeginInit();
                image.StreamSource = ms;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
            }

            image.Freeze();
            return image;
        }
    }
}
