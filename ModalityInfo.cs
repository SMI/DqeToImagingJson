namespace DqeToImagingJson
{
    internal class ModalityInfo
    {
        public string Modality { get; set; }

        public int TotalNoImages { get; set; }
        public int TotalNoSeries { get; set; }
        public int TotalNoStudies { get; set; }

        public DateTime? EvaluationDate { get; set; }

        public List<MonthCount> CountsPerMonth { get; set; }

    }
}
