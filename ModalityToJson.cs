using Dicom;
using DqeToImagingJson;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataQualityEngine.Data;
using Rdmp.Core.Repositories;
using Rdmp.Core.Startup;
using System.Text.RegularExpressions;

internal class ModalityToJson
{
    private Catalogue[] modalityCatalogues;
    private readonly Options options;
    private DQERepository dqe;
    private ModalityInfo modalityInfo;
    private Dictionary<string, MonthCount> monthCounts;
    private Dictionary<string, TagInfo> tagInfo;

    public ModalityToJson(string modality, Catalogue[] modalityCatalogues, LinkedRepositoryProvider repo, Options options)
    {
        this.modalityCatalogues = modalityCatalogues;
        this.options = options;
        this.dqe = new DQERepository(repo.CatalogueRepository);

        modalityInfo = new ModalityInfo();
        modalityInfo.Modality = modality;
        monthCounts = new Dictionary<string, MonthCount>();
        tagInfo = new Dictionary<string, TagInfo>();
    }

    internal ModalityInfo GetModalityInfo()
    {
        // if there is a study table
        var study = modalityCatalogues.FirstOrDefault(c => c.Name.Contains("StudyTable") && Regex.IsMatch(c.Name, options.OnlyPattern,RegexOptions.IgnoreCase));
        var series = modalityCatalogues.FirstOrDefault(c => c.Name.Contains("SeriesTable") && Regex.IsMatch(c.Name, options.OnlyPattern, RegexOptions.IgnoreCase));
        var image = modalityCatalogues.FirstOrDefault(c => c.Name.Contains("ImageTable") && Regex.IsMatch(c.Name, options.OnlyPattern, RegexOptions.IgnoreCase));

        if (study != null)
        {
            AddCounts("Study",study,c => modalityInfo.TotalNoStudies = c, (m,c)=>m.StudyCount = c);
        }

        if (series != null)
        {
            AddCounts("Series",series, c => modalityInfo.TotalNoSeries = c, (m, c) => m.SeriesCount = c);
        }

        if (image != null)
        {
            AddCounts("Image",image, c => modalityInfo.TotalNoImages = c, (m, c) => m.ImageCount = c);
        }

        modalityInfo.CountsPerMonth = monthCounts.Values.ToList();
        modalityInfo.Tags = tagInfo.Values.ToList();
        return modalityInfo;
    }

    private void AddCounts(string level, Catalogue catalogue, Action<int> totalCount, Action<MonthCount,int> monthCount)
    {
        var evaluation = dqe.GetMostRecentEvaluationFor(catalogue);
        
        if (evaluation == null)
            return;

        totalCount(evaluation.GetRecordCount() ?? 0);
                
        // TODO: set 'false' to true to discard outliers
        foreach (var dates in PeriodicityState.GetPeriodicityCountsForEvaluation(evaluation,false))
        {
            var date = dates.Key.ToString("yyyy-MM");

            if(!monthCounts.ContainsKey(date))
            {
                monthCounts.Add(date, new MonthCount { Date = date});
            }

            monthCount(monthCounts[date], dates.Value.Total);
        }

        foreach(var col in evaluation.ColumnStates)
        {
            if(col.PivotCategory == "ALL")
            {
                // we have already seen this tag in a higher level
                if (tagInfo.ContainsKey(col.TargetProperty))
                    continue;

                // TODO : Handle dicom leaf node columns
                var tag = DicomDictionary.Default.FirstOrDefault(t => t.Keyword == col.TargetProperty);

                // its not a dicom tag
                if (tag == null)
                    continue;

                var info = new TagInfo(col.TargetProperty);
                info.Level = level;

                // the total number of values seen in this column (including nulls)
                var total = col.CountCorrect + col.CountWrong + col.CountInvalidatesRow + col.CountMissing;

                // the proportion that were null
                info.Frequency = total == 0 ? 0: 1 - (float)col.CountDBNull / (float)total;

                tagInfo.Add(col.TargetProperty, info);
            }
        }

        // record the oldest DQE date of all evaluations
        if (modalityInfo.EvaluationDate == null || evaluation.DateOfEvaluation < modalityInfo.EvaluationDate)
            modalityInfo.EvaluationDate = evaluation.DateOfEvaluation;
        
    }
}