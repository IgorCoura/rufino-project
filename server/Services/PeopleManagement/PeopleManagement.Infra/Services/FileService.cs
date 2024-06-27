namespace PeopleManagement.Infra.Services
{
    public static class FileService
    {
        public static Task<string> CreateTemporatyDiretory()
        {
            var temporaryPath = Path.Combine("temp", Guid.NewGuid().ToString());

            if (Directory.Exists(temporaryPath) is false)
            {
                Directory.CreateDirectory(temporaryPath);
            }

            return Task.FromResult(temporaryPath);
        }

        public static Task CopyDiretory(string sourceDir, string destinyDir)
        {
            if (!Directory.Exists(sourceDir))
            {
                throw new DirectoryNotFoundException($"The directory {sourceDir}, not found.");
            }
            List<Task> listTasks = CopyFolder(sourceDir, destinyDir);
            Task.WaitAll(listTasks.ToArray());
            return Task.CompletedTask;
        }

        public static List<Task> CopyFolder(string sourceFolder, string destFolder)
        {
            if (!Directory.Exists(destFolder))
            {
                Directory.CreateDirectory(destFolder);
            }

            var tasks = new List<Task>();

            tasks.AddRange(CopyFiles(sourceFolder, destFolder));

            foreach (string folder in Directory.GetDirectories(sourceFolder))
            {
                tasks.Add(Task.Run(() => {
                    string folderName = Path.GetFileName(folder);
                    var newDestFolder = Path.Combine(destFolder, folderName);
                    CopyFolder(folder, newDestFolder);
                }));
            }

            return tasks;
        }

        public static List<Task> CopyFiles(string sourceDir, string destinyDir)
        {
            var tasks = new List<Task>();

            foreach (string file in Directory.GetFiles(sourceDir))
            {
                tasks.Add(Task.Run(() =>
                {
                    string fileName = Path.GetFileName(file);
                    string destFile = Path.Combine(destinyDir, fileName);
                    File.Copy(file, destFile, true);
                }));
            }

            return tasks;
        }
    }
}
