using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using GLAtools.Models;

namespace GLAtools
{
    public partial class ManageMembersWindow : Window
    {
        private readonly ObservableCollection<Player> _players;
        private readonly ObservableCollection<AllianceGoal> _goals;

        // Recebe tambem a lista de metas (Goals) para poder remover o
        // membro de todas as metas existentes quando ele for excluido --
        // sem essa referencia, a janela so conseguiria editar a lista de
        // players, deixando "fantasmas" (linhas de doacao orfas) nos cards.
        public ManageMembersWindow(ObservableCollection<Player> players, ObservableCollection<AllianceGoal> goals)
        {
            InitializeComponent();
            _players = players;
            _goals = goals;
            PlayersItemsControl.ItemsSource = _players;
        }

        private void AddMember_Click(object sender, RoutedEventArgs e)
        {
            int nextNumber = _players.Count + 1;
            var newPlayer = new Player { Name = $"Player{nextNumber}" };
            _players.Add(newPlayer);

            // Adicionar um membro tambem afeta metas no modo TotalSplit (o
            // total passa a ser dividido por mais gente). Cada meta existente
            // precisa ganhar uma nova linha de doacao para esse player, e
            // entao ser recalculada.
            foreach (var goal in _goals)
            {
                goal.Donations.Add(new PlayerDonation
                {
                    PlayerId = newPlayer.Id,
                    PlayerName = newPlayer.Name,
                    BerriesTarget = goal.BerriesTarget,
                    GemsTarget = goal.GemsTarget,
                    ParentGoal = goal
                });

                if (goal.DivisionMode == GoalDivisionMode.TotalSplit)
                {
                    goal.RecalculateSplit(wasTriggeredByMemberChange: true);
                }
                else
                {
                    goal.RaiseAllChanged();
                }
            }
        }

        private void DeleteMember_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Player player)
            {
                var result = MessageBox.Show(
                    $"Tem certeza que deseja remover \"{player.Name}\" da aliança?\n\nEle também será removido de todas as metas já criadas (incluindo o progresso de doação registrado nelas).",
                    "Confirmar remoção",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    _players.Remove(player);

                    // Remocao em cascata: tira esse player de TODAS as metas
                    // ja criadas, nao so da lista geral de membros.
                    foreach (var goal in _goals)
                    {
                        var donation = goal.Donations.FirstOrDefault(d => d.PlayerId == player.Id);
                        if (donation != null)
                        {
                            goal.Donations.Remove(donation);

                            if (goal.DivisionMode == GoalDivisionMode.TotalSplit)
                            {
                                // Redivide o MESMO total entre os membros que restaram,
                                // e marca a meta com aviso de recalculo (WasRecalculated).
                                goal.RecalculateSplit(wasTriggeredByMemberChange: true);
                            }
                            else
                            {
                                goal.RaiseAllChanged(); // so atualiza contadores (CompletedCount/TotalCount)
                            }
                        }
                    }
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
