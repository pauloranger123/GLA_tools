using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GLAtools.Models
{
    // Representa quanto UM player especifico doou DENTRO de uma meta especifica.
    // Cada AllianceGoal tem uma lista dessas, uma para cada player da alianca.
    public class PlayerDonation : INotifyPropertyChanged
    {
        // Referencia ao card pai, usada apenas para notificar recalculo do
        // contador de progresso (CompletedCount) quando este valor muda.
        // Nao é serializada (ver [JsonIgnore]) para evitar referencia circular no JSON.
        [System.Text.Json.Serialization.JsonIgnore]
        public AllianceGoal? ParentGoal { get; set; }

        public string PlayerId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;

        private long _berriesDonated;
        public long BerriesDonated
        {
            get => _berriesDonated;
            set
            {
                _berriesDonated = value < 0 ? 0 : value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasMetBerries));
                OnPropertyChanged(nameof(BerriesDonatedText));
                ParentGoal?.RaiseAllChanged();
            }
        }

        // Versao em texto, formatada com ponto de milhar (ex: "100.000"),
        // usada no binding do TextBox no card. O getter formata; o setter
        // interpreta o que o usuario digitou (com ou sem pontos) e converte
        // de volta para o BerriesDonated (long), que e a fonte real da verdade.
        // [JsonIgnore]: nao salva essa versao no disco, ja que ela e so uma
        // "vitrine" de BerriesDonated -- salvar as duas seria redundante.
        [System.Text.Json.Serialization.JsonIgnore]
        public string BerriesDonatedText
        {
            get => Services.NumberFormatHelper.FormatThousands(BerriesDonated);
            set
            {
                if (Services.NumberFormatHelper.TryParse(value, out long parsed))
                    BerriesDonated = parsed;
                else if (string.IsNullOrWhiteSpace(value))
                    BerriesDonated = 0;
                // Se o texto for invalido (algo que nao seja numero), o valor
                // antigo de BerriesDonated e mantido -- o TextBox sera
                // "corrigido" de volta no proximo PropertyChanged.
            }
        }

        private long _gemsDonated;
        public long GemsDonated
        {
            get => _gemsDonated;
            set
            {
                _gemsDonated = value < 0 ? 0 : value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasMetGems));
                ParentGoal?.RaiseAllChanged();
            }
        }

        // Essas duas propriedades guardam a META da doacao (copiada do goal pai)
        // para que cada linha saiba calcular se bateu a meta ou nao, sem precisar
        // voltar a consultar o objeto pai a cada binding. Agora sao propriedades
        // completas (em vez de auto-propriedades) porque RecalculateSplit() no
        // AllianceGoal pode alterar esses valores em tempo real (modo TotalSplit
        // recalculando apos mudanca na quantidade de membros), e a UI precisa
        // ser notificada para refletir o novo valor (texto, "✓" de meta batida, etc).
        private long _berriesTarget;
        public long BerriesTarget
        {
            get => _berriesTarget;
            set
            {
                _berriesTarget = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasBerriesGoal));
                OnPropertyChanged(nameof(HasMetBerries));
                OnPropertyChanged(nameof(HasMetGoal));
            }
        }

        private long _gemsTarget;
        public long GemsTarget
        {
            get => _gemsTarget;
            set
            {
                _gemsTarget = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasGemsGoal));
                OnPropertyChanged(nameof(HasMetGems));
                OnPropertyChanged(nameof(HasMetGoal));
            }
        }

        // Usadas no XAML para mostrar/esconder a coluna de Berries ou Gemas
        // sem precisar de binding ascendente (RelativeSource AncestorType).
        public bool HasBerriesGoal => BerriesTarget > 0;
        public bool HasGemsGoal => GemsTarget > 0;

        public bool HasMetBerries => BerriesTarget <= 0 || BerriesDonated >= BerriesTarget;
        public bool HasMetGems => GemsTarget <= 0 || GemsDonated >= GemsTarget;
        public bool HasMetGoal => HasMetBerries && HasMetGems;

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
