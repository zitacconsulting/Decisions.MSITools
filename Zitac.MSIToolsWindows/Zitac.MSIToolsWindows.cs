using DecisionsFramework.Design.Flow;
using DecisionsFramework.Design.Flow.CoreSteps;
using DecisionsFramework.Design.Flow.Mapping;
using DecisionsFramework.Design.Properties;
using DecisionsFramework.Design.ConfigurationStorage.Attributes;
using DecisionsFramework.Design.Flow.Service.Debugging.DebugData;

using WixToolset.Dtf.WindowsInstaller;

namespace Zitac.MSIToolsWindows;

[AutoRegisterStep("Get MSI Version Windows", "Integration", "MSITools")]
[Writable]
public class GetMSIVersionWindows : BaseFlowAwareStep, ISyncStep, IDataConsumer, IDataProducer //, INotifyPropertyChanged
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
            using (var database = new Database(FilePath, DatabaseOpenMode.ReadOnly))
            {
                string query = "SELECT `Value` FROM `Property` WHERE `Property`='ProductVersion'";
                using (var view = database.OpenView(query))
                {
                    view.Execute();
                    using (var record = view.Fetch())
                    {
                        Dictionary<string, object> dictionary = new Dictionary<string, object>();
                            dictionary.Add("MSI Version", (string)record.GetString("Value"));

                        return new ResultData("Done", (IDictionary<string, object>)dictionary);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            string ExceptionMessage = ex.ToString();
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
            // Clean up the temporary file.
            if (File.Exists(FilePath))
            {
                File.Delete(FilePath);
            }
        }
    }
}