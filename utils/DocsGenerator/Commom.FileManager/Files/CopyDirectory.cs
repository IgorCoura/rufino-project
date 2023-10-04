using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Commom.FileManager.Files
{
    public  class CopyDirectory
    {
        public static Task Copy(string sourceDir, string destinyDir)
        {
            if (!Directory.Exists(sourceDir))
            {
                throw new DirectoryNotFoundException($"The directory {sourceDir}, not found.");
            }
            List<Task> listTasks = CopyFolder(sourceDir, destinyDir);
            Task.WaitAll(listTasks.ToArray());
            return Task.CompletedTask;
        }

        static List<Task> CopyFiles(string sourceDir, string destinyDir)
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

        static List<Task> CopyFolder(string sourceFolder, string destFolder)
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
    }
}
