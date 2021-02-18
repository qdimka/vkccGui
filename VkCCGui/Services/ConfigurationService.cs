using System.IO;
using System.Text.Json;

namespace VkCCGui.Services
{
    public class Configuration
    {
        public string Token { get; set; }

        public string From { get; set; }

        public string To { get; set; }

        public int Delay { get; set; }
    }
    
    public class ConfigurationService
    {
        private readonly string _path;

        public ConfigurationService(string path)
        {
            _path = path;
        }

        public Configuration? Get()
        {
            if (!File.Exists(_path))
                return null;
            
            var fileContent = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<Configuration>(fileContent);
        }

        public void Save(Configuration configuration)
        {
            var content = JsonSerializer.Serialize(configuration);
            File.WriteAllText(_path, content);
        }
    }
}