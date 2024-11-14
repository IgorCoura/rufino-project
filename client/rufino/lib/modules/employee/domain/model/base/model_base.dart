import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

abstract class ModelBase extends Equatable {
  final bool isLoading;
  final bool isLazyLoading;

  const ModelBase({this.isLoading = false, this.isLazyLoading = false});

  final List<TextPropBase> textProps = const [];
}
