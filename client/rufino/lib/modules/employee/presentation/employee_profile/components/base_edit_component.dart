import 'package:flutter/material.dart';

abstract class BaseEditComponent extends StatelessWidget {
  final bool isEditing;
  final bool isLoading;
  final Function(Object value)? onSaveChanges;

  const BaseEditComponent(
      {this.onSaveChanges,
      this.isLoading = true,
      this.isEditing = false,
      super.key});

  BaseEditComponent copyWith(
      {Function(Object value)? onSaveChanges,
      bool? isEditing,
      bool? isLoading});

  @override
  Widget build(BuildContext context) {
    return const Placeholder();
  }
}
