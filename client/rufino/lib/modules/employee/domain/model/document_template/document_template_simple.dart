class DocumentTemplateSimple {
  final String id;
  final String name;
  final String description;
  final bool usePreviousPeriod;

  DocumentTemplateSimple(this.id, this.name, this.description,
      {this.usePreviousPeriod = false});
  DocumentTemplateSimple.empty(
      {this.id = "",
      this.name = "",
      this.description = "",
      this.usePreviousPeriod = false});

  factory DocumentTemplateSimple.fromJson(Map<String, dynamic> json) {
    return DocumentTemplateSimple(
      json['id'] as String,
      json['name'] as String,
      json['description'] as String,
      usePreviousPeriod: json['usePreviousPeriod'] as bool? ?? false,
    );
  }

  static List<DocumentTemplateSimple> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => DocumentTemplateSimple.fromJson(el)).toList();
  }
}
