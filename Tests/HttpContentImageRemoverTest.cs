using System.Net;
using System.Net.Http.Headers;
using System.Text;
using HttpContentLib;

namespace Tests
{
    [TestFixture]
    public class HttpContentImageRemoverTest
    {
        private HttpContentService httpContentService;

        [SetUp]
        public void Setup()
        {
            httpContentService = new HttpContentService();
        }

        [Test]
        public async Task RequestTestAsync()
        {
            var testRequest = CreateTestRequest();
            var jsonRequest = CreateJsonRequest(JsonConvertHelper.ToJson(testRequest));

            var requestText = await httpContentService.SerializeRequestWithoutBinaryDataAsync(jsonRequest, ["picture"]);

            Assert.That(requestText, Is.Not.Null);

            var httpRequest = await HttpContentService.DeserializeToRequestAsync(requestText);

            Assert.That(httpRequest.Content, Is.Not.Null);

            var json = await httpRequest.Content.ReadAsStringAsync();

            var newRequest = JsonConvertHelper.FromJson<TestRequest>(json);

            Assert.That(newRequest, Is.Not.Null);
            Assert.That(newRequest.Picture, Is.Not.Null);
            Assert.That(newRequest.Picture.StartsWith("image_"), Is.True);
        }

        [Test]
        public async Task ResponseTestAsync()
        {
            var testResponse = CreateTestResponse();
            var jsonResponse = CreateJsonResponse(JsonConvertHelper.ToJson(testResponse));

            var responseText = await httpContentService.SerializeResponseWithoutBinaryDataAsync(jsonResponse, ["picture"]);

            Assert.That(responseText, Is.Not.Null);

            var httpResponse = await HttpContentService.DeserializeToResponseAsync(responseText);

            Assert.That(httpResponse.Content, Is.Not.Null);

            var json = await httpResponse.Content.ReadAsStringAsync();

            var newResponse = JsonConvertHelper.FromJson<TestResponse>(json);

            Assert.That(newResponse, Is.Not.Null);
            Assert.That(newResponse.Status, Is.EqualTo((int)HttpStatusCode.OK));
            Assert.That(newResponse.Picture, Is.Not.Null);
            Assert.That(newResponse.Picture.StartsWith("image_"), Is.True);
        }

        [Test]
        public async Task MultipartFormDataRequestTestAsync()
        {
            var request = CreateMultipartFormDataRequest();

            Assert.That(request.Content, Is.Not.Null);
            Assert.That(request.Content.IsMimeMultipartContent(), Is.True);

            var newRequestText = await httpContentService.SerializeMultipartRequestWithoutBinaryDataAsync(request);

            Assert.IsNotNull(newRequestText);

            var newRequest = await HttpContentService.DeserializeToRequestAsync(newRequestText);

            Assert.That(newRequest.Content, Is.Not.Null);
            Assert.That(newRequest.Content.IsMimeMultipartContent(), Is.True);

            Assert.That(request.Content.Headers.ContentLength / 10, Is.GreaterThan(newRequest.Content.Headers.ContentLength));
        }

        private static byte[] GetBinaryContent(string imageFileName)
        {
            var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Images", imageFileName);
            return File.ReadAllBytes(path);
        }

        private static HttpRequestMessage CreateJsonRequest(string requestContent)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "http://api.example.com/api/test");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "123");
            request.Content = new StringContent(requestContent, Encoding.UTF8);
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return request;
        }

        private static HttpResponseMessage CreateJsonResponse(string responseContent)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseContent, Encoding.UTF8)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            return response;
        }

        private static ByteArrayContent CreateImageContent(string name, byte[] imageByteContent)
        {
            var imageContent = new ByteArrayContent(imageByteContent);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            imageContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = name,
                FileName = name
            };
            return imageContent;
        }

        private static HttpRequestMessage CreateMultipartFormDataRequest()
        {
            var dataContent = new MultipartFormDataContent();

            dataContent.Add(CreateImageContent("Selfie", GetBinaryContent("OrangeCat.jpg")));
            dataContent.Add(new StringContent("Ruddy"), "Name");
            dataContent.Add(new StringContent("12345"), "TransactionId");

            var request = new HttpRequestMessage(HttpMethod.Post, "http://api.example.com/api/test")
            {
                Content = dataContent
            };
            request.Content.Headers.Add("ContentType", "multipart/form-data; boundary=b4d49894");

            return request;
        }

        private static TestRequest CreateTestRequest()
        {
            var binContent = GetBinaryContent("OrangeCat.jpg");
            Assert.That(binContent, Is.Not.Null);

            var request = new TestRequest
            {
                TransactionId = 1111,
                Picture = Convert.ToBase64String(binContent)
            };
            return request;
        }

        private static TestResponse CreateTestResponse()
        {
            var binContent = GetBinaryContent("OrangeCat.jpg");
            Assert.That(binContent, Is.Not.Null);

            var response = new TestResponse
            {
                TransactionId = 1111,
                Status = (int)HttpStatusCode.OK,
                Picture = Convert.ToBase64String(binContent)
            };
            return response;
        }

        class TestRequest
        {
            public int TransactionId { get; set; }
            public string? Picture { get; set; }
        }

        class TestResponse
        {
            public int TransactionId { get; set; }
            public int Status { get; set; }
            public string? Picture { get; set; }
        }
    }
}