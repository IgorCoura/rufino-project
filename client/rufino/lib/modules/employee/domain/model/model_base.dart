import 'package:rufino/modules/employee/domain/model/enumeration.dart';
import 'package:rufino/modules/employee/domain/model/enumeration_collection.dart';
import 'package:rufino/modules/employee/domain/model/text_prop_base.dart';

abstract class ModelBase {
  final bool isLoading;

  const ModelBase({this.isLoading = false});

  List<TextPropBase> get props;
  List<ModelBase> get models;
  List<List<Enumeration>> get enumerations;
  List<EnumerationCollection> get enumerationCollection;
}
