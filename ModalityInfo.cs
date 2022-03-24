using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DqeToImagingJson
{
    /*
     * {
    "countsPerMonthRaw": [
        {
            "date": "<YYYY/MM>",
            "imageCount": "<NUMBER>",
            "seriesCount": "<NUMBER>",
            "studyCount": "<NUMBER>"
        }
    ],
    "countsPerMonthStaging": [
        {
            "date": "<YYYY/MM>",
            "imageCount": "<NUMBER>",
            "seriesCount": "<NUMBER>",
            "studyCount": "<NUMBER>"
        }
    ],
    "countsPerMonthLive": [
        {
            "date": "<YYYY/MM>",
            "imageCount": "<NUMBER>",
            "seriesCount": "<NUMBER>",
            "studyCount": "<NUMBER>"
        }
    ],
    "promotionStatus": "<blocked|unavailable|processing|available>",
}

     * */

    internal class ModalityInfo
    {
        public string Modality { get; set; }
        public string Description { get; set; }

        public int TotalNoImages { get; set; }
        public int TotalNoSeries { get; set; }
        public int TotalNoStudies { get; set; }

        public DateTime? EvaluationDate { get; set; }

        public List<MonthCount> CountsPerMonth { get; set; }

    }
}
