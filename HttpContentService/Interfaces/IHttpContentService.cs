namespace HttpContentLib.Interfaces
{
    public interface IHttpContentService
    {
        Task<string> SerializeRequestWithoutBinaryDataAsync(HttpRequestMessage request, string[] imageFielsList);
        Task<string> SerializeResponseWithoutBinaryDataAsync(HttpResponseMessage response, string[] imageFielsList);
        Task<string> SerializeMultipartRequestWithoutBinaryDataAsync(HttpRequestMessage request);
    }
}
