//using Fake.API.Models;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;

//namespace Fake.API.Services
//{
//    public class MockTouristRouteRepository : ITouristRouteRepository
//    {
//        private List<TouristRoute> _routes;
        
//        public MockTouristRouteRepository()
//        {
//            if(_routes == null)
//            {
//                InitializeTouristRoutes();
//            }
//        }

//        private void InitializeTouristRoutes()
//        {
//            _routes = new List<TouristRoute>() {
//                new TouristRoute{
//                    Id = Guid.NewGuid(),
//                    Title = "黃山",
//                    Description = "黃山真好玩",
//                    OriginPrice = 1299,
//                    Features = "<p>吃住行遊購娛</p>",
//                    Fees = "<p>交通費用自理</p>",
//                    Notes = "小心危險"
//                },
//                new TouristRoute{
//                     Id = Guid.NewGuid(),
//                    Title = "華山",
//                    Description = "華山真好玩",
//                    OriginPrice = 1299,
//                    Features = "<p>吃住行遊購娛</p>",
//                    Fees = "<p>交通費用自理</p>",
//                    Notes = "小心危險"
//                }
//            };
//        }

//        public TouristRoute GetTouristRoute(Guid touristRouteId)
//        {
//            return _routes.FirstOrDefault(item => item.Id == touristRouteId);
//        }

//        public IEnumerable<TouristRoute> GetTouristRoutes()
//        {
//            return _routes;
//        }

//        public bool TouristRouteExists(Guid touristRouteId)
//        {
//            throw new NotImplementedException();
//        }

//        public IEnumerable<TouristRoutePicture> GetPicturesByTouristRouteId(Guid touristRouteId)
//        {
//            throw new NotImplementedException();
//        }

//        public TouristRoutePicture GetPicture(int pictureId)
//        {
//            throw new NotImplementedException();
//        }
//    }
//}
