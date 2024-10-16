import 'package:flutter/material.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/base_edit_component.dart';

class PropsContainerComponent extends StatefulWidget {
  final String containerName;
  final bool isSavingData;
  final Function loadingContainerData;
  final Function? loadingLazyContainerData;
  final Function(List<Object> changes) saveContainerData;
  final List<BaseEditComponent> children;
  final bool isLoading;
  final bool isLazyLoading;
  const PropsContainerComponent(
      {required this.children,
      required this.containerName,
      required this.isSavingData,
      required this.saveContainerData,
      required this.loadingContainerData,
      required this.isLoading,
      this.loadingLazyContainerData,
      this.isLazyLoading = false,
      super.key});

  @override
  State<PropsContainerComponent> createState() =>
      _PropsContainerComponentState();
}

class _PropsContainerComponentState extends State<PropsContainerComponent> {
  final _formKey = GlobalKey<FormState>();
  bool _isExpanded = false;
  bool _isEditing = false;
  late List<BaseEditComponent> _children;
  List<Object> _childrenChages = [];

  @override
  void initState() {
    _children = widget.children;
    setChildrenConfigs(isEditing: _isEditing, isLoading: widget.isLoading);
    super.initState();
  }

  @override
  void didUpdateWidget(covariant PropsContainerComponent oldWidget) {
    _children = widget.children;
    setChildrenConfigs(isEditing: _isEditing, isLoading: widget.isLoading);
    super.didUpdateWidget(oldWidget);
  }

  void addChildChange(Object obj) {
    _childrenChages.add(obj);
  }

  void changeEditState(bool isEditing) {
    setState(() {
      _isEditing = isEditing;
      setChildrenConfigs(isEditing: _isEditing);
      if (widget.loadingLazyContainerData != null) {
        widget.loadingLazyContainerData!();
      }
    });
  }

  void setChildrenConfigs({bool? isEditing, bool? isLoading}) {
    _children = _children
        .map((child) => child.copyWith(
              onSaveChanges: addChildChange,
              isEditing: isEditing,
              isLoading: isLoading,
            ))
        .toList();
  }

  void saveChanges() {
    if (_formKey.currentState != null && _formKey.currentState!.validate()) {
      _formKey.currentState!.save();
      changeEditState(false);
      widget.saveContainerData(_childrenChages);
      _childrenChages = [];
    }
  }

  void expand() {
    setState(() {
      _isExpanded = !_isExpanded;
      if (_isExpanded) {
        widget.loadingContainerData();
        _childrenChages = [];
      } else {
        changeEditState(false);
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        border: Border.all(),
        borderRadius: BorderRadius.circular(5),
      ),
      child: Column(
        children: [
          _header(),
          _isExpanded
              ? Column(
                  children: [
                    _body(),
                    _buttons(),
                  ],
                )
              : Container(),
        ],
      ),
    );
  }

  Widget _body() {
    return Padding(
      padding: const EdgeInsets.all(8.0),
      child: Form(
          key: _formKey,
          child: Column(
            children: _children
                .expand((child) => [
                      const SizedBox(
                        height: 16,
                      ),
                      child
                    ])
                .toList(),
          )),
    );
  }

  Widget _header() {
    return Container(
      padding: const EdgeInsets.all(8),
      decoration: BoxDecoration(
        border: _isExpanded ? const Border(bottom: BorderSide()) : null,
        borderRadius: BorderRadius.circular(5),
      ),
      child: InkWell(
        onTap: () => expand(),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              widget.containerName,
              style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
            ),
            const Icon(
              Icons.arrow_drop_down_sharp,
            )
          ],
        ),
      ),
    );
  }

  Widget _buttons() {
    return Padding(
      padding: const EdgeInsets.all(8),
      child: widget.isSavingData
          ? const CircularProgressIndicator()
          : _isEditing
              ? Row(
                  mainAxisAlignment: MainAxisAlignment.end,
                  children: [
                    TextButton(
                      onPressed: () => {changeEditState(false), expand()},
                      child: const Text("Cancelar"),
                    ),
                    FilledButton(
                      onPressed: () => saveChanges(),
                      child: const Text("Salvar"),
                    ),
                  ],
                )
              : Align(
                  alignment: Alignment.centerRight,
                  child: TextButton(
                    onPressed: () => changeEditState(true),
                    child: const Text("Editar"),
                  ),
                ),
    );
  }
}
