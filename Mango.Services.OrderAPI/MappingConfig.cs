using AutoMapper;
using Mango.Services.OrderAPI.Messages;
using Mango.Services.OrderAPI.Models.Dtos;

namespace Mango.Services.OrderAPI
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mapperConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<CheckoutHeaderDto, OrderHeaderDto>().ReverseMap();
                config.CreateMap<CheckoutHeaderDto, OrderDetailsDto>().ReverseMap();
            });
            return mapperConfig;
        }
    }
}
