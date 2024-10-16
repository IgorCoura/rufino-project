import 'package:flutter/material.dart';
import 'package:rufino/modules/employee/domain/model/base/enumeration.dart';
import 'package:rufino/modules/employee/domain/model/base/enumeration_list.dart';
import 'package:shimmer/shimmer.dart';

class ListViewFormComponent extends StatefulWidget {
  final bool isEditing;
  final bool isLoading;
  final bool isLazyLoading;
  final EnumerationList enumerationsList;
  final List<Enumeration> enumerationOptionsList;
  final Function(EnumerationList) onSaved;
  const ListViewFormComponent({
    required this.enumerationsList,
    required this.enumerationOptionsList,
    required this.onSaved,
    this.isEditing = true,
    this.isLoading = false,
    this.isLazyLoading = false,
    super.key,
  });

  @override
  State<ListViewFormComponent> createState() => _ListViewFormComponentState();
}

class _ListViewFormComponentState extends State<ListViewFormComponent> {
  late EnumerationList enumerationsList;
  late List<Enumeration> enumerationOptionsList;

  @override
  void initState() {
    enumerationsList = widget.enumerationsList;
    enumerationOptionsList = widget.enumerationOptionsList;
    super.initState();
  }

  @override
  void didUpdateWidget(covariant ListViewFormComponent oldWidget) {
    enumerationsList = widget.enumerationsList;
    enumerationOptionsList = widget.enumerationOptionsList;
    super.didUpdateWidget(oldWidget);
  }

  void removeItem(Enumeration item) {
    setState(() {
      enumerationsList = enumerationsList.removeItem(item);
    });
  }

  void addItem(Enumeration item) {
    setState(() {
      enumerationsList = enumerationsList.addItem(item);
    });
  }

  @override
  Widget build(BuildContext context) {
    return FormField(onSaved: (value) {
      widget.onSaved(enumerationsList);
    }, builder: (state) {
      return Stack(
        children: [
          Padding(
            padding: const EdgeInsets.only(top: 8),
            child: Container(
              padding: const EdgeInsets.all(8),
              decoration: BoxDecoration(
                border: Border.all(
                    color: widget.isEditing
                        ? Theme.of(context).colorScheme.primary
                        : Theme.of(context).disabledColor),
                borderRadius: BorderRadius.circular(5),
              ),
              child: widget.isLoading
                  ? _loadingView(context)
                  : Column(
                      children: [
                        Column(
                          children: enumerationsList.list.isEmpty
                              ? [const Text("Lista em branco")]
                              : enumerationsList.list
                                  .map((x) => _itemView(context, x))
                                  .toList(),
                        ),
                        _footerButton()
                      ],
                    ),
            ),
          ),
          _labelText(context)
        ],
      );
    });
  }

  Widget _loadingView(BuildContext context) {
    return Shimmer.fromColors(
      baseColor: Theme.of(context).colorScheme.surface.withOpacity(0.01),
      highlightColor: Theme.of(context).colorScheme.onSurface.withOpacity(0.1),
      child: Column(
        children: [
          Container(
            width: double.infinity,
            height: 32,
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.onSurface,
              borderRadius: const BorderRadius.all(
                Radius.circular(5),
              ),
            ),
          ),
          const SizedBox(
            height: 16,
          ),
          Container(
            width: double.infinity,
            height: 32,
            decoration: BoxDecoration(
              color: Theme.of(context).colorScheme.onSurface,
              borderRadius: const BorderRadius.all(
                Radius.circular(5),
              ),
            ),
          ),
        ],
      ),
    );
  }

  Widget _itemView(BuildContext context, Enumeration item) {
    return Column(
      children: [
        Container(
          height: 42,
          decoration: BoxDecoration(
            border: Border(
                bottom: BorderSide(
                    color: Theme.of(context).colorScheme.onSurface,
                    width: 0.2)),
          ),
          padding: const EdgeInsets.only(left: 8, right: 8),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Text(item.name),
              widget.isEditing
                  ? IconButton(
                      icon: Icon(Icons.delete,
                          color: Theme.of(context).colorScheme.error),
                      onPressed: () => removeItem(item),
                    )
                  : Container(),
            ],
          ),
        )
      ],
    );
  }

  Widget _footerButton() {
    return Padding(
      padding: const EdgeInsets.only(top: 8.0),
      child: Align(
        alignment: Alignment.centerRight,
        child: widget.isEditing
            ? widget.isLazyLoading
                ? const Padding(
                    padding: EdgeInsets.only(right: 8),
                    child: CircularProgressIndicator(),
                  )
                : TextButton(
                    onPressed: () {
                      _dialogAddItem(context);
                    }, // Add function
                    child: Text(
                      "Adicionar ${enumerationsList.displayName}",
                      style: const TextStyle(fontSize: 14),
                    ),
                  )
            : Container(
                height: 18,
              ),
      ),
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
          enumerationsList.displayName,
          style: TextStyle(
              color: widget.isEditing
                  ? Theme.of(context).colorScheme.primary
                  : Theme.of(context).disabledColor,
              fontSize: 12),
        ),
      ),
    );
  }

  Future _dialogAddItem(BuildContext context) {
    var seletectedValue = enumerationOptionsList.first;
    return showDialog(
        context: context,
        builder: (context) {
          return AlertDialog(
            title: Text('Adicionar ${enumerationsList.displayName}'),
            content: SizedBox(
                width: 600,
                child: DropdownButtonFormField(
                  value: seletectedValue,
                  items: enumerationOptionsList
                      .map((e) =>
                          DropdownMenuItem(value: e, child: Text(e.name)))
                      .toList(),
                  onChanged: (value) {
                    if (value != null) {
                      seletectedValue = value;
                    }
                  },
                  decoration: const InputDecoration(
                    border: OutlineInputBorder(),
                  ),
                )),
            actions: [
              TextButton(
                onPressed: () {
                  Navigator.of(context).pop();
                },
                child: const Text('Cancelar'),
              ),
              ElevatedButton(
                onPressed: () {
                  Navigator.of(context).pop();
                  addItem(seletectedValue);
                },
                child: const Text('Adicionar'),
              ),
            ],
          );
        });
  }
}
