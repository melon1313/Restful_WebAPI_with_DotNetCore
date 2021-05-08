using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Fake.API.ResourceParameters
{
    public class TouristRouteResourceParameters
    {
        private string _rating;

        public string OperatorType { get; set; }
        public int? RatingValue { get; set; }
        public string Keyword { get; set; }

        public string Rating
        {
            get { return _rating; }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    Regex regex = new Regex(@"([A-Za-z0-9\-]+)(\d+)");
                    Match match = regex.Match(value);
                    if (match.Success)
                    {
                        OperatorType = match.Groups[1].Value;
                        RatingValue = Int32.Parse(match.Groups[2].Value);
                    }
                }

                _rating = value;
            }
        }
    }
}
