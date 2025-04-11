import 'package:flutter/material.dart';
import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';
import 'package:rufino/modules/employee/presentation/components/base_edit_component.dart';
import 'package:shimmer/shimmer.dart';

class EnumerationViewComponent extends BaseEditComponent {
  late final List<Enumeration> _listEnumerationOptions;
  final Enumeration enumeration;
  final Function(Object obj)? onChanged;

  EnumerationViewComponent(
      {required this.enumeration,
      required List<Enumeration> listEnumerationOptions,
      this.onChanged,
      super.onSaveChanges,
      super.isEditing,
      super.isLoading,
      super.key}) {
    List<Enumeration> list = [];
    if (listEnumerationOptions.any((e) => e.id == enumeration.id) == false) {
      list.add(enumeration);
    }
    list.addAll(listEnumerationOptions);
    _listEnumerationOptions = list;
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
        onChanged: onChanged,
        listEnumerationOptions: _listEnumerationOptions,
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
            items: _listEnumerationOptions
                .map((e) => DropdownMenuItem(value: e, child: Text(e.name)))
                .toList(),
            onChanged: isEditing
                ? (Enumeration? value) {
                    if (this.onChanged != null && value != null) {
                      onChanged!(value);
                    }
                  }
                : null,
            onSaved: (newValue) {
              if (onSaveChanges != null && newValue != enumeration) {
                onSaveChanges!(newValue!);
              }
            },
            validator: (value) {
              if (value == null || value.id == Enumeration.emptyId) {
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
