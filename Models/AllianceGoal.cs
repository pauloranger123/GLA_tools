using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GLAtools.Models
{
    // Define como o valor da meta e interpretado: PerMember = o valor
    // digitado e o que CADA membro deve doar; TotalSplit = o valor digitado
    // e o TOTAL da alianca, dividido igualmente entre os membros.
    public enum GoalDivisionMode
    {
        PerMember,
        TotalSplit
    }

    // Representa um CARD de meta. Ex: "Meta da Semana 1", com prazo
    // ate uma certa data, pedindo X berries e/ou Y gemas de cada player.
    public class AllianceGoal : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();

        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public GoalDivisionMode DivisionMode { get; set; } = GoalDivisionMode.PerMember;

        // No modo TotalSplit, estes guardam o TOTAL pedido pela alianca
        // (valor de referencia fixo, usado para recalcular se a quantidade
        // de membros mudar). No modo PerMember, esses campos nao sao usados.
        public long BerriesTotalTarget { get; set; }
        public long GemsTotalTarget { get; set; }

        // Sobra da divisao por inteiro (ex: 100 / 3 = 33 cada, sobra 1).
        // Calculadas automaticamente sempre que a divisao e (re)feita.
        private long _berriesRemainder;
        public long BerriesRemainder
        {
            get => _berriesRemainder;
            set { _berriesRemainder = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasRemainder)); OnPropertyChanged(nameof(RemainderMessage)); }
        }

        private long _gemsRemainder;
        public long GemsRemainder
        {
            get => _gemsRemainder;
            set { _gemsRemainder = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasRemainder)); OnPropertyChanged(nameof(RemainderMessage)); }
        }

        public bool HasRemainder => BerriesRemainder > 0 || GemsRemainder > 0;

        // Monta a mensagem de aviso de sobra, mencionando so os recursos
        // que de fato tem sobra (evita mostrar "0 berries" quando a meta
        // so usa Gemas, por exemplo).
        public string RemainderMessage
        {
            get
            {
                var parts = new List<string>();
                if (BerriesRemainder > 0) parts.Add($"{Services.NumberFormatHelper.FormatThousands(BerriesRemainder)} berries");
                if (GemsRemainder > 0) parts.Add($"{GemsRemainder} gemas");

                if (parts.Count == 0) return string.Empty;

                string joined = string.Join(" e ", parts);
                return $"Divisão completa. Restou(aram) {joined} que não foi possível dividir igualmente entre todos.";
            }
        }

        // Fica true quando a divisao foi recalculada por uma mudanca na
        // quantidade de membros (adicao/remocao) DEPOIS da meta criada.
        // O card mostra um aviso visual enquanto isso for true; o lider
        // pode "reconhecer" o aviso (ver MainWindow) para limpa-lo.
        private bool _wasRecalculated;
        public bool WasRecalculated
        {
            get => _wasRecalculated;
            set { _wasRecalculated = value; OnPropertyChanged(); }
        }

        private long _berriesTarget;
        public long BerriesTarget
        {
            get => _berriesTarget;
            set { _berriesTarget = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasBerriesGoal)); }
        }

        private long _gemsTarget;
        public long GemsTarget
        {
            get => _gemsTarget;
            set { _gemsTarget = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasGemsGoal)); }
        }

        public bool HasBerriesGoal => BerriesTarget > 0;
        public bool HasGemsGoal => GemsTarget > 0;

        public DateTime Deadline { get; set; }

        // Quando true (padrao), os campos de doacao podem ser editados.
        // Quando false, os valores ficam protegidos contra edicao acidental
        // (os TextBox do card ficam desabilitados/somente leitura).
        private bool _isLocked = false;
        public bool IsLocked
        {
            get => _isLocked;
            set { _isLocked = value; OnPropertyChanged(); OnPropertyChanged(nameof(IsEditable)); }
        }

        // Inverso de IsLocked, conveniente para bindings de IsEnabled no XAML
        public bool IsEditable => !IsLocked;

        // Command que o botao "Excluir" do card chama. E injetado pelo
        // MainWindow no momento em que a meta e criada/carregada (nao e
        // serializado, ja que um delegate nao pode ir para o JSON).
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Windows.Input.ICommand? DeleteCommand { get; set; }

        // Command que o banner de aviso "recalculado" chama ao ser clicado,
        // para o lider dispensar o aviso (zera WasRecalculated). Tambem
        // injetado pelo MainWindow, pelo mesmo motivo do DeleteCommand acima.
        [System.Text.Json.Serialization.JsonIgnore]
        public System.Windows.Input.ICommand? DismissRecalculatedCommand { get; set; }

        public ObservableCollection<PlayerDonation> Donations { get; set; } = new();

        // Recalcula BerriesTarget/GemsTarget (valor por membro) a partir do
        // total e da quantidade ATUAL de membros na lista de doacoes.
        // So tem efeito no modo TotalSplit -- no modo PerMember nao faz nada,
        // ja que ali o valor por membro e digitado diretamente pelo lider.
        //
        // wasTriggeredByMemberChange: true quando chamado por uma mudanca na
        // lista de membros (adicao/remocao) DEPOIS da meta ja existir --
        // nesse caso, marca WasRecalculated para o card exibir o aviso.
        public void RecalculateSplit(bool wasTriggeredByMemberChange = false)
        {
            if (DivisionMode != GoalDivisionMode.TotalSplit) return;

            int memberCount = Donations.Count;
            if (memberCount == 0)
            {
                BerriesTarget = 0;
                GemsTarget = 0;
                BerriesRemainder = 0;
                GemsRemainder = 0;
                return;
            }

            BerriesTarget = BerriesTotalTarget / memberCount;
            BerriesRemainder = BerriesTotalTarget % memberCount;

            GemsTarget = GemsTotalTarget / memberCount;
            GemsRemainder = GemsTotalTarget % memberCount;

            // Propaga o novo valor por membro para cada linha de doacao
            // existente, ja que cada PlayerDonation guarda sua propria copia
            // (BerriesTarget/GemsTarget) usada para calcular HasMetGoal.
            foreach (var donation in Donations)
            {
                donation.BerriesTarget = BerriesTarget;
                donation.GemsTarget = GemsTarget;
            }

            if (wasTriggeredByMemberChange)
                WasRecalculated = true;

            RaiseAllChanged();
        }

        // Quantos players ja bateram a meta (berries E gemas, conforme aplicavel)
        public int CompletedCount => Donations.Count(d => d.HasMetGoal);
        public int TotalCount => Donations.Count;

        public bool IsExpired => DateTime.Now > Deadline;

        public int DaysRemaining
        {
            get
            {
                var diff = Deadline.Date - DateTime.Now.Date;
                return diff.Days < 0 ? 0 : diff.Days;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public void RaiseAllChanged()
        {
            // Usado depois de editar uma doacao, para recalcular contadores no card
            OnPropertyChanged(nameof(CompletedCount));
            OnPropertyChanged(nameof(TotalCount));
            OnPropertyChanged(nameof(IsExpired));
            OnPropertyChanged(nameof(DaysRemaining));
        }

        protected void OnPropertyChanged([CallerMemberName] string? propName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));
    }
}
