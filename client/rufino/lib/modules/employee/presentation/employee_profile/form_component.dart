import 'package:flutter/material.dart';
import 'package:rufino/modules/employee/domain/model/model_base.dart';
import 'package:rufino/modules/employee/domain/model/prop_base.dart';
import 'package:shimmer/shimmer.dart';

class FormComponent extends StatefulWidget {
  final ModelBase modelBase;
  final Function saveFunction;
  final String formName;
  final Function loadingFormData;
  final bool isSavingData;
  const FormComponent(
      {required this.modelBase,
      required this.saveFunction,
      required this.formName,
      required this.loadingFormData,
      required this.isSavingData,
      super.key});

  @override
  State<FormComponent> createState() => _FormComponentState();
}

class _FormComponentState extends State<FormComponent> {
  final _formKey = GlobalKey<FormState>();
  bool _isExpanded = false;
  bool _isEditing = false;

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        border: Border.all(),
        borderRadius: BorderRadius.circular(5),
      ),
      child: Column(
        children: [
          Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              border: _isExpanded ? const Border(bottom: BorderSide()) : null,
              borderRadius: BorderRadius.circular(5),
            ),
            child: InkWell(
              onTap: () => {
                setState(() {
                  _isExpanded = !_isExpanded;
                  if (_isExpanded == false) {
                    _isEditing = false;
                  } else {
                    widget.loadingFormData();
                  }
                })
              },
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    widget.formName,
                    style: const TextStyle(
                        fontWeight: FontWeight.bold, fontSize: 16),
                  ),
                  const Icon(
                    Icons.arrow_drop_down_sharp,
                  )
                ],
              ),
            ),
          ),
          _isExpanded
              ? Padding(
                  padding: const EdgeInsets.all(8.0),
                  child: Form(
                      key: _formKey,
                      child: Column(
                        children: [
                          const SizedBox(
                            height: 16,
                          ),
                          Column(
                              children: widget.modelBase.props
                                  .map((prop) => _textFormField(context, prop))
                                  .toList()),
                          Align(
                            alignment: Alignment.centerRight,
                            child: widget.isSavingData
                                ? const CircularProgressIndicator()
                                : _isEditing
                                    ? FilledButton(
                                        onPressed: () {
                                          if (_formKey.currentState != null &&
                                              _formKey.currentState!
                                                  .validate()) {
                                            _formKey.currentState!.save();
                                            setState(() {
                                              _isEditing = false;
                                            });
                                            widget.saveFunction();
                                          }
                                        },
                                        child: const Text("Salvar"),
                                      )
                                    : TextButton(
                                        onPressed: () {
                                          setState(() {
                                            _isEditing = true;
                                          });
                                        },
                                        child: const Text("Editar"),
                                      ),
                          ),
                        ],
                      )),
                )
              : Container()
        ],
      ),
    );
  }

  Widget _textFormField(BuildContext context, PropBase prop) {
    return Column(
      children: [
        widget.modelBase.isLoading
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
                    prop.formatter != null ? [prop.formatter!] : null,
                keyboardType: prop.inputType,
                controller: TextEditingController(text: prop.value),
                enabled: _isEditing,
                decoration: InputDecoration(
                    labelText: prop.displayName,
                    border: const OutlineInputBorder()),
                style:
                    TextStyle(color: Theme.of(context).colorScheme.onSurface),
                validator: (value) => prop.validate(value),
                onSaved: (newValue) {
                  if (newValue != null) {
                    prop.value = newValue;
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
