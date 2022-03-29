using Rdmp.Core.CommandLine.Options;
using Rdmp.Core.Curation.Data;
using Rdmp.Core.Startup;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.Json;
using DqeToImagingJson;
using ReusableLibraryCode.Checks;
using CommandLine;
using CommandLine.Text;

public class Program
{
    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
               .WithParsed<Options>(o =>
               {

                   LinkedRepositoryProvider repo;

                   string? databasesYaml = o.DatabasesYaml;

                   if (string.IsNullOrWhiteSpace(databasesYaml))
                   {
                       string assemblyFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                       databasesYaml = Path.Combine(assemblyFolder, "Databases.yaml");
                   }

                   if (File.Exists(databasesYaml))
                   {
                       var cstrs = ConnectionStringsYamlFile.LoadFrom(new FileInfo(databasesYaml));
                       repo = new LinkedRepositoryProvider(cstrs.CatalogueConnectionString, cstrs.DataExportConnectionString);
                   }
                   else
                   {
                       throw new FileNotFoundException($"Could not find file {databasesYaml}");
                   }


                   var startup = new Startup(new EnvironmentInfo(), repo);
                   ICheckNotifier checkNotifier = o.LogStartup ? new ThrowImmediatelyCheckNotifier() { WriteToConsole = true } : new ToMemoryCheckNotifier();

                   try
                   {
                       startup.DoStartup(checkNotifier);
                   }
                   catch (Exception ex)
                   {
                       Console.WriteLine(ex.ToString());
                   }


               // Figure out modalities in the RDMP metadata database by name
               var imagingCatalogues = repo.CatalogueRepository.GetAllObjects<Catalogue>()
                       .Where(c => GetCatalogueModalityIfAny(c, o) != null);

                   var modalityGroups = imagingCatalogues.GroupBy(c => GetCatalogueModalityIfAny(c, o));

                   List<ModalityInfo> modalityInfos = new();

               // for each modality
               foreach (var modality in modalityGroups)
                   {
                   // generate statistics
                   var modalityToJson = new ModalityToJson(modality.Key, modality.ToArray(), repo, o);
                       modalityInfos.Add(modalityToJson.GetModalityInfo());
                   }

               // output the final json
               Console.WriteLine(JsonSerializer.Serialize(modalityInfos, new JsonSerializerOptions { WriteIndented = true }));



               });
    }

    static string? GetCatalogueModalityIfAny(Catalogue c, Options o)
    {
        // find Catalogues called CT_ImageTable etc
        Regex modalityExtractor = new Regex(o.ModalityPattern);
        var match = modalityExtractor.Match(c.Name);

        if (match.Success)
        {
            if (match.Groups.Count < 2)
                throw new Exception("ModalityPattern must contain a regex capture group");

            var modality = match.Groups[1].Value;

            if (string.IsNullOrEmpty(modality))
                throw new Exception($"ModalityPattern returned no capture group for Catalogue named '{c.Name}'");
            
            return modality;
        }
            

        return null;
    }

}

class Options
{
    const string DefaultOnlyPattern = "Table$";
    const string DefaultModalityPattern = "^([A-Z][A-Z])_";

    [Option("logstartup", Required = false, HelpText = "Output the RDMP startup messages (for debugging connection issues)")]
    public bool LogStartup { get; set; }

    [Option("only", Default = DefaultOnlyPattern, HelpText = $"Regular expression for matching Catalogue names to process.  Defaults to '{DefaultOnlyPattern}'")]
    public string OnlyPattern { get; set; } = DefaultOnlyPattern;

    [Option("modalitypattern", Default = DefaultModalityPattern, HelpText = $"Regular expression for extracting Modality from a Catalogue name.  Must have a capture group.  Defaults to '{DefaultModalityPattern}'")]
    public string ModalityPattern { get; set; } = DefaultModalityPattern;

    [Value(0, Required = false, HelpText = "Path to a yaml file that stores the connection strings to the RDMP platform databases.  Defaults to 'Databases.yaml'",Default = "Databases.yaml")]
    public string? DatabasesYaml { get; set; }

    [Usage]
    public static IEnumerable<Example> Examples
    {
        get
        {
            yield return new Example("Normal usage", new Options { DatabasesYaml = null, ModalityPattern = null, OnlyPattern = null});

            yield return new Example("Run only on Catalogues with 'edris' in the name (case insensitive)", new Options { DatabasesYaml = null, ModalityPattern = null, OnlyPattern = "edris" });
        }
    }
}
