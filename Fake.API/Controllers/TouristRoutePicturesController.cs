using System;
using System.Collections.Generic;
using System.Linq;
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
    [Route("api/TouristRoutes/{touristRouteId}/pictures")]
    [ApiController]
    public class TouristRoutePicturesController : ControllerBase
    {
        private ITouristRouteRepository _touristRouteRepository;
        private readonly IMapper _mapper;

        public TouristRoutePicturesController(
            ITouristRouteRepository touristRouteRepository,
            IMapper mapper)
        {
            _touristRouteRepository = touristRouteRepository;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IActionResult> GetPictureListForTouristRouteAsync(Guid touristRouteId)
        {
            if (!await _touristRouteRepository.TouristRouteExistsAsync(touristRouteId))
            {
                return NotFound("旅遊路線不存在!");
            }

            var picturesFromReop = await _touristRouteRepository.GetPicturesByTouristRouteIdAsync(touristRouteId);
            if(picturesFromReop == null || picturesFromReop.Count() <= 0)
            {
                return NotFound("照片不存在!");
            }

            var touristRoouteDto = _mapper.Map<IEnumerable<TouristRoutePictureDto>>(picturesFromReop);

            return Ok(touristRoouteDto);
        }

        [HttpGet("{pictureId}", Name = "GetPictureAsync")]
        public async Task<IActionResult> GetPictureAsync(Guid touristRouteId, int pictureId)
        {
            if (! await _touristRouteRepository.TouristRouteExistsAsync(touristRouteId))
            {
                return NotFound("旅遊路線不存在!");
            }

            var picture = await _touristRouteRepository.GetPictureAsync(touristRouteId, pictureId);
            if(picture == null)
            {
                return NotFound("圖片不存在!");
            }

            var pictureDto = _mapper.Map<TouristRoutePictureDto>(picture);

            return Ok(pictureDto);
        }

        [HttpPost]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTouristRoutePictureAsync(
            [FromRoute] Guid touristRouteId,
            [FromBody] TouristRoutePictureForCreationDto touristRoutePictureForCreationDto)
        {
            if (! await _touristRouteRepository.TouristRouteExistsAsync(touristRouteId))
            {
                return NotFound("旅遊路線不存在!");
            }

            var touristRoutePictureModel = _mapper.Map<TouristRoutePicture>(touristRoutePictureForCreationDto);
            _touristRouteRepository.AddTouristRoutePicture(touristRouteId, touristRoutePictureModel);
            await _touristRouteRepository.SaveAsync();

            var touristRoutePictureDto = _mapper.Map<TouristRoutePictureDto>(touristRoutePictureModel);

            return CreatedAtRoute(
                    "GetPictureAsync",
                    new
                    {
                        touristRouteId = touristRoutePictureModel.TouristRouteId,
                        pictureId = touristRoutePictureModel.Id
                    },
                    touristRoutePictureDto
                );
        }

        [HttpDelete("{pictureId}")]
        [Authorize(AuthenticationSchemes = "Bearer")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePictureAsync(
            [FromRoute]Guid touristRouteId,
            [FromRoute] int pictureId)
        {
            if (! await _touristRouteRepository.TouristRouteExistsAsync(touristRouteId)) return NotFound("旅遊路線不存在!");

            var picture = await _touristRouteRepository.GetPictureAsync(touristRouteId, pictureId);

            _touristRouteRepository.DeletTouristRoutePicture(picture);
            await _touristRouteRepository.SaveAsync();

            return NoContent(); //204
        }
    }
}
