# DqeToImagingJson

Connects to an RDMP database defined in a Databases.yaml file and reformats the DQE results for imaging tables to JSON.

```
USAGE:
Normal usage:
  DqeToImagingJson
Run only on Catalogues with 'edris' in the name (case insensitive):
  DqeToImagingJson --only edris

  --logstartup         Output the RDMP startup messages (for debugging connection issues)

  --only               (Default: Table$) Regular expression for matching Catalogue names to process.  Defaults to
                       'Table$'

  --modalitypattern    (Default: ^([A-Z][A-Z])_) Regular expression for extracting Modality from a Catalogue name.  Must
                       have a capture group.  Defaults to '^([A-Z][A-Z])_'

  --help               Display this help screen.

  --version            Display version information.

  value pos. 0         (Default: Databases.yaml) Path to a yaml file that stores the connection strings to the RDMP
                       platform databases.  Defaults to 'Databases.yaml'
```
