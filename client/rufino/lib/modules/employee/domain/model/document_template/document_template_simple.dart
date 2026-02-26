class DocumentTemplateSimple {
  final String id;
  final String name;
  final String description;
  final bool usePreviousPeriod;
  final bool acceptsSignature;

  DocumentTemplateSimple(this.id, this.name, this.description,
      {this.usePreviousPeriod = false, this.acceptsSignature = true});
  DocumentTemplateSimple.empty(
      {this.id = "",
      this.name = "",
      this.description = "",
      this.usePreviousPeriod = false,
      this.acceptsSignature = true});

  factory DocumentTemplateSimple.fromJson(Map<String, dynamic> json) {
    return DocumentTemplateSimple(
      json['id'] as String,
      json['name'] as String,
      json['description'] as String,
      usePreviousPeriod: json['usePreviousPeriod'] as bool? ?? false,
      acceptsSignature: json['acceptsSignature'] as bool? ?? true,
    );
  }

  static List<DocumentTemplateSimple> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => DocumentTemplateSimple.fromJson(el)).toList();
  }
}
