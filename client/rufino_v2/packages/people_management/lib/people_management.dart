/// Gestão de pessoas: funcionários, documentos, cargos e locais de trabalho.
///
/// A casca compõe; este pacote não conhece a casca. O contrato entre os dois é
/// só o que está exportado aqui: as rotas, os tipos que a casca precisa
/// instanciar (api services, repositórios e suas interfaces), o domínio, e os
/// typedefs das capacidades de plataforma que ele recebe de fora.
///
/// **Tela e ViewModel não são exportados de propósito.** Quem navega para uma
/// tela deste produto usa uma rota de [PeopleManagementRoutes]; quem precisa de
/// um dado usa um repositório. Exportar a UI transformaria cada tela em API
/// pública e faria a casca voltar a conhecer o produto por dentro.
library;

// (as exportações entram junto com o código, fase a fase — ver
// doc/plano-migracao-people-management.md)
