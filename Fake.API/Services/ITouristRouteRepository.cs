using Fake.API.Helper;
using Fake.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fake.API.Services
{
    public interface ITouristRouteRepository
    {
        Task<PaginationList<TouristRoute>> GetTouristRoutesAsync(string keyword, string operatorType, int? ratingValue, int pageSize, int pageNumber);
        Task<TouristRoute> GetTouristRouteAsync(Guid touristRouteId);
        Task<bool> TouristRouteExistsAsync(Guid touristRouteId);
        Task<IEnumerable<TouristRoutePicture>> GetPicturesByTouristRouteIdAsync(Guid touristRouteId);
        Task<TouristRoutePicture> GetPictureAsync(Guid touristRouteId,int pictureId);
        void AddTouristRoute(TouristRoute touristRouteModel);
        void AddTouristRoutePicture(Guid touristRouteId, TouristRoutePicture touristRoutePicture);
        void DeleteTouristRoute(TouristRoute touristRoute);
        void DeletTouristRoutePicture(TouristRoutePicture touristRoutePicture);
        Task<ShoppingCart> GetShoppingCartByUserId(string userId);
        Task CreateShoppingCart(ShoppingCart shoppingCart);
        Task AddShoppingCartItem(LineItem lineItem);
        Task<LineItem> GetShoppingCartItemByItemId(int itemId);
        void DeleteShoppingCartItem(LineItem lineItem);
        Task AddOrderAsync(Order order);
        Task<IEnumerable<Order>> GetOrdersByUserId(string userId);
        Task<Order> GetOrderById(Guid orderId);
        Task<bool> SaveAsync();
    }
}
