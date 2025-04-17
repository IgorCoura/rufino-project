class DocumentTemplateSimple {
  final String id;
  final String name;
  final String description;

  DocumentTemplateSimple(this.id, this.name, this.description);

  factory DocumentTemplateSimple.fromJson(Map<String, dynamic> json) {
    return DocumentTemplateSimple(
      json['id'] as String,
      json['name'] as String,
      json['description'] as String,
    );
  }

  static List<DocumentTemplateSimple> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => DocumentTemplateSimple.fromJson(el)).toList();
  }
}
