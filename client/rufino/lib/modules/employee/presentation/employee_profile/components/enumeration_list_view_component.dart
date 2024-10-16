import 'package:flutter/material.dart';
import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';
import 'package:rufino/modules/employee/domain/model/base/enumeration_list.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/base_edit_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/list_view_form_component.dart';

class EnumerationListViewComponent extends BaseEditComponent {
  final EnumerationList enumerationsList;
  final List<Enumeration> enumerationOptionsList;

  const EnumerationListViewComponent(
      {required this.enumerationsList,
      required this.enumerationOptionsList,
      super.onSaveChanges,
      super.isEditing,
      super.isLoading,
      super.key});

  @override
  EnumerationListViewComponent copyWith(
      {Function(Object value)? onSaveChanges,
      bool? isEditing,
      bool? isLoading,
      GlobalKey<FormState>? formKey}) {
    return EnumerationListViewComponent(
        enumerationsList: enumerationsList,
        enumerationOptionsList: enumerationOptionsList,
        onSaveChanges: onSaveChanges ?? this.onSaveChanges,
        isEditing: isEditing ?? this.isEditing,
        isLoading: isLoading ?? this.isLoading);
  }

  @override
  Widget build(BuildContext context) {
    return ListViewFormComponent(
        enumerationsList: enumerationsList,
        enumerationOptionsList: enumerationOptionsList,
        isEditing: isEditing,
        isLoading: isLoading,
        onSaved: (value) {
          if (onSaveChanges != null) {
            onSaveChanges!(value);
          }
        });
  }
}
