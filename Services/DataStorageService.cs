using System.IO;
using System.Text.Json;
using GLAtools.Models;

namespace GLAtools.Services
{
    // Responsavel por ler/escrever o estado da alianca em um arquivo JSON
    // guardado na pasta AppData do usuario (local seguro e padrao do Windows
    // para esse tipo de dado, nao precisa de permissao especial).
    public class DataStorageService
    {
        private readonly string _filePath;

        public DataStorageService()
        {
            string folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GLAtools");

            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "alliance_data.json");
        }

        public AllianceData Load()
        {
            if (!File.Exists(_filePath))
                return new AllianceData();

            try
            {
                string json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<AllianceData>(json);
                if (data == null) return new AllianceData();

                // A referencia ParentGoal nao e salva no JSON (JsonIgnore),
                // entao precisa ser reconectada manualmente apos carregar.
                foreach (var goal in data.Goals)
                {
                    foreach (var donation in goal.Donations)
                    {
                        donation.ParentGoal = goal;
                    }
                }

                return data;
            }
            catch
            {
                // Se o arquivo estiver corrompido ou em formato antigo,
                // comeca do zero em vez de quebrar o app.
                return new AllianceData();
            }
        }

        public void Save(AllianceData data)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(data, options);
            File.WriteAllText(_filePath, json);
        }
    }
}
