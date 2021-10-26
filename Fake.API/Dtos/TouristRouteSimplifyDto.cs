using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fake.API.Dtos
{
    public class TouristRouteSimplifyDto
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
    }
}
