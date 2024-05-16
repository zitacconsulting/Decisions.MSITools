using DecisionsFramework.Design.Flow;
using System.Diagnostics;

namespace Zitac.MSITools;

[AutoRegisterMethodsOnClass(true, "Integration", "MSITools")]
public class MSITools
{

    public string GetMSIVersion(byte[] FileContent)
    {
        // Ensure msitools is installed
        EnsureMsitoolsInstalled();

        // Create a temporary file to write the byte array to.
        string tempFilePath = Path.GetTempFileName();
        File.WriteAllBytes(tempFilePath, FileContent);

        try
        {
            // Use msiextract to extract the version information
            var startInfo = new ProcessStartInfo
            {
                FileName = "msiinfo",
                Arguments = $"export {tempFilePath} Property",
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
                            return line.Split('\t')[1].Trim();
                        }
                    }
                }
            }

            throw new InvalidOperationException("ProductVersion not found in MSI file.");
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

