using DqeToImagingJson;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.DataQualityEngine.Data;
using Rdmp.Core.Repositories;
using Rdmp.Core.Startup;

internal class ModalityToJson
{
    private string modality;
    private Catalogue[] modalityCatalogues;
    private LinkedRepositoryProvider repo;
    private DQERepository dqe;
    private ModalityInfo modalityInfo;
    private Dictionary<string, MonthCount> monthCounts;

    public ModalityToJson(string modality, Catalogue[] modalityCatalogues, LinkedRepositoryProvider repo)
    {
        this.modality = modality;
        this.modalityCatalogues = modalityCatalogues;
        this.repo = repo;

        this.dqe = new DQERepository(repo.CatalogueRepository);

        modalityInfo = new ModalityInfo();
        modalityInfo.Modality = modality;
        monthCounts = new Dictionary<string, MonthCount>();

    }

    internal ModalityInfo GetModalityInfo()
    {
        // if there is a study table
        var study = modalityCatalogues.FirstOrDefault(c => c.Name.EndsWith("StudyTable"));
        var series = modalityCatalogues.FirstOrDefault(c => c.Name.EndsWith("SeriesTable"));
        var image = modalityCatalogues.FirstOrDefault(c => c.Name.EndsWith("ImageTable"));

        if (study != null)
        {
            AddCounts(study,c => modalityInfo.TotalNoStudies = c, (m,c)=>m.StudyCount = c);
        }

        if (series != null)
        {
            AddCounts(series, c => modalityInfo.TotalNoSeries = c, (m, c) => m.SeriesCount = c);
        }

        if (image != null)
        {
            AddCounts(image, c => modalityInfo.TotalNoImages = c, (m, c) => m.ImageCount = c);
        }

        modalityInfo.CountsPerMonth = monthCounts.Values.ToList();

        return modalityInfo;
    }

    private void AddCounts(Catalogue catalogue, Action<int> totalCount, Action<MonthCount,int> monthCount)
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

        // record the oldest DQE date of all evaluations
        if (modalityInfo.EvaluationDate == null || evaluation.DateOfEvaluation < modalityInfo.EvaluationDate)
            modalityInfo.EvaluationDate = evaluation.DateOfEvaluation;
        
    }
}