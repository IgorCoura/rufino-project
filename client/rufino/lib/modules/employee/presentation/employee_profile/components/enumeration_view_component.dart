import 'package:flutter/material.dart';
import 'package:rufino/modules/employee/domain/model/enumeration.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/base_edit_component.dart';
import 'package:shimmer/shimmer.dart';

class EnumerationViewComponent extends BaseEditComponent {
  final List<Enumeration> listEnumerationOptions;
  final Enumeration enumeration;

  EnumerationViewComponent(
      {required this.enumeration,
      required this.listEnumerationOptions,
      super.onSaveChanges,
      super.isEditing,
      super.isLoading,
      super.key}) {
    if (enumeration.id > -1 &&
        listEnumerationOptions.any((e) => e.id == enumeration.id) == false) {
      listEnumerationOptions.add(enumeration);
    }
  }

  @override
  EnumerationViewComponent copyWith(
      {Function(Object value)? onSaveChanges,
      bool? isEditing,
      bool? isLoading,
      GlobalKey<FormState>? formKey}) {
    return EnumerationViewComponent(
        enumeration: enumeration,
        onSaveChanges: onSaveChanges ?? this.onSaveChanges,
        listEnumerationOptions: listEnumerationOptions,
        isEditing: isEditing ?? this.isEditing,
        isLoading: isLoading ?? this.isLoading);
  }

  @override
  Widget build(BuildContext context) {
    return isLoading
        ? Shimmer.fromColors(
            baseColor: Theme.of(context).colorScheme.surface,
            highlightColor: Theme.of(context).colorScheme.onSurface,
            child: DropdownButtonFormField(
              items: []
                  .map(
                      (e) => DropdownMenuItem(value: e.id, child: Text(e.name)))
                  .toList(),
              onChanged: null,
              decoration: const InputDecoration(
                  enabled: false,
                  labelText: ".......",
                  border: OutlineInputBorder()),
            ),
          )
        : DropdownButtonFormField(
            value: enumeration,
            items: listEnumerationOptions
                .map((e) => DropdownMenuItem(value: e, child: Text(e.name)))
                .toList(),
            onChanged: isEditing ? (Enumeration? value) {} : null,
            onSaved: (newValue) {
              if (onSaveChanges != null && newValue != enumeration) {
                onSaveChanges!(newValue!);
              }
            },
            validator: (value) {
              if (value == null || value == Enumeration.emptyId) {
                return 'Por favor, selecione um opção.';
              }
              return null;
            },
            decoration: InputDecoration(
              enabled: isEditing,
              labelText: enumeration.displayName,
              border: const OutlineInputBorder(),
            ),
          );
  }
}
