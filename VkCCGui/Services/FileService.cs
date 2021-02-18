using System;
using System.IO;

namespace VkCCGui.Services
{
    public class FileService
    {
        public string[] ReadLines(string path)
        {
            if (!File.Exists(path))
                throw new InvalidOperationException("Файл не найден.");
            
            return File.ReadAllLines(path);
        }

        public void SaveFile(string fileName, string[] lines)
        {
            File.WriteAllLines(fileName, lines);
        }
    }
}