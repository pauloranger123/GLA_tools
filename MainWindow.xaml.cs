using System.Windows;
using GLAtools.Models;
using GLAtools.Services;

namespace GLAtools
{
    public partial class MainWindow : Window
    {
        private readonly DataStorageService _storage = new();
        private readonly UpdateService _updateService = new();
        private AllianceData _data = new();

        public MainWindow()
        {
            InitializeComponent();
            LoadData();

            // Checagem de atualizacao roda em segundo plano (fire-and-forget,
            // com "_ =" para indicar isso explicitamente) e nao bloqueia a
            // abertura da janela. Se houver atualizacao, ela e baixada e
            // aplicada automaticamente, reiniciando o app.
            _ = CheckForUpdatesOnStartup();
        }

        private async Task CheckForUpdatesOnStartup()
        {
            string result = await _updateService.CheckAndApplyUpdatesAsync();
            System.Diagnostics.Debug.WriteLine($"[Update] {result}");
        }

        // Cria (ou recria) o Command de exclusao para uma meta especifica.
        // Precisa ser chamado para toda meta nova ou recem-carregada do JSON,
        // ja que ICommand nao e serializavel (perde a referencia ao recarregar).
        private void AttachDeleteCommand(AllianceGoal goal)
        {
            goal.DeleteCommand = new RelayCommand(_ => DeleteGoal(goal));

            // Salva automaticamente quando o lider bloquear/desbloquear a meta
            // (em vez de esperar a proxima acao que chamasse SaveData()).
            goal.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(AllianceGoal.IsLocked))
                    SaveData();
            };
        }

        private void DeleteGoal(AllianceGoal goal)
        {
            var result = MessageBox.Show(
                $"Tem certeza que deseja excluir a meta \"{goal.Title}\"? Essa ação não pode ser desfeita.",
                "Confirmar exclusão",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _data.Goals.Remove(goal);
                SaveData();
                RefreshEmptyState();
            }
        }

        private void LoadData()
        {
            _data = _storage.Load();

            // Cada meta carregada do disco precisa receber um DeleteCommand
            // novo, ja que comandos (delegates) nao sao salvos no JSON.
            foreach (var goal in _data.Goals)
            {
                AttachDeleteCommand(goal);
            }

            if (!_data.IsSetupComplete)
            {
                SetupPanel.Visibility = Visibility.Visible;
                MainContentPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                SetupPanel.Visibility = Visibility.Collapsed;
                MainContentPanel.Visibility = Visibility.Visible;
                GoalsItemsControl.ItemsSource = _data.Goals;
                RefreshEmptyState();
            }
        }

        private void SaveData()
        {
            _storage.Save(_data);
        }

        private void RefreshEmptyState()
        {
            EmptyStateText.Visibility = _data.Goals.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        // ============ TELA DE SETUP INICIAL ============
        private void CreateAlliance_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PlayerCountInput.Text.Trim(), out int count) || count <= 0)
            {
                MessageBox.Show("Digite um número válido de membros (maior que zero).",
                    "Valor inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (count > 200)
            {
                MessageBox.Show("Esse número parece muito alto. Confirme a quantidade de membros.",
                    "Confirme o valor", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            for (int i = 1; i <= count; i++)
            {
                _data.Players.Add(new Player { Name = $"Player{i}" });
            }

            _data.IsSetupComplete = true;
            SaveData();

            SetupPanel.Visibility = Visibility.Collapsed;
            MainContentPanel.Visibility = Visibility.Visible;
            GoalsItemsControl.ItemsSource = _data.Goals;
            RefreshEmptyState();
        }

        // ============ NOVA META ============
        private void NewGoal_Click(object sender, RoutedEventArgs e)
        {
            var window = new NewGoalWindow(_data.Players.ToList()) { Owner = this };
            bool? result = window.ShowDialog();

            if (result == true && window.CreatedGoal != null)
            {
                AttachDeleteCommand(window.CreatedGoal);
                _data.Goals.Add(window.CreatedGoal);
                SaveData();
                RefreshEmptyState();
            }
        }

        // ============ GERENCIAR MEMBROS ============
        private void ManageMembers_Click(object sender, RoutedEventArgs e)
        {
            var window = new ManageMembersWindow(_data.Players, _data.Goals) { Owner = this };
            window.ShowDialog();

            // Apos editar nomes, propaga para os cards de metas existentes,
            // ja que cada PlayerDonation guarda uma copia do nome.
            // (A remocao em cascata ja e feita dentro da propria ManageMembersWindow.)
            foreach (var goal in _data.Goals)
            {
                foreach (var donation in goal.Donations)
                {
                    var player = _data.Players.FirstOrDefault(p => p.Id == donation.PlayerId);
                    if (player != null)
                        donation.PlayerName = player.Name;
                }
            }

            SaveData();
            RefreshEmptyState();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            SaveData();
            base.OnClosing(e);
        }
    }
}
