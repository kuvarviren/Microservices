using Mango.Services.ShoppingCartAPI.Models.Dto;
using Newtonsoft.Json;

namespace Mango.Services.ShoppingCartAPI.Repository
{
    public class CouponRespository : ICouponRepository
    {
        private readonly HttpClient _httpClient;
        public CouponRespository(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<CouponDto> GetCoupon(string couponName)
        {
            var response = await _httpClient.GetAsync($"/api/coupons/{couponName}");
            var apiContent = await response.Content.ReadAsStringAsync();
            var resp = JsonConvert.DeserializeObject<ResponseDto>(apiContent);

            if(resp.IsSuccess)
            {
                return JsonConvert.DeserializeObject<CouponDto>(Convert.ToString(resp.Result));
            }

            return new CouponDto();
        }
    }
}
