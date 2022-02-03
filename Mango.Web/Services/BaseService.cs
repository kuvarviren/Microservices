using System.Net.Http.Headers;
using System.Text;
using Mango.Web.Models.Dtos;
using Mango.Web.Models;
using Newtonsoft.Json;

namespace Mango.Web.Services
{
    public class BaseService : IBaseService
    {
        public IHttpClientFactory httpClient { get; set; }
        public ResponseDto responseModel { get; set; }

        public BaseService(IHttpClientFactory httpClient)
        {
            this.httpClient = httpClient;
            responseModel = new ResponseDto();
        }

        public async Task<T> SendAsync<T>(ApiRequest apiRequest)
        {
            try
            {
                var client = httpClient.CreateClient("MangoAPI");
                HttpRequestMessage requestMsg = new HttpRequestMessage();
                requestMsg.Headers.Add("Accept", "application/json");
                requestMsg.RequestUri = new Uri(apiRequest.Url);
                client.DefaultRequestHeaders.Clear();
                if (apiRequest.Data != null)
                {
                    requestMsg.Content = new StringContent(JsonConvert.SerializeObject(apiRequest.Data), Encoding.UTF8, "application/json");
                }

                if(!string.IsNullOrEmpty(apiRequest.AccessToken))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",apiRequest.AccessToken);
                }

                HttpResponseMessage responseMsg = null;
                switch (apiRequest.ApiType)
                {

                    case SD.ApiType.POST:
                        requestMsg.Method = HttpMethod.Post;
                        break;
                    case SD.ApiType.PUT:
                        requestMsg.Method = HttpMethod.Put;
                        break;
                    case SD.ApiType.DELETE:
                        requestMsg.Method = HttpMethod.Delete;
                        break;
                    default:
                        requestMsg.Method = HttpMethod.Get;
                        break;
                }
                responseMsg = await client.SendAsync(requestMsg);
                var apiContent = await responseMsg.Content.ReadAsStringAsync();
                var apiResponseDto = JsonConvert.DeserializeObject<T>(apiContent);
                return apiResponseDto;
            }
            catch (Exception ex)
            {
                var dto = new ResponseDto
                {
                    DisplayMessage = "Error",
                    ErrorMessages = new List<string> { ex.Message },
                    IsSuccess = false
                };
                var res = JsonConvert.SerializeObject(dto);
                var apiResponseDto = JsonConvert.DeserializeObject<T>(res);
                return apiResponseDto;
            }

        }
        public void Dispose()
        {
            GC.SuppressFinalize(true);
        }

    }
}
