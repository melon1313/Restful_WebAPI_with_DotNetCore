using Fake.API.Database;
using Fake.API.Helper;
using Fake.API.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fake.API.Services
{
    public class TouristRouteRepository : ITouristRouteRepository
    {
        private readonly AppDbContext _context;

        public TouristRouteRepository(AppDbContext appDbContext)
        {
            _context = appDbContext;
        }

        public async Task<TouristRoute> GetTouristRouteAsync(Guid touristRouteId)
        {
            return await _context.TouristRoutes.Include(item => item.TouristRoutePictures).FirstOrDefaultAsync(item => item.Id == touristRouteId);
        }

        public async Task<IEnumerable<TouristRoutePicture>> GetPicturesByTouristRouteIdAsync(Guid touristRouteId)
        {
            return await _context.TouristRoutePictures.Where(item => item.TouristRouteId == touristRouteId).ToListAsync();
        }

        public async Task<PaginationList<TouristRoute>> GetTouristRoutesAsync(string keyword, string operatorType, int? ratingValue, int pageSize, int pageNumber, string orderBy)
        {
            IQueryable<TouristRoute> result = _context.TouristRoutes.Include(item => item.TouristRoutePictures);

            if (!string.IsNullOrEmpty(keyword))
            {
                keyword = keyword.Trim();

                //TODO:在進行分頁同時查詢關鍵字時，會有bug，流程應改成取得關鍵字後的集合再進行分頁。注意Linq的延遲執行。
                result = result.Where(item => item.Title.Contains(keyword));
            }

            if (ratingValue >= 0)
            {
                result = operatorType.ToLower() switch
                {
                    "largerthan" => result.Where(item => item.Rating > ratingValue),
                    "lessthan" => result.Where(item => item.Rating < ratingValue),
                    _ => result.Where(item => item.Rating == ratingValue)
                };
            }

            if (!string.IsNullOrEmpty(orderBy))
            {
                if (orderBy.ToLowerInvariant() == "originalprice")
                {
                    result = result.OrderBy(t => t.OriginPrice);
                }
            }

            return await PaginationList<TouristRoute>.CreateAsync(pageNumber, pageSize, result);
        }

        public async Task<bool> TouristRouteExistsAsync(Guid touristRouteId)
        {
            return await _context.TouristRoutes.AnyAsync(item => item.Id == touristRouteId);
        }

        public async Task<TouristRoutePicture> GetPictureAsync(Guid touristRouteId,int pictureId)
        {
            return await _context.TouristRoutePictures.Where(item => item.TouristRouteId == touristRouteId && item.Id == pictureId).FirstOrDefaultAsync();
        }

        public void AddTouristRoute(TouristRoute touristRoute)
        {
            if (touristRoute == null) throw new ArgumentNullException(nameof(touristRoute));
            _context.TouristRoutes.Add(touristRoute);
        }

        public void AddTouristRoutePicture(Guid touristRouteId, TouristRoutePicture touristRoutePicture)
        {
            if (touristRouteId == Guid.Empty) throw new ArgumentNullException(nameof(touristRouteId));
            if (touristRoutePicture == null) throw new ArgumentNullException(nameof(touristRoutePicture));

            touristRoutePicture.TouristRouteId = touristRouteId;  
            _context.TouristRoutePictures.Add(touristRoutePicture);
        }

        public async Task<bool> SaveAsync()
        {
            return await _context.SaveChangesAsync() >= 0;
        }

        public void DeleteTouristRoute(TouristRoute touristRoute)
        {
            _context.TouristRoutes.Remove(touristRoute);
        }

        public void DeletTouristRoutePicture(TouristRoutePicture touristRoutePicture)
        {
            _context.TouristRoutePictures.Remove(touristRoutePicture);
        }

        public async Task<IEnumerable<TouristRoute>> GetTouristRouteByIdListAsync(IEnumerable<Guid> touristRoutesId)
        {
            return await _context.TouristRoutes.Where(item => touristRoutesId.Contains(item.Id)).ToListAsync();
        }

        public async Task<ShoppingCart> GetShoppingCartByUserIdAsync(string userId)
        {
            return await _context.ShoppingCarts
                .Include(soppingCart => soppingCart.User)
                .Include(soppingCart => soppingCart.ShoppingCartItems)
                .ThenInclude(lineItem => lineItem.TouristRoute)
                .Where(shoppingCart => shoppingCart.UserId == userId)
                .FirstOrDefaultAsync();
        }

        public async Task CreateShoppingCartAsync(ShoppingCart shoppingCart)
        {
            await _context.ShoppingCarts.AddAsync(shoppingCart);
        }

        public async Task AddShoppingCartItemAsync(LineItem lineItem)
        {
            await _context.LineItems.AddAsync(lineItem);
        }

        public async Task<LineItem> GetShoppingCartItemByItemIdAsync(int itemId)
        {
            return await _context.LineItems
                .Where(lineItem => lineItem.Id == itemId)
                .FirstOrDefaultAsync();
        }

        public void DeleteShoppingCartItem(LineItem lineItem)
        {
            _context.LineItems.Remove(lineItem);
        }

        public async Task AddOrderAsync(Order order)
        {
            await _context.Orders.AddAsync(order);
        }

        public async Task<PaginationList<Order>> GetOrdersByUserIdAsync(string userId, int pageSize, int pageNumber)
        {
            IQueryable<Order> result = _context.Orders
                .Where(item => item.UserId == userId);

            return await PaginationList<Order>.CreateAsync(pageNumber, pageSize, result);  
        }

        public async Task<Order> GetOrderByIdAsync(Guid orderId)
        {
            return await _context.Orders
                .Include(item => item.OrderItems)
                .ThenInclude(item => item.TouristRoute)
                .Where(item => item.Id == orderId)
                .FirstOrDefaultAsync();
        }
    }
}
 