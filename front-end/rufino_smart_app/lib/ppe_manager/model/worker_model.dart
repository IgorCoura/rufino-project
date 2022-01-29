class WorkerModel {
  final int id;
  final String name;
  final String role;
  final String cpf;
  final String registrationNumber;
  final String admissionDate;
  final DateTime? dueDate;
  final int ppesNotDelivered;

  WorkerModel(this.id, this.name, this.role, this.cpf, this.registrationNumber,
      this.admissionDate, this.dueDate, this.ppesNotDelivered);

  int getValidity() {
    if (dueDate == null) {
      return -1;
    } else {
      return dueDate!.day - DateTime.now().day;
    }
  }

  factory WorkerModel.fromJson(Map<dynamic, dynamic> json) => WorkerModel(
      json["id"],
      json["name"],
      json["role"],
      json["cpf"],
      json["registrationNumber"],
      json["admissionDate"],
      DateTime.tryParse(json['dueDate'] ?? ""),
      json["ppesNotDelivered"]);
}
