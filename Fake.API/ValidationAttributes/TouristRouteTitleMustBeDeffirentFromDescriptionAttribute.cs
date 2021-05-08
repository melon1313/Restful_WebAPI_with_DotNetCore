using Fake.API.Dtos;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace Fake.API.ValidationAttributes
{
    public class TouristRouteTitleMustBeDeffirentFromDescriptionAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(
            object value, 
            ValidationContext validationContext)
        {
            var touristRoouteDto = (TouristRouteForManipulationDto)validationContext.ObjectInstance;
            if(touristRoouteDto.Title == touristRoouteDto.Description)
            {
                return new ValidationResult(
                    "路線名稱必須與路線描述不同!"  ,
                    new[] { "TouristRouteForCreationDto" }
                );
            }

            return ValidationResult.Success;
        }
    }
}
