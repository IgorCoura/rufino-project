import 'package:flutter/material.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/base_edit_component.dart';
import 'package:shimmer/shimmer.dart';

class TextEditComponent extends BaseEditComponent {
  final TextPropBase textProp;

  const TextEditComponent(
      {super.onSaveChanges,
      super.isEditing,
      super.isLoading,
      super.key,
      required this.textProp});

  @override
  TextEditComponent copyWith(
          {Function(Object value)? onSaveChanges,
          bool? isEditing,
          bool? isLoading,
          GlobalKey<FormState>? formKey}) =>
      TextEditComponent(
        onSaveChanges: onSaveChanges ?? this.onSaveChanges,
        isEditing: isEditing ?? this.isEditing,
        textProp: textProp,
        isLoading: isLoading ?? this.isLoading,
      );

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        isLoading
            ? Shimmer.fromColors(
                baseColor: Theme.of(context).colorScheme.surface,
                highlightColor: Theme.of(context).colorScheme.onSurface,
                child: TextFormField(
                  enabled: false,
                  decoration: const InputDecoration(
                    border: OutlineInputBorder(),
                  ),
                ),
              )
            : TextFormField(
                inputFormatters:
                    textProp.formatter != null ? [textProp.formatter!] : null,
                keyboardType: textProp.inputType,
                controller: TextEditingController(text: textProp.value),
                enabled: isEditing,
                decoration: InputDecoration(
                  labelText: textProp.displayName,
                  border: const OutlineInputBorder(),
                ),
                style:
                    TextStyle(color: Theme.of(context).colorScheme.onSurface),
                validator: (value) => textProp.validate(value),
                onSaved: (newValue) {
                  if (newValue != null &&
                      onSaveChanges != null &&
                      newValue != textProp.value) {
                    var tempTextProp = textProp.copyWith(value: newValue);
                    onSaveChanges!(tempTextProp);
                  }
                },
              ),
        const SizedBox(
          height: 16,
        ),
      ],
    );
  }
}
