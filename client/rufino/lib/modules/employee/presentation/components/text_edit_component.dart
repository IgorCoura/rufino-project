import 'package:flutter/material.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';
import 'package:rufino/modules/employee/presentation/components/base_edit_component.dart';
import 'package:shimmer/shimmer.dart';

class TextEditComponent extends BaseEditComponent {
  final TextPropBase textProp;
  final Function(Object value)? onChangedValue;

  const TextEditComponent({
    super.onSaveChanges,
    this.onChangedValue,
    super.isEditing,
    super.isLoading,
    super.key,
    required this.textProp,
  });

  @override
  TextEditComponent copyWith({
    Function(Object value)? onSaveChanges,
    bool? isEditing,
    bool? isLoading,
    GlobalKey<FormState>? formKey,
  }) =>
      TextEditComponent(
        onSaveChanges: onSaveChanges ?? this.onSaveChanges,
        isEditing: isEditing ?? this.isEditing,
        textProp: textProp,
        isLoading: isLoading ?? this.isLoading,
        onChangedValue: onChangedValue,
      );

  @override
  Widget build(BuildContext context) {
    return _TextEditComponentStateful(
      textProp: textProp,
      onSaveChanges: onSaveChanges,
      onChangedValue: onChangedValue,
      isEditing: isEditing,
      isLoading: isLoading,
    );
  }
}

class _TextEditComponentStateful extends StatefulWidget {
  final TextPropBase textProp;
  final Function(Object value)? onChangedValue;
  final Function(Object value)? onSaveChanges;
  final bool isEditing;
  final bool isLoading;

  const _TextEditComponentStateful({
    this.onSaveChanges,
    this.onChangedValue,
    this.isEditing = false,
    this.isLoading = false,
    required this.textProp,
  });

  @override
  State<_TextEditComponentStateful> createState() =>
      _TextEditComponentStatefulState();
}

class _TextEditComponentStatefulState
    extends State<_TextEditComponentStateful> {
  late TextEditingController _controller;

  @override
  void initState() {
    super.initState();
    _controller = TextEditingController(text: widget.textProp.value);
  }

  @override
  void didUpdateWidget(_TextEditComponentStateful oldWidget) {
    super.didUpdateWidget(oldWidget);
    // Só atualiza o texto se o valor externo mudou E é diferente do texto atual
    if (oldWidget.textProp.value != widget.textProp.value &&
        _controller.text != widget.textProp.value) {
      _controller.text = widget.textProp.value;
    }
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    TextPropBase value = widget.textProp;
    return Column(
      children: [
        widget.isLoading
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
                inputFormatters: widget.textProp.formatter != null
                    ? [widget.textProp.formatter!]
                    : null,
                keyboardType: widget.textProp.inputType,
                controller: _controller,
                enabled: widget.isEditing,
                decoration: InputDecoration(
                  labelText: widget.textProp.displayName,
                  border: const OutlineInputBorder(),
                ),
                style:
                    TextStyle(color: Theme.of(context).colorScheme.onSurface),
                validator: (value) => widget.textProp.validate(value),
                onSaved: (newValue) {
                  if (newValue != null &&
                      widget.onSaveChanges != null &&
                      newValue != widget.textProp.value) {
                    var tempTextProp =
                        widget.textProp.copyWith(value: newValue);
                    widget.onSaveChanges!(tempTextProp);
                  }
                },
                onChanged: (newValue) {
                  if (newValue != widget.textProp.value) {
                    value = widget.textProp.copyWith(value: newValue);
                    if (widget.onChangedValue != null) {
                      widget.onChangedValue!(value);
                    }
                  }
                },
              ),
      ],
    );
  }
}
