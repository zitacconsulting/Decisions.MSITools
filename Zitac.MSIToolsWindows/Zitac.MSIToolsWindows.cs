using DecisionsFramework.Design.Flow;
using WixToolset.Dtf.WindowsInstaller;

namespace Zitac.MSIToolsWindows;

[AutoRegisterMethodsOnClass(true, "Integration", "MSITools")]
public class MSIToolsWindows
{

    public string GetMSIVersionWindows(byte[] FileContent)
    {
                    // Create a temporary file to write the byte array to.
            string tempFilePath = Path.GetTempFileName();
            File.WriteAllBytes(tempFilePath, FileContent);

            try
            {
                using (var database = new Database(tempFilePath, DatabaseOpenMode.ReadOnly))
                {
                    string query = "SELECT `Value` FROM `Property` WHERE `Property`='ProductVersion'";
                    using (var view = database.OpenView(query))
                    {
                        view.Execute();
                        using (var record = view.Fetch())
                        {
                            return record.GetString("Value");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to get MSI file version. The error was: {ex.Message}", ex);
            }
            finally
            {
                // Clean up the temporary file.
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                }
            }
    }
}