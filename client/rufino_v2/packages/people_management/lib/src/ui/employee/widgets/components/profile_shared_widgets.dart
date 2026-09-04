import 'package:rufino_core/rufino_core.dart';

/// `SectionCard` subiu para `rufino_core`: a moldura do bloco "ler, então
/// editar no lugar" é a mesma no perfil do funcionário e no cadastro do
/// tenant, e nenhuma das duas é dona dela.
export 'package:rufino_core/rufino_core.dart' show InfoRow, SectionCard;

/// Nome anterior de [InfoRow], preservado para não trocar os pontos de uso do
/// perfil do funcionário.
typedef ContactInfoRow = InfoRow;
