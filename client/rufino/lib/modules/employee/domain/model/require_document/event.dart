import 'package:equatable/equatable.dart';

class Event extends Equatable {
  static const Map<String, String> conversionMapLanguage = {
    "createdevent": "Evento Criado",
    "namechangeevent": "Evento de Alteração de Nome",
    "rolechangeevent": "Evento de Alteração de Função",
    "workplacechangeevent": "Evento de Alteração de Local de Trabalho",
    "addresschangeevent": "Evento de Alteração de Endereço",
    "contactchangeevent": "Evento de Alteração de Contato",
    "medicaladmissionexamchangeevent":
        "Evento de Alteração de Exame Médico Admissional",
    "personalinfochangeevent": "Evento de Alteração de Informações Pessoais",
    "idcardchangeevent": "Evento de Alteração de Documento de Identidade",
    "voteidchangeevent": "Evento de Alteração de Título de Eleitor",
    "militardocumentchangeevent": "Evento de Alteração de Documento Militar",
    "completeadmissionevent": "Evento de Admissão Completa",
    "dependentchildchangeevent": "Evento de Alteração de Dependente Filho",
    "dependentspousechangeevent": "Evento de Alteração de Dependente Cônjuge",
    "dependentremovedevent": "Evento de Remoção de Dependente",
    "finishedcontractevent": "Evento de Contrato Finalizado",
    "demissionalexamrequestevent": "Evento de Solicitação de Exame Demissional",
    "documentsigningoptionschangeevent":
        "Evento de Alteração de Opções de Assinatura de Documentos",
    "januaryevent": "Evento Recorrente de Janeiro",
    "februaryevent": "Evento Recorrente de Fevereiro",
    "marchevent": "Evento Recorrente de Março",
    "aprilevent": "Evento Recorrente de Abril",
    "mayevent": "Evento Recorrente de Maio",
    "juneevent": "Evento Recorrente de Junho",
    "julyevent": "Evento Recorrente de Julho",
    "augustevent": "Evento Recorrente de Agosto",
    "septemberevent": "Evento Recorrente de Setembro",
    "octoberevent": "Evento Recorrente de Outubro",
    "novemberevent": "Evento Recorrente de Novembro",
    "decemberevent": "Evento Recorrente de Dezembro",
    "dailyevent": "Evento Recorrente Diário",
    "weeklyevent": "Evento Recorrente Semanal",
    "monthlyevent": "Evento Recorrente Mensal",
    "yearlyevent": "Evento Recorrente Anual",
  };

  static const Map<int, String> conversionMapIntToString = {
    0: "",
    1: "aaaaa",
  };

  final int id;
  final String name;

  const Event(this.id, this.name);
  const Event.empty({this.id = 0, this.name = ""});

  @override
  String toString() {
    return name;
  }

  @override
  List<Object?> get props => [id, name];

  static const defaultList = [Event(0, "Todos")];

  factory Event.fromJson(Map<String, dynamic> json) {
    return Event(json["id"], _convertName(json["id"], json["name"]));
  }

  factory Event.fromNumber(int id) {
    return Event(id, conversionMapIntToString[id] ?? id.toString());
  }

  static List<Event> fromListJson(List<dynamic> jsonList) {
    return jsonList.map<Event>((element) => Event.fromJson(element)).toList();
  }

  static Event getFromList(int id, List<Event>? listStatus) {
    if (listStatus == null) {
      return Event.fromNumber(id);
    }
    return listStatus.singleWhere((status) => status.id == id,
        orElse: () => const Event.empty());
  }

  static String _convertName(int id, String name) {
    var convertedName = conversionMapLanguage[name.toLowerCase()] ?? name;
    return convertedName.isEmpty ? id.toString() : convertedName;
  }
}
