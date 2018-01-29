using System;
using System.Text;
using System.Diagnostics;
using Octokit;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;

namespace GitHubReleaser
{
     class Program
    {
        static void Main(string[] args)
        {
            Init();
            
            string apiKey = string.Empty;
            string repo = string.Empty;
            string username = string.Empty;
            string buildNumber = string.Empty;
            string projectName = string.Empty;
            string target = string.Empty;
            string buildDir = string.Empty;
            bool promote = true;


            foreach (string s in args)
            {
                string[] str = s.Split('=');
                switch (str[0])
                {
                    case "User":
                        username = str[1];
                        break;
                    case "Token":
                        apiKey = str[1];
                        break;
                    case "TokenFile":
                        apiKey = GrabTokenFromFile(str[1]);
                        break;
                    case "Repo":
                        repo = str[1];
                        break;
                    case "ProName":
                        projectName = str[1];
                        break;
                    case "Target":
                        target = str[1];
                        break;
                    case "BuildDir":
                        buildDir = str[1];
                        break;
                    case "Promote":
                        bool.TryParse(str[1], out promote);
                        break;

                    default:
                        Console.WriteLine("Unknown Argument: {0}", s);
                        break;
                }
            }

            if (projectName == string.Empty)
                projectName = repo;

            if (args.Length == 0)
                Console.Write(Help());
            else
            {
                Task task = Release(username, apiKey, repo, projectName, buildNumber, promote, target, buildDir);
                while (!task.IsCompleted)
                    ;
            }
        }

        static async Task Release(string username, string token, string repo, string projectName, string buildNumber, bool promote, string target, string buildDir)
        {
            GitHubClient client = new GitHubClient(new ProductHeaderValue("GitHubReleaser"));
            var basicAuth = new Credentials(token);
            client.Credentials = basicAuth;
            
            var releases = client.Repository.Release.GetAll(username, repo);

            try
            {
                var latestRelease = await client.Repository.Release.GetLatest(username, repo);
                if (string.IsNullOrWhiteSpace(buildNumber))
                    buildNumber = (int.Parse(latestRelease.TagName.Split('-')[1]) + 1).ToString();
            }
            catch (Exception)
            {
                buildNumber = "1";
            }

            Zip(projectName, buildNumber, target, buildDir);

            var newRelease = new NewRelease("Build-" + buildNumber);
            newRelease.Name = "Build " + buildNumber+" @ "+ DateTime.UtcNow.ToString("MM/dd/yyyy HH:mm:ss UTC");
            newRelease.Body = promote ? "This was automatically uploaded by [GitHubReleaser](https://github.com/BadCodr/GitHubReleaser) @BadCodr" : "";
            newRelease.Draft = false;
            newRelease.Prerelease = false;

            var result = await client.Repository.Release.Create(username, repo, newRelease);

            var archiveContents = File.OpenRead(Path.GetTempPath() + projectName + buildNumber + ".zip"); // TODO: better sample
            var assetUpload = new ReleaseAssetUpload()
            {
                FileName = projectName + buildNumber + ".zip",
                ContentType = "application/zip",
                RawData = archiveContents
            };
            
            var asset = await client.Repository.Release.UploadAsset(result, assetUpload);


            Clean(projectName,buildNumber);
            Console.WriteLine("Created release id {0}", releases.Id);
        }

        static string GrabTokenFromFile(string path)
        {
            return File.ReadAllText(path);
        }

        static void Zip(string projectName, string buildNumber, string targets, string buildDir)
        {

            string[] Files = targets.Split(',');
            if (Files[0].Length > 0)
            {
                var zip = ZipFile.Open(Path.GetTempPath() + projectName + buildNumber + ".zip", ZipArchiveMode.Create);
                foreach (var file in Files)
                {
                    zip.CreateEntryFromFile(buildDir+file, file, CompressionLevel.Optimal);
                }
                zip.Dispose();
            }

            else
                ZipFile.CreateFromDirectory(buildDir, Path.GetTempPath() + projectName + buildNumber + ".zip");
            
        }

        static void Clean(string projectName, string buildNumber)
        {
            File.Delete(Path.GetTempPath() + projectName + buildNumber + ".zip");
        }

        static void Init()
        {
            EmbeddedAssembly.Init();
        }

        static string Help()
        {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append("User      :   Username\n");
            stringBuilder.Append("Token     :   ApiKey From https://github.com/settings/tokens\n");
            stringBuilder.Append("TokenFile :   Grabs Token from file\n");
            stringBuilder.Append("Repo      :   Repository\n");
            stringBuilder.Append("ProName   :   Project name. if none is set it will default to the repo name\n");
            stringBuilder.Append("target    :   Files to target located in BuildDir. seperate with ','. if none is set it will default to all\n");
            stringBuilder.Append("BuildDir  :   build directory\n");
            stringBuilder.Append("Promote   :   Help the dev out. defaults to true\n");

            stringBuilder.Append("\nExample: " + Process.GetCurrentProcess().ProcessName + ".exe " + "User=BadCoder Token=abcdef12345 Repo=GitHubReleaser BuildDir=$(TargetDir) Promote=true\n");  

            return stringBuilder.ToString();
        }
    }
}
