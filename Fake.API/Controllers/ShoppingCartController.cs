using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using Fake.API.Dtos;
using Fake.API.Models;
using Fake.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Fake.API.Controllers
{
    [Route("api/shoppingCart")]
    [ApiController]
    public class ShoppingCartController : ControllerBase
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ITouristRouteRepository _touristRouteRepository;
        private readonly IMapper _mapper;

        public ShoppingCartController(
            IHttpContextAccessor httpContextAccessor,
            ITouristRouteRepository touristRouteRepository,
            IMapper mapper
        )
        {
            _httpContextAccessor = httpContextAccessor;
            _touristRouteRepository = touristRouteRepository;
            _mapper = mapper;
        }

        [HttpGet]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> GetShoppingCart()
        {
            //1. 取得當前用戶
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

            //2. 使用userid獲得購物車
            var shoppingCart = await _touristRouteRepository.GetShoppingCartByUserId(userId);

            var shoppingCartDto = _mapper.Map<ShoppingCartDto>(shoppingCart);

            return Ok(shoppingCartDto);
        }

        [HttpPost("items")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> AddShoppingCartItem(
            [FromBody] AddShoppingCartItemDto addShoppingCartItemDto )
        {
            //1. 取得當前用戶
            var userId = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;

            //2. 使用userid獲得購物車
            var shoppingCart = await _touristRouteRepository.GetShoppingCartByUserId(userId);

            //3. 創建lineItem
            var touristRoute = await _touristRouteRepository
                .GetTouristRouteAsync(addShoppingCartItemDto.TouristRouteId);
            if (touristRoute == null) return NotFound("旅遊路線不存在!");

            var lineItem = new LineItem()
            {
                TouristRouteId = addShoppingCartItemDto.TouristRouteId,
                ShoppingCartId = shoppingCart.Id,
                OriginPrice = touristRoute.OriginPrice,
                DiscountPresent = touristRoute.DiscountPresent
            };

            // 4. 添加lineItem，並保存至DB
            await _touristRouteRepository.AddShoppingCartItem(lineItem);
            await _touristRouteRepository.SaveAsync();

            var shoppingCartDto = _mapper.Map<ShoppingCartDto>(shoppingCart);

            return Ok(shoppingCartDto);
        }

        [HttpDelete("items/{itemId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> DeleteShoppingCartItem([FromRoute] int itemId)
        {
            //1. 獲取lineItem數據
            var lineItem = await _touristRouteRepository.GetShoppingCartItemByItemId(itemId);
            if (lineItem == null) return NotFound("購物車商品找不到!");

            _touristRouteRepository.DeleteShoppingCartItem(lineItem);
            await _touristRouteRepository.SaveAsync();

            return NoContent();
        }

        [HttpPost("checkout")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        public async Task<IActionResult> Checkout()
        {
            //1. 獲得當前用戶
            var userId = _httpContextAccessor.HttpContext.User
                .FindFirst(ClaimTypes.NameIdentifier).Value;

            //2. 使用userId獲得購物車
            var shoppingCarrt = await _touristRouteRepository.GetShoppingCartByUserId(userId);

            // 3. 創建訂單
            var order = new Order()
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                State = OrderStateEnum.Pending,
                OrderItems = shoppingCarrt.ShoppingCartItems,
                CreateDateUTC = DateTime.UtcNow
            };

            //清空購物車
            shoppingCarrt.ShoppingCartItems = null;


            //4. 保存數據 
            await _touristRouteRepository.AddOrderAsync(order);
            await _touristRouteRepository.SaveAsync();

            //5. return
            var orderDto = _mapper.Map<OrderDto>(order);

            return Ok(orderDto);
        }
    }
}
