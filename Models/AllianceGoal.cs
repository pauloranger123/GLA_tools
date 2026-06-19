using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GLAtools.Models
{
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

        public ObservableCollection<PlayerDonation> Donations { get; set; } = new();

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
