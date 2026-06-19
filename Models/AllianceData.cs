using System.Collections.ObjectModel;

namespace GLAtools.Models
{
    // Container principal: tudo que precisa ser salvo em disco vive aqui.
    public class AllianceData
    {
        public ObservableCollection<Player> Players { get; set; } = new();
        public ObservableCollection<AllianceGoal> Goals { get; set; } = new();

        // Indica se o lider ja passou pela tela inicial de "quantos players tem"
        public bool IsSetupComplete { get; set; } = false;
    }
}
