import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:rufino_app/src/stock/model/stock_order_item_model.dart';

class EditItemDialogWidget extends StatelessWidget {
  final TextEditingController controller = TextEditingController();
  final StockOrderItemModel item;
  final Function(StockOrderItemModel item) returnFunction;

  EditItemDialogWidget({
    Key? key,
    required this.item,
    required this.returnFunction,
  }) : super(key: key) {
    controller.text = item.quantityVariation.toString();
  }

  _subtractQuantity() {
    var value = controller.text == "" ? 0 : int.parse(controller.text) - 1;
    controller.text = value < 0 ? "0" : value.toString();
  }

  _addQuantity() {
    var value = controller.text == "" ? 0 : int.parse(controller.text);
    controller.text = (value + 1).toString();
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(item.name),
      content: Text(item.description),
      actions: [
        _textFieldAlertDialogWidget(context),
        _buttonsAlertDialogWidget(context),
      ],
    );
  }

  Widget _textFieldAlertDialogWidget(BuildContext context) {
    return Center(
      child: Row(mainAxisSize: MainAxisSize.min, children: [
        IconButton(
          onPressed: () {
            _addQuantity();
          },
          icon: const Icon(
            Icons.add,
            size: 32,
          ),
        ),
        SizedBox(
          width: 200,
          child: TextField(
            keyboardType: TextInputType.number,
            inputFormatters: [FilteringTextInputFormatter.digitsOnly],
            style: const TextStyle(fontSize: 24),
            textAlign: TextAlign.center,
            controller: controller,
            decoration: const InputDecoration(
                hintText: 'Quantidade', border: InputBorder.none),
            // onChanged: onSearchTextChanged,
          ),
        ),
        IconButton(
          onPressed: () {
            _subtractQuantity();
          },
          icon: const Icon(
            Icons.remove,
            size: 32,
          ),
        ),
      ]),
    );
  }

  Widget _buttonsAlertDialogWidget(BuildContext context) {
    return Row(
      children: [
        Expanded(
          child: Padding(
            padding: const EdgeInsets.all(8.0),
            child: ElevatedButton(
              onPressed: () {
                item.quantityVariation =
                    controller.text == "" ? 0 : int.parse(controller.text);
                Navigator.pop(context);
                returnFunction(item);
              },
              child: const Text("INSERIR"),
            ),
          ),
        ),
        Expanded(
          child: Padding(
            padding: const EdgeInsets.all(8.0),
            child: ElevatedButton(
              onPressed: () {
                Navigator.pop(context);
              },
              child: const Text("CANCELAR"),
            ),
          ),
        ),
      ],
    );
  }
}
