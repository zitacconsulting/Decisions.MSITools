using System.Diagnostics;
using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;


namespace Zitac.MSIToolsContainer;

[AutoRegisterStep("Get MSI Version Container", "Integration", "MSITools")]
[Writable]
public class GetMSIVersionContainer : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer //, INotifyPropertyChanged
{

    [WritableValue]
    private bool provideAsByteArray;

    [PropertyClassification(0, "Provide as Byte Array", new string[] { "Settings" })]
    public bool ProvideAsByteArray
    {
        get { return provideAsByteArray; }
        set
        {
            provideAsByteArray = value;
            this.OnPropertyChanged("InputData");
        }

    }

    public DataDescription[] InputData
    {
        get
        {

            List<DataDescription> dataDescriptionList = new List<DataDescription>();
            if (provideAsByteArray)
            {
                dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(ByteArray)), "Byte Array"));
            }
            else
            {
                dataDescriptionList.Add(new DataDescription((DecisionsType)new DecisionsNativeType(typeof(String)), "File Path"));
            }
            return dataDescriptionList.ToArray();
        }
    }

    public override OutcomeScenarioData[] OutcomeScenarios
    {
        get
        {
            List<OutcomeScenarioData> outcomeScenarioDataList = new List<OutcomeScenarioData>();

            outcomeScenarioDataList.Add(new OutcomeScenarioData("Done", new DataDescription(typeof(string), "MSI Version")));
            outcomeScenarioDataList.Add(new OutcomeScenarioData("Error", new DataDescription(typeof(string), "Error Message")));
            return outcomeScenarioDataList.ToArray();
        }
    }


    public ResultData Run(StepStartData data)
    {
        string FilePath;
        if (provideAsByteArray)
        {
            ByteArray FileContent = data.Data["Byte Array"] as ByteArray;
            // Create a temporary file to write the byte array to.
            FilePath = Path.GetTempFileName();
            File.WriteAllBytes(FilePath, FileContent.Content);
        }
        else
        {
            FilePath = data.Data["File Path"] as string;
        }

        try
        {
            // Ensure msitools is installed
            EnsureMsitoolsInstalled();


            // Use msiextract to extract the version information
            var startInfo = new ProcessStartInfo
            {
                FileName = "msiinfo",
                Arguments = $"export {FilePath} Property",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(startInfo))
            {
                process.WaitForExit();

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException($"msiinfo failed: {error}");
                }

                // Parse the output to get the ProductVersion
                using (StringReader reader = new StringReader(output))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.StartsWith("ProductVersion", StringComparison.OrdinalIgnoreCase))
                        {
                            Dictionary<string, object> dictionary = new Dictionary<string, object>();
                            dictionary.Add("MSI Version", (string)line.Split('\t')[1].Trim());

                            return new ResultData("Done", (IDictionary<string, object>)dictionary);
                        }
                    }
                }
            }

            string ExceptionMessage = "Could not find MSI version in file";
            return new ResultData("Error", (IDictionary<string, object>)new Dictionary<string, object>()
                {
                {
                    "Error Message",
                    (object) ExceptionMessage
                }
                });
        }
        finally
        {
            if (provideAsByteArray)
            {
                // Clean up the temporary file.
                if (File.Exists(FilePath))
                {
                    File.Delete(FilePath);
                }
            }
        }
    }
    static void EnsureMsitoolsInstalled()
    {
        try
        {
            var checkProcess = new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = "-c \"command -v msiinfo\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(checkProcess))
            {
                process.WaitForExit();
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (process.ExitCode != 0 || string.IsNullOrEmpty(output.Trim()))
                {
                    Console.WriteLine("msitools not found. Installing...");
                    var installProcess = new ProcessStartInfo
                    {
                        FileName = "bash",
                        Arguments = "-c \"apt-get update && apt-get install -y msitools\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var install = Process.Start(installProcess))
                    {
                        install.WaitForExit();
                        string installOutput = install.StandardOutput.ReadToEnd();
                        string installError = install.StandardError.ReadToEnd();

                        if (install.ExitCode != 0)
                        {
                            throw new InvalidOperationException($"Failed to install msitools: {installError}");
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to check/install msitools: {ex.Message}", ex);
        }
    }
}

