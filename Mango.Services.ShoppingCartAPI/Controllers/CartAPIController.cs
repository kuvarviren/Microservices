using Mango.MessageBus;
using Mango.Services.ShoppingCartAPI.Messages;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Mango.Services.ShoppingCartAPI.Repository;
using Microsoft.AspNetCore.Mvc;

namespace Mango.Services.ShoppingCartAPI.Controllers
{
    [ApiController]
    [Route("api/carts")]
    public class CartAPIController : Controller
    {
        private readonly ICartRepository _cartRepository;
        private readonly IMessageBus _messageBus;
        private readonly ICouponRepository _couponRepository;
        protected ResponseDto _response;
        public CartAPIController(ICartRepository cartRepository, IMessageBus messageBus,
            ICouponRepository couponRepository)
        {
            _cartRepository = cartRepository;
            _messageBus = messageBus;
            _couponRepository = couponRepository;
            _response = new ResponseDto();
        }
        [HttpGet("GetCart/{userId}")]
        public async Task<object> GetCart(string userId)
        {
            try
            {
                CartDto cartDto = await _cartRepository.GetCartByUserId(userId);
                _response.Result = cartDto;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.Message };
            }
            return _response;
        }
        [HttpPost("AddCart")]
        public async Task<object> AddCart([FromBody] CartDto cartDto)
        {
            try
            {
                CartDto cartDt = await _cartRepository.CreateUpdateCart(cartDto);
                _response.Result = cartDt;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.Message };
            }
            return _response;
        }
        [HttpPost("UpdateCart")]
        public async Task<object> UpdateCart([FromBody] CartDto cartDto)
        {
            try
            {
                CartDto cartDt = await _cartRepository.CreateUpdateCart(cartDto);
                _response.Result = cartDt;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.Message };
            }
            return _response;
        }
        [HttpPost("RemoveCart")]
        public async Task<object> RemoveCart([FromBody] int cartId)
        {
            try
            {
                bool isSuccess = await _cartRepository.RemoveFromCart(cartId);
                _response.Result = isSuccess;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.Message };
            }
            return _response;
        }
        [HttpPost("ApplyCoupon")]
        public async Task<object> ApplyCoupon([FromBody] CartDto cartDto)
        {
            try
            {
                var result = await _cartRepository.ApplyCoupon(cartDto.CartHeader.UserId,
                     cartDto.CartHeader.CouponCode);
                _response.Result = result;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.Message };
            }
            return _response;
        }
        [HttpPost("RemoveCoupon")]
        public async Task<object> RemoveCoupon([FromBody] string userId)
        {
            try
            {
                var result = await _cartRepository.RemoveCoupon(userId);
                _response.Result = result;
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.Message };
            }
            return _response;
        }
        [HttpPost("Checkout")]
        public async Task<object> Checkout(CheckoutHeaderDto checkoutHeaderDto)
        {
            try
            {
                var cartDto = await _cartRepository.GetCartByUserId(checkoutHeaderDto.UserId);
                if (cartDto == null)
                {
                    return BadRequest();
                }
                var cartDetails = cartDto.CartDetails;
                //check if coupon is valid
                if(!string.IsNullOrEmpty(checkoutHeaderDto.CouponCode))
                {
                    CouponDto coupon = await _couponRepository.GetCoupon(checkoutHeaderDto.CouponCode);
                    if(checkoutHeaderDto.DiscountTotal != coupon.Discount)
                    {
                        _response.IsSuccess=false;
                        _response.ErrorMessages = new List<string>() { "Coupon price has changed, please confirm!" };
                        _response.DisplayMessage = "Coupon price has changed, please confirm!";
                        return _response;
                    }
                }
                checkoutHeaderDto.CartDetails = cartDetails;
                //logic to add message to process order
                await _messageBus.PublishMessage(checkoutHeaderDto, "checkoutmessagetopic");
            }
            catch (Exception ex)
            {
                _response.IsSuccess = false;
                _response.ErrorMessages = new List<string>() { ex.Message };
            }
            return _response;
        }
    }
}
