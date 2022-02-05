using AutoMapper;
using Mango.Services.ShoppingCartAPI.DbContexts;
using Mango.Services.ShoppingCartAPI.Models;
using Mango.Services.ShoppingCartAPI.Models.Dto;
using Microsoft.EntityFrameworkCore;

namespace Mango.Services.ShoppingCartAPI.Repository
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;

        public CartRepository(AppDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<bool> ApplyCoupon(string userId, string couponCode)
        {
            var cartFromDb = await _context.CartHeaders.FirstOrDefaultAsync(u => u.UserId == userId);
            cartFromDb.CouponCode = couponCode;
            _context.CartHeaders.Update(cartFromDb);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ClearCart(string userId)
        {
            var cartHeaderFromDb = await _context.CartHeaders.FirstOrDefaultAsync(x => x.UserId == userId);
            if (cartHeaderFromDb != null)
            {
                _context.CartDetails.RemoveRange(_context.CartDetails
                    .Where(u => u.CartHeaderId == cartHeaderFromDb.CartHeaderId));
                _context.CartHeaders.Remove(cartHeaderFromDb);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<CartDto> CreateUpdateCart(CartDto cartDto)
        {
            var cart = _mapper.Map<Cart>(cartDto);
            //check if product exist in Database, if not create it
            var prodInDb = await _context.Products.FirstOrDefaultAsync(m => m.Id == cart.CartDetails.FirstOrDefault().ProductId);
            if (prodInDb == null)
            {
                _context.Products.Add(cart.CartDetails.FirstOrDefault().Product);
                await _context.SaveChangesAsync();
            }
            //check if header is null
            var cartHeaderFromDb = await _context.CartHeaders.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == cart.CartHeader.UserId);
            if (cartHeaderFromDb == null)
            {
                //create header and details
                _context.CartHeaders.Add(cart.CartHeader);
                await _context.SaveChangesAsync();
                //populate cart Details with new cartHeader
                cart.CartDetails.FirstOrDefault().CartHeaderId = cart.CartHeader.CartHeaderId;
                //Add cart details in database
                cart.CartDetails.FirstOrDefault().Product = null;//we have alrady added the product
                _context.CartDetails.Add(cart.CartDetails.FirstOrDefault());
                await _context.SaveChangesAsync();
            }
            else
            {
                //if header is not null
                //check if details have same product
                var cartDetailsFromDb = await _context.CartDetails.AsNoTracking().FirstOrDefaultAsync(
                    u => u.ProductId == cart.CartDetails.FirstOrDefault().ProductId &&
                    u.CartHeader.CartHeaderId == cartHeaderFromDb.CartHeaderId);
                if (cartDetailsFromDb == null)
                {
                    //create details
                    //populate cart Details with new cartHeader
                    cart.CartDetails.FirstOrDefault().CartHeaderId = cartHeaderFromDb.CartHeaderId;
                    cart.CartDetails.FirstOrDefault().Product = null;//we have alrady added the product
                    _context.CartDetails.Add(cart.CartDetails.FirstOrDefault());
                    await _context.SaveChangesAsync();
                }
                else
                {
                    //update count / cart details
                    cart.CartDetails.FirstOrDefault().Count += cartDetailsFromDb.Count;
                    cart.CartDetails.FirstOrDefault().Product = null;//we have alrady added the product
                    cart.CartDetails.FirstOrDefault().CartHeaderId = cartHeaderFromDb.CartHeaderId;
                    cart.CartDetails.FirstOrDefault().CartDetailsId = cartDetailsFromDb.CartDetailsId;
                    _context.CartDetails.Update(cart.CartDetails.FirstOrDefault());
                    await _context.SaveChangesAsync();
                }
            }

            return _mapper.Map<CartDto>(cart);
        }

        public async Task<CartDto> GetCartByUserId(string userId)
        {
            Cart cart = new()
            {
                CartHeader = await _context.CartHeaders.FirstOrDefaultAsync(x => x.UserId == userId)
            };
           cart.CartDetails = _context.CartDetails.Where(u=>u.CartHeaderId==cart.CartHeader.CartHeaderId)
                .Include(p=>p.Product);
            return _mapper.Map<CartDto>(cart);
        }

        public async Task<bool> RemoveCoupon(string userId)
        {
            var cartFromDb = await _context.CartHeaders.FirstOrDefaultAsync(u => u.UserId == userId);
            cartFromDb.CouponCode = "";
            _context.CartHeaders.Update(cartFromDb);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RemoveFromCart(int cartDetailsId)
        {
            try
            {
                CartDetails cartDetails = await _context.CartDetails
                    .FirstOrDefaultAsync(u => u.CartDetailsId == cartDetailsId);

                int totalCountOfCartItems = _context.CartDetails
                    .Where(u => u.CartHeaderId == cartDetails.CartHeaderId).Count();

                _context.CartDetails.Remove(cartDetails);
                //If there is only one item remove the header
                if (totalCountOfCartItems == 1)
                {
                    var cartHeaderToRemove = await _context.CartHeaders
                        .FirstOrDefaultAsync(u => u.CartHeaderId == cartDetails.CartHeaderId);

                    _context.CartHeaders.Remove(cartHeaderToRemove);
                }
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
