using Velopack;
using Velopack.Sources;
using Velopack.Exceptions;

namespace GLAtools.Services
{
    // Centraliza a logica de checar/baixar/aplicar atualizacoes via Velopack,
    // usando o GitHub Releases do projeto como fonte.
    //
    // IMPORTANTE: troque a URL abaixo pela URL real do seu repositorio
    // no GitHub antes de gerar um release de verdade.
    public class UpdateService
    {
        // Repositorio publico no GitHub onde os releases (gerados pelo
        // workflow do GitHub Actions) serao publicados.
        private const string GithubRepoUrl = "https://github.com/pauloranger123/GLA_tools";

        private readonly UpdateManager _manager;

        public UpdateService()
        {
            _manager = new UpdateManager(new GithubSource(GithubRepoUrl, null, false));
        }

        // Verifica se ha uma atualizacao disponivel, baixa e aplica
        // (reiniciando o app) automaticamente se houver. Retorna uma
        // mensagem de status (para log ou exibicao, se desejado).
        //
        // Chamadas a este metodo SO funcionam quando o app foi instalado
        // de verdade via o instalador do Velopack -- rodando direto do
        // bin/Debug (durante desenvolvimento) sempre lanca NotInstalledException,
        // que tratamos aqui silenciosamente (nao e um erro real nesse caso).
        public async Task<string> CheckAndApplyUpdatesAsync()
        {
            try
            {
                var newVersion = await _manager.CheckForUpdatesAsync();
                if (newVersion == null)
                    return "Nenhuma atualização disponível. Você já está na versão mais recente.";

                await _manager.DownloadUpdatesAsync(newVersion);

                // Aplica a atualizacao e reinicia o app automaticamente.
                // Isso encerra o processo atual -- nenhum codigo depois
                // desta linha sera executado.
                _manager.ApplyUpdatesAndRestart(newVersion);

                return "Atualização aplicada com sucesso.";
            }
            catch (NotInstalledException)
            {
                // Esperado quando rodando via "dotnet run"/bin/Debug (nao
                // instalado pelo Velopack). Nao e um erro real nesse contexto.
                return "Checagem de atualização ignorada (app não está instalado via Velopack).";
            }
            catch (Exception ex)
            {
                // Falha de rede, GitHub fora do ar, etc. Nao deve travar o
                // app -- so reportamos e seguimos normalmente.
                return $"Não foi possível checar atualizações: {ex.Message}";
            }
        }
    }
}
