using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using Fake.API.Dtos;
using Fake.API.Helper;
using Fake.API.Models;
using Fake.API.ResourceParameters;
using Fake.API.Services;
using Fake.API.ValidationAttributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Fake.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TouristRoutesController : ControllerBase
    {
        private ITouristRouteRepository _touristRouteRepository;
        private readonly IMapper _mapper;
        private readonly IUrlHelper _urlHelper;

        public TouristRoutesController(
            ITouristRouteRepository touristRouteRepository,
            IMapper mapper,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor)
        {
            _touristRouteRepository = touristRouteRepository;
            _mapper = mapper;
            _urlHelper = urlHelperFactory.GetUrlHelper(actionContextAccessor.ActionContext);
        }

        private string GenerateTouristRouteResourceURL(
            TouristRouteResourceParameters touristRoute,
            PaginationResourceParameters pagination,
            ResourceUriType resourceUriType)
        {
            return resourceUriType switch
            {
                ResourceUriType.PreviousPage => _urlHelper.Link(nameof(GetTouristRoutesAsync),
                    new
                    {
                        fileds = touristRoute.Fields,
                        keyword = touristRoute.Keyword,
                        rating = touristRoute.Rating,
                        pageNumber = pagination.PageNumber -1,
                        pageSize = pagination.PageSize
                    }
                ),
                ResourceUriType.NextPage => _urlHelper.Link(nameof(GetTouristRoutesAsync),
                   new
                   {
                       fileds = touristRoute.Fields,
                       keyword = touristRoute.Keyword,
                       rating = touristRoute.Rating,
                       pageNumber = pagination.PageNumber + 1,
                       pageSize = pagination.PageSize
                   }
               ),
               _ => _urlHelper.Link(nameof(GetTouristRoutesAsync),
                   new
                   {
                       fileds = touristRoute.Fields,
                       keyword = touristRoute.Keyword,
                       rating = touristRoute.Rating,
                       pageNumber = pagination.PageNumber,
                       pageSize = pagination.PageSize
                   }
               ),
            };
        }

        [HttpGet(Name = nameof(GetTouristRoutesAsync))]
        public async Task<IActionResult> GetTouristRoutesAsync(
            [FromQuery] TouristRouteResourceParameters touristRoute,
            [FromQuery] PaginationResourceParameters pagination)
        {
            var touristRoutesFromRepo = await _touristRouteRepository
                .GetTouristRoutesAsync(
                touristRoute.Keyword, 
                touristRoute.OperatorType, 
                touristRoute.RatingValue,
                pagination.PageSize,
                pagination.PageNumber,
                touristRoute.OrderBy);
            if (touristRoutesFromRepo == null || touristRoutesFromRepo.Count() == 0)
            {
                return NotFound("沒有旅遊路線");
            }

            var touristRouteDto = _mapper.Map<IEnumerable<TouristRouteDto>>(touristRoutesFromRepo);

            var previousPageLink = touristRoutesFromRepo.HasPrevious ? GenerateTouristRouteResourceURL(touristRoute, pagination, ResourceUriType.PreviousPage) : null;
            var nextPageLink = touristRoutesFromRepo.HasNext ? GenerateTouristRouteResourceURL(touristRoute, pagination, ResourceUriType.NextPage) : null;

            //x-pagination
            var paginationMetadata = new
            {
                previousPageLink,
                nextPageLink,
                totalCount = touristRoutesFromRepo.TotalCount,
                pageSize = touristRoutesFromRepo.PageSize,
                currentPage = touristRoutesFromRepo.CurrentPage,
                totalPages = touristRoutesFromRepo.TotalPages
            };

            Response.Headers.Add("x-pagination",
                Newtonsoft.Json.JsonConvert.SerializeObject(paginationMetadata));

            return Ok(touristRouteDto.ShapeData(touristRoute.Fields));
        }

        [HttpGet("{touristRouteId}", Name = "GetTouristRoutesByIdAsync")] //Name: route Name
        public async Task<IActionResult> GetTouristRoutesByIdAsync([FromRoute]Guid touristRouteId, [FromQuery] string fields)
        {
            var touristRouteFromRepo = await _touristRouteRepository.GetTouristRouteAsync(touristRouteId);
            if (touristRouteFromRepo == null)
            {
                return NotFound($@"旅遊路線{touristRouteId}找不到!");
            }

            if (!fields.IsPropertiesExists<TouristRouteDto>()) { return BadRequest("請輸入正確的塑形參數!"); }

            //var touristRouteDto = new TouristRouteDto()
            //{
            //    Id = touristRouteFromRepo.Id,
            //    Price = touristRouteFromRepo.OriginPrice * (decimal)(touristRouteFromRepo.DiscountPresent ?? 1)
            //};

            var touristRouteDto = _mapper.Map<TouristRouteDto>(touristRouteFromRepo);
            return Ok(touristRouteDto.ShapData(fields));
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTouristRouteAsync([FromBody] TouristRouteForCreationDto touristRouteForCreationDto)
        {
            var touristRouteModel = _mapper.Map<TouristRoute>(touristRouteForCreationDto);
            _touristRouteRepository.AddTouristRoute(touristRouteModel);
            await _touristRouteRepository.SaveAsync();

            var touristRouteDto = _mapper.Map<TouristRouteDto>(touristRouteModel);

            //(7-3) HATOAS: 回傳 body:touristRouteDto, 同時也回傳 header location: GET 旅遊路線URL
            return CreatedAtRoute(
                    "GetTouristRoutesByIdAsync",
                    new { touristRouteId = touristRouteDto.Id },
                    touristRouteDto
                );
        }

        [HttpPut("{touristRouteId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateTouristRouteAsync(
            [FromRoute]Guid touristRouteId,
            [FromBody] TouristRouteForUpdateDto touristRouteForUpdateDto
            )
        {
            if (!await _touristRouteRepository.TouristRouteExistsAsync(touristRouteId)) return NotFound("旅遊路線找不到!");

            var touristRouteFromRepo = await _touristRouteRepository.GetTouristRouteAsync(touristRouteId);
            //1.映射dto
            //2.更新dto
            //3.映射model
            _mapper.Map(touristRouteForUpdateDto, touristRouteFromRepo);

            await _touristRouteRepository.SaveAsync();

            return NoContent();
        }

        [HttpPatch("{touristRouteId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> PartiallyUpdateTouristRouteAsync(
            [FromRoute]Guid touristRouteId,
            [FromBody]JsonPatchDocument<TouristRouteForUpdateDto> patchDocument)
        {
            if (! await _touristRouteRepository.TouristRouteExistsAsync(touristRouteId)) return NotFound("旅遊路線找不到!");

            var touristRouteFromRepo = await _touristRouteRepository.GetTouristRouteAsync(touristRouteId);
            var touristRouteToPatch = _mapper.Map<TouristRouteForUpdateDto>(touristRouteFromRepo);

            patchDocument.ApplyTo(touristRouteToPatch, ModelState);

            //進行數據驗證
            if (!TryValidateModel(touristRouteToPatch))
            {
                return ValidationProblem(ModelState);
            }
            _mapper.Map(touristRouteToPatch, touristRouteFromRepo);
            await _touristRouteRepository.SaveAsync();

            return NoContent();
        } 

        [HttpDelete("{touristRouteId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTouristRouteAsync([FromRoute] Guid touristRouteId)
        {
            if (! await _touristRouteRepository.TouristRouteExistsAsync(touristRouteId)) return NotFound("旅遊路線找不到!");

             var touristRoute =await _touristRouteRepository.GetTouristRouteAsync(touristRouteId);
            _touristRouteRepository.DeleteTouristRoute(touristRoute);
            await _touristRouteRepository.SaveAsync();

            return NoContent(); //204
        }
    }
}
