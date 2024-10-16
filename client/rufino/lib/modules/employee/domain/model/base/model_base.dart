import 'package:equatable/equatable.dart';

abstract class ModelBase extends Equatable {
  final bool isLoading;
  final bool isLazyLoading;

  const ModelBase({this.isLoading = false, this.isLazyLoading = false});
}
