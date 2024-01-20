using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjectEarthLauncher
{
    internal static class HttpClientExtensions
    {
        // https://stackoverflow.com/a/46497896/15878562
        public static async Task DownloadAsync(this HttpClient client, string requestUri, Stream destination, Action<long, long> progress = null, CancellationToken cancellationToken = default)
        {
            // Get the http headers first to examine the content length
            using (var response = await client.GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead))
            {
                var contentLength = response.Content.Headers.ContentLength;

                using (Stream download = await response.Content.ReadAsStreamAsync(cancellationToken))
                {

                    // Ignore progress reporting when no progress reporter was 
                    // passed or when the content length is unknown
                    if (progress == null || !contentLength.HasValue)
                    {
                        await download.CopyToAsync(destination);
                        return;
                    }

                    // Use extension method to report progress while downloading
                    await download.CopyToAsync(destination, 81920, prog => progress?.Invoke(prog, contentLength.Value), cancellationToken);
                }
            }
        }
    }
}
