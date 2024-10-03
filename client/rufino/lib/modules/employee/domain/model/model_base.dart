import 'package:rufino/modules/employee/domain/model/prop_base.dart';

abstract class ModelBase {
  final bool isLoading;

  const ModelBase({this.isLoading = false});

  List<PropBase> get props;
  List<ModelBase> get models;
}
