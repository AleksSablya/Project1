using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using HttpContentLib.Interfaces;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HttpContentLib
{
    public class HttpContentService: IHttpContentService
    {
        private const string _hashFmt = "image_{0}";

        public async Task<string> SerializeRequestWithoutBinaryDataAsync(HttpRequestMessage request, string[] imageFielsList)
        {
            HttpRequestMessage newRequest = request;
            if (request.Content != null)
            {
                newRequest = new HttpRequestMessage(request.Method, request.RequestUri)
                {
                    Content = await CloneJsonContentAsync(request.Content, imageFielsList)
                };
                CopyHeaders(request, newRequest);
            }
            var httpMessageContent = new HttpMessageContent(newRequest);
            return await httpMessageContent.ReadAsStringAsync();
        }

        public async Task<string> SerializeResponseWithoutBinaryDataAsync(HttpResponseMessage response, string[] imageFielsList)
        {
            var newResponse = new HttpResponseMessage(response.StatusCode)
            {
                Content = await CloneJsonContentAsync(response.Content, imageFielsList)
            };
            CopyHeaders(response, newResponse);
            var httpMessageContent = new HttpMessageContent(newResponse);
            return await httpMessageContent.ReadAsStringAsync();
        }

        public async Task<string> SerializeMultipartRequestWithoutBinaryDataAsync(HttpRequestMessage request)
        {
            //var streamProvider = new MultipartMemoryStreamProvider();
            var streamProvider = await request.Content.ReadAsMultipartAsync();
            var newContent = new MultipartFormDataContent();
            foreach (var part in streamProvider.Contents)
            {
                if (part.Headers.ContentType?.MediaType != null && part.Headers.ContentType.MediaType.StartsWith("image"))
                {
                    var byteContent = await part.ReadAsByteArrayAsync() ?? Array.Empty<byte>();
                    var contentHash = GetHash(byteContent);
                    var newpart = new ByteArrayContent(Encoding.UTF8.GetBytes(string.Format(_hashFmt, contentHash)));
                    newpart.Headers.ContentType = part.Headers.ContentType;
                    var contentDispositionType = part.Headers.ContentDisposition?.DispositionType ?? "form-data";
                    newpart.Headers.ContentDisposition = new ContentDispositionHeaderValue(contentDispositionType)
                    {
                        Name = part.Headers.ContentDisposition?.Name ?? "Name",
                        FileName = part.Headers.ContentDisposition?.FileName
                    };
                    newContent.Add(newpart);
                }
                else
                {
                    newContent.Add(part);
                }
            }

            var newRequest = new HttpRequestMessage(request.Method, request.RequestUri)
            {
                Content = newContent
            };
            CopyHeaders(request, newRequest);
            var httpMessageContent = new HttpMessageContent(newRequest);
            return await httpMessageContent.ReadAsStringAsync();
        }

        public static async Task<HttpRequestMessage> DeserializeToRequestAsync(string text)
        {
            var request = new HttpRequestMessage
            {
                Content = new StringContent(text)
            };
            request.Content.Headers.Remove("Content-Type");
            request.Content.Headers.Add("Content-Type", "application/http;msgtype=request");
            return await request.Content.ReadAsHttpRequestMessageAsync();
        }

        public static async Task<HttpResponseMessage> DeserializeToResponseAsync(string text)
        {
            var response = new HttpResponseMessage
            {
                Content = new StringContent(text)
            };
            response.Content.Headers.Remove("Content-Type");
            response.Content.Headers.Add("Content-Type", "application/http;msgtype=response");
            return await response.Content.ReadAsHttpResponseMessageAsync();
        }

        #region Private methods

        private static void CopyHeaders(HttpRequestMessage source, HttpRequestMessage destination)
        {
            foreach (var header in source.Headers)
            {
                if (destination.Headers.Contains(header.Key))
                    destination.Headers.Remove(header.Key);
                destination.Headers.Add(header.Key, header.Value);
            }
        }

        private static void CopyHeaders(HttpResponseMessage source, HttpResponseMessage destination)
        {
            foreach (var header in source.Headers)
            {
                if (destination.Headers.Contains(header.Key))
                    destination.Headers.Remove(header.Key);
                destination.Headers.Add(header.Key, header.Value);
            }
        }

        private static async Task<HttpContent> CloneJsonContentAsync(HttpContent content, string[] imageFields)
        {
            var json = await content.ReadAsStringAsync();
            var jobj = JsonConvert.DeserializeObject<JObject>(json);
            if (jobj == null)
            {
                return content;
            }
            SearchAndReplaceImages(jobj, imageFields);

            var newContent = new StringContent(JsonConvert.SerializeObject(jobj),
                !string.IsNullOrEmpty(content.Headers.ContentType?.CharSet)
                    ? Encoding.GetEncoding(content.Headers.ContentType.CharSet)
                    : Encoding.UTF8,
                content.Headers.ContentType?.MediaType ?? "application/json");

            return newContent;
        }

        private static void SearchAndReplaceImages(JObject jobj, string[] imageFields)
        {
            var props = jobj.Properties().ToList();
            foreach (var prop in props)
            {
                if (prop.Value is JObject @object)
                {
                    SearchAndReplaceImages(@object, imageFields);
                }
                else if (prop.Value != null && prop.Value.Type == JTokenType.String &&
                    imageFields.Select(x => x.ToLower()).Contains(prop.Name.ToLower()))
                {
                    var valueHash = GetHash(prop.Value.ToString());
                    prop.Value = string.Format(_hashFmt, valueHash);
                }
            }
        }

        private static string GetHash(string content) => GetHash(Encoding.UTF8.GetBytes(content));

        private static string GetHash(byte[] content)
        {
            using MD5 md5 = MD5.Create();
            var requestHash = md5.ComputeHash(content);
            return Convert.ToBase64String(requestHash);
        }

        #endregion
    }
}
