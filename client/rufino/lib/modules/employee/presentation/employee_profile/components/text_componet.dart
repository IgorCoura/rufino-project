import 'package:flutter/material.dart';
import 'package:rufino/modules/employee/domain/model/base/text_base.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/base_edit_component.dart';

class TextComponet extends BaseEditComponent {
  final TextBase textBase;
  const TextComponet(
      {required this.textBase, super.key, super.isEditing, super.isLoading});

  @override
  TextComponet copyWith(
      {Function(Object value)? onSaveChanges,
      bool? isEditing,
      bool? isLoading}) {
    return TextComponet(
        textBase: textBase,
        isEditing: isEditing ?? this.isEditing,
        isLoading: isLoading ?? this.isLoading);
  }

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        Padding(
          padding: const EdgeInsets.only(top: 8),
          child: Container(
              width: double.infinity,
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                border: Border.all(
                    color: isEditing
                        ? Theme.of(context).colorScheme.primary
                        : Theme.of(context).disabledColor),
                borderRadius: BorderRadius.circular(5),
              ),
              child: Text(textBase.value)),
        ),
        _labelText(context)
      ],
    );
  }

  Widget _labelText(BuildContext context) {
    return Positioned(
      left: 10,
      top: -2,
      child: Container(
        color: Theme.of(context).colorScheme.surface,
        padding: const EdgeInsets.symmetric(horizontal: 5),
        child: Text(
          textBase.displayName,
          style: TextStyle(
              color: isEditing
                  ? Theme.of(context).colorScheme.primary
                  : Theme.of(context).disabledColor,
              fontSize: 12),
        ),
      ),
    );
  }
}
