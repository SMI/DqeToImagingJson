using Rdmp.Core.CommandLine.Options;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Startup;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.Json;
using DqeToImagingJson;


# region Where is the Catalogue Database?

LinkedRepositoryProvider repo;

if(args.Length == 0)
{
    string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
    var yaml = Path.Combine(assemblyFolder, "Databases.yaml");

    if (File.Exists(yaml))
    {
        args = new string[] { yaml };
    }
}

if (args.Length == 1)
{
    if (args[0].EndsWith(".yaml"))
    {
        var cstrs = ConnectionStringsYamlFile.LoadFrom(new FileInfo(args[0]));
        repo = new LinkedRepositoryProvider(cstrs.CatalogueConnectionString,cstrs.DataExportConnectionString);
    }
    else
    {
        repo = new LinkedRepositoryProvider(args[0], null);
    }
}
else
{
    Console.WriteLine("Expected 1 argument which is the RDMP Catalogue connection string or path to a Databases.yaml file");
    return;
}

#endregion


// Figure out modalities in the RDMP metadata database by name
var imagingCatalogues = repo.CatalogueRepository.GetAllObjects<Catalogue>()
    .Where(c=>GetCatalogueModalityIfAny(c) != null);

var modalityGroups = imagingCatalogues.GroupBy(GetCatalogueModalityIfAny);

List<ModalityInfo> modalityInfos = new ();

// for each modality
foreach (var modality in modalityGroups)
{
    // generate statistics
    var modalityToJson = new ModalityToJson(modality.Key, modality.ToArray(), repo);
    modalityInfos.Add(modalityToJson.GetModalityInfo());
}

// output the final json
Console.WriteLine(JsonSerializer.Serialize(modalityInfos,new JsonSerializerOptions { WriteIndented = true}));

#region Helper Methods
string? GetCatalogueModalityIfAny(Catalogue c)
{
    // find Catalogues called CT_ImageTable etc (must have only letters and a single underscore)
    Regex modalityExtractor = new Regex("^([A-Z]+)_[A-Za-z]+$");

    if (modalityExtractor.IsMatch(c.Name))
        return modalityExtractor.Match(c.Name).Groups[1].Value;

    return null;
}
#endregion