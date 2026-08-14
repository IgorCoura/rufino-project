/// Reexporta a família que agora vive em `rufino_core`.
///
/// Ela subiu junto com o cliente UMA: `SessionExpiredException` e
/// `AccessDeniedException` são o vocabulário que o `checkApiStatus` dos BCs
/// novos precisa falar para que o listener de sessão do app continue
/// reconhecendo 401 e 403 vindos de qualquer produto.
///
/// Código novo deve importar `package:rufino_core/rufino_core.dart`.
library;
export 'package:rufino_core/rufino_core.dart'
    show
        AccessDeniedException,
        AuthException,
        InvalidCredentialsException,
        NetworkAuthException,
        NoCredentialsException,
        SessionExpiredException;
