using System.Windows;
using System.Windows.Threading;
using Velopack;

namespace GLAtools
{
    public partial class App : Application
    {
        // O Velopack precisa "interceptar" a inicializacao do app antes de
        // qualquer coisa do WPF rodar -- por isso usamos um Main customizado
        // em vez do startup automatico padrao do WPF (que normalmente nao
        // tem Main visivel). VelopackApp.Build().Run() verifica se o app
        // esta sendo executado em um momento especial de instalacao/update
        // (ex: logo apos instalar, ou logo apos uma atualizacao ser aplicada)
        // e executa as acoes necessarias antes do resto do app iniciar.
        [STAThread]
        private static void Main(string[] args)
        {
            VelopackApp.Build().Run();

            App app = new();
            app.InitializeComponent();
            app.Run();
        }

        // Captura qualquer exceção não tratada que aconteça na thread da
        // interface (UI thread). Sem isso, um erro durante a inicialização
        // ou em qualquer binding faz o app fechar silenciosamente, sem
        // mostrar nada na tela — o que torna o problema quase impossível
        // de diagnosticar. Com isso, mostramos a mensagem real do erro.
        protected override void OnStartup(StartupEventArgs e)
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(
                $"Ocorreu um erro inesperado:\n\n{e.Exception.Message}\n\nDetalhes:\n{e.Exception}",
                "Erro na aplicação",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            e.Handled = true; // evita o crash imediato, mostrando a mensagem antes
        }
    }
}
