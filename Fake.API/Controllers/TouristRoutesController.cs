using System;
using System.Collections.Generic;
using System.Dynamic;
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
using Microsoft.Net.Http.Headers;

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

        //application/json -> 旅遊路線資源
        //application/vnd.alice.hateoas+json -> 旅遊路線資源+自我發現連結
        //application/vnd.alice.touristRoute.simplify+json -> 輸出簡化版資源數據
        //application/vnd.alice.touristRoute.simplify.hateoas+json -> 輸出簡化版資源數據+自我發現連結
        [Produces(
            "application/json",
            "application/vnd.alice.hateoas+json",
            "application/vnd.alice.touristRoute.simplify+json",
            "application/vnd.alice.touristRoute.simplify.hateoas+json"
        )]
        [HttpGet(Name = nameof(GetTouristRoutesAsync))]
        [ResponseCache(
            Duration = 60, 
            VaryByQueryKeys = new string[] { "fields", "keyword" },
            VaryByHeader = "Accept")]
        public async Task<IActionResult> GetTouristRoutesAsync(
            [FromQuery] TouristRouteResourceParameters touristRoute,
            [FromQuery] PaginationResourceParameters pagination,
            [FromHeader(Name = "Accept")] string mediaType)
        {
            if(!MediaTypeHeaderValue.TryParse(mediaType, out MediaTypeHeaderValue parseMediaType))
            {
                return BadRequest();
            }

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

            bool isHateoas = parseMediaType.SubTypeWithoutSuffix
                .EndsWith("hateoas", StringComparison.InvariantCultureIgnoreCase);

            var primaryMediaType = isHateoas ? 
                    parseMediaType.SubTypeWithoutSuffix.Substring(0, parseMediaType.SubTypeWithoutSuffix.Length - 8)
                    : parseMediaType.SubTypeWithoutSuffix;

            //var touristRouteDto = _mapper.Map<IEnumerable<TouristRouteDto>>(touristRoutesFromRepo);
            //var shapeDtoList = touristRouteDto.ShapeData(touristRoute.Fields);

            IEnumerable<object> touristRouteDto;
            IEnumerable<ExpandoObject> shapeDtoList;

            if(primaryMediaType == "vnd.alice.touristRoute.simplify")
            {
                touristRouteDto = _mapper.Map<IEnumerable<TouristRouteSimplifyDto>>(touristRoutesFromRepo);
                shapeDtoList = ((IEnumerable<TouristRouteSimplifyDto>)touristRouteDto).ShapeData(touristRoute.Fields);
            }
            else
            {
                touristRouteDto = _mapper.Map<IEnumerable<TouristRouteDto>>(touristRoutesFromRepo);
                shapeDtoList = ((IEnumerable<TouristRouteDto>)touristRouteDto).ShapeData(touristRoute.Fields);
            }

            if (isHateoas)
            {
                var linkDto = CreateLinkForTouristRouteList(touristRoute, pagination);

                IDictionary<string, object> touristRouteDictionary = null;
                var shapeDtoWithLinkList = shapeDtoList.Select(
                    data => {
                        touristRouteDictionary = data as IDictionary<string, object>;
                        touristRouteDictionary.Add("links", CreateLinkForTouristRoute((Guid)touristRouteDictionary["Id"], touristRoute.Fields));

                        return touristRouteDictionary;
                    });

                var result = new { values = shapeDtoWithLinkList, links = linkDto };

                return Ok(result);
            }

            return Ok(shapeDtoList);
        }

        private IEnumerable<LinkDto> CreateLinkForTouristRouteList(
                TouristRouteResourceParameters touristRoute,
                PaginationResourceParameters pagination
            )
        {
            var links = new List<LinkDto>();
            //添加self自我連接
            links.Add(new LinkDto(
                    GenerateTouristRouteResourceURL(touristRoute, pagination, ResourceUriType.CurrentPage),
                    "self",
                    "GET"
                ));

            //添加創建旅遊路線
            links.Add(new LinkDto(
                    Url.Link(nameof(CreateTouristRouteAsync), null),
                    "create_tourist_route",
                    "POST"
                    ));

            return links;
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

            var linkDtos = CreateLinkForTouristRoute(touristRouteId, fields);

            //var result = touristRouteDto.ShapData(fields) as IDictionary<string, object>;
            //result.Add("links", linkDtos);

            dynamic result = touristRouteDto.ShapData(fields);
            result.links = linkDtos;

            return Ok(result);

        }

        private IEnumerable<LinkDto> CreateLinkForTouristRoute(Guid touristRouteId, string fields)
        {
            var links = new List<LinkDto>();

            //Read
            links.Add(new LinkDto
                (
                    Url.Link(nameof(GetTouristRoutesByIdAsync), new { touristRouteId, fields }),
                    "self",
                    "GET"
                ));
            //Update all
            links.Add(new LinkDto
                (
                    Url.Link(nameof(UpdateTouristRouteAsync), new { touristRouteId }),
                    "update",
                    "PUT"
                ));

            //partially update
            links.Add(new LinkDto
                (
                    Url.Link(nameof(PartiallyUpdateTouristRouteAsync), new { touristRouteId }),
                    "partially_update",
                    "PATCH"
                ));

            //Delete
            links.Add(new LinkDto
                (
                    Url.Link(nameof(DeleteTouristRouteAsync), new { touristRouteId }),
                    "delete",
                    "DELETE"
                ));

            //獲取路線圖片
            links.Add(new LinkDto
               (
                   Url.Link("GetPictureListForTouristRouteAsync", new { touristRouteId }),
                   "get_pictures",
                   "GET"
               ));

            //添加新圖片 
            links.Add(new LinkDto
               (
                   Url.Link("CreateTouristRoutePictureAsync", new { touristRouteId }),
                   "create_picture",
                   "POST"
               ));

            return links;
        }


        [HttpPost(Name = nameof(CreateTouristRouteAsync))]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTouristRouteAsync([FromBody] TouristRouteForCreationDto touristRouteForCreationDto)
        {
            var touristRouteModel = _mapper.Map<TouristRoute>(touristRouteForCreationDto);
            _touristRouteRepository.AddTouristRoute(touristRouteModel);
            await _touristRouteRepository.SaveAsync();

            var touristRouteDto = _mapper.Map<TouristRouteDto>(touristRouteModel);

            var links = CreateLinkForTouristRoute(touristRouteModel.Id, null);

            dynamic result = touristRouteDto.ShapData(null);
            result.links = links;

            //(7-3) HATOAS: 回傳 body:touristRouteDto, 同時也回傳 header location: GET 旅遊路線URL
            return CreatedAtRoute(
                    "GetTouristRoutesByIdAsync",
                    new { touristRouteId = result.Id},
                    result
                );
        }

        [HttpPut("{touristRouteId}", Name = nameof(UpdateTouristRouteAsync))]
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

        [HttpPatch("{touristRouteId}", Name = nameof(PartiallyUpdateTouristRouteAsync))]
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

        [HttpDelete("{touristRouteId}", Name = nameof(DeleteTouristRouteAsync))]
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
