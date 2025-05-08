class RequireDocumentSimple {
  final String id;
  final String name;
  final String description;

  RequireDocumentSimple(this.id, this.name, this.description);

  factory RequireDocumentSimple.fromJson(Map<String, dynamic> json) {
    return RequireDocumentSimple(
      json['id'] as String,
      json['name'] as String,
      json['description'] as String,
    );
  }

  static List<RequireDocumentSimple> fromListJson(List<dynamic> listJson) {
    return listJson.map((el) => RequireDocumentSimple.fromJson(el)).toList();
  }
}
