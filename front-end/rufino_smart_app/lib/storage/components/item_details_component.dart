import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/storage/bloc/order/storage_order_bloc.dart';
import 'package:rufino_smart_app/storage/bloc/search/storage_order__search_bloc.dart';
import 'package:rufino_smart_app/storage/model/storage_order_item_model.dart';
import 'package:rufino_smart_app/utils/constants.dart';

class ItemDetailsComponent extends StatefulWidget {
  final Function() checkButton;
  final Function()? removeButton;
  final Function() backButton;
  final StorageOrderItemModel item;

  const ItemDetailsComponent({
    required this.item,
    required this.checkButton,
    required this.backButton,
    Key? key,
    this.removeButton,
  }) : super(key: key);

  @override
  State<ItemDetailsComponent> createState() => _ItemDetailsComponentState();
}

class _ItemDetailsComponentState extends State<ItemDetailsComponent> {
  int _quantity = 0;
  final controller = TextEditingController();

  @override
  void initState() {
    _quantity = widget.item.quantity;
    controller.text = widget.item.quantity.toString();
    super.initState();
  }

  _changeQuantity(int value) {
    setState(() {
      _quantity = value;
      controller.text = _quantity.toString();
    });
  }

  @override
  Widget build(BuildContext context) {
    Size size = MediaQuery.of(context).size;
    return Container(
      color: kPrimaryColor.withOpacity(0.6),
      height: double.infinity,
      width: double.infinity,
      child: Center(
        child: Padding(
          padding: const EdgeInsets.all(8.0),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              Container(
                constraints: const BoxConstraints(maxWidth: 600),
                padding: const EdgeInsets.all(16),
                width: size.width,
                decoration: BoxDecoration(
                  borderRadius: BorderRadius.circular(30),
                  color: kPrimaryDarkColor,
                ),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    _textBox(),
                    _fieldBox(),
                    const SizedBox(
                      height: 10,
                    ),
                    _buttonBox()
                  ],
                ),
              ),
              size.height * 0.2 < 100
                  ? Container()
                  : SizedBox(
                      height: size.height * 0.2,
                    ),
            ],
          ),
        ),
      ),
    );
  }

  _textBox() {
    return Column(
      children: [
        Text(
          widget.item.name,
          textAlign: TextAlign.justify,
          style: const TextStyle(
            color: Colors.white,
            fontSize: 22,
          ),
        ),
        const Center(
          child: Text(
            "Descrição",
            style: TextStyle(
                color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
          ),
        ),
        Padding(
          padding: const EdgeInsets.all(8.0),
          child: Text(
            widget.item.description,
            textAlign: TextAlign.justify,
            style: const TextStyle(color: Colors.white),
          ),
        ),
        const SizedBox(height: 10),
      ],
    );
  }

  _fieldBox() {
    return Row(
      mainAxisSize: MainAxisSize.min,
      mainAxisAlignment: MainAxisAlignment.center,
      children: [
        Container(
          alignment: Alignment.center,
          margin: const EdgeInsets.only(right: 8),
          decoration: BoxDecoration(
              color: kSecondaryColor, borderRadius: BorderRadius.circular(30)),
          child: IconButton(
            onPressed: () {
              _changeQuantity(_quantity + 1);
            },
            icon: const Icon(
              Icons.add,
              color: Colors.black,
              size: 30,
            ),
          ),
        ),
        Expanded(
          child: TextFormField(
            controller: controller,
            inputFormatters: <TextInputFormatter>[
              FilteringTextInputFormatter.digitsOnly
            ],
            keyboardType: TextInputType.number,
            style: const TextStyle(color: Colors.white, fontSize: 18),
            textAlign: TextAlign.center,
            onFieldSubmitted: (value) {
              if (value == "") {
                value = "0";
              }
              _changeQuantity(int.parse(value));
            },
          ),
        ),
        Text(
          widget.item.unity,
          style: const TextStyle(
            color: Colors.white,
            fontSize: 18,
          ),
        ),
        Container(
          alignment: Alignment.center,
          margin: const EdgeInsets.only(left: 8),
          decoration: BoxDecoration(
              color: kSecondaryColor, borderRadius: BorderRadius.circular(30)),
          child: IconButton(
            onPressed: () {
              _changeQuantity(_quantity - 1);
            },
            icon: const Icon(
              Icons.remove,
              color: Colors.black,
              size: 30,
            ),
          ),
        ),
      ],
    );
  }

  _buttonBox() {
    return Column(
      children: [
        Row(
          mainAxisAlignment: MainAxisAlignment.center,
          children: [
            Expanded(
              child: Padding(
                padding: const EdgeInsets.symmetric(horizontal: 8),
                child: ElevatedButton(
                  onPressed: () {
                    widget.item.quantity = _quantity;
                    widget.checkButton();
                  },
                  child: const Icon(
                    Icons.check_outlined,
                    size: 30,
                  ),
                  style: ButtonStyle(
                      backgroundColor: MaterialStateColor.resolveWith(
                          (states) => Colors.green)),
                ),
              ),
            ),
            widget.removeButton != null
                ? Expanded(
                    child: Padding(
                      padding: const EdgeInsets.symmetric(horizontal: 8),
                      child: ElevatedButton(
                        onPressed: () {
                          widget.removeButton!();
                        },
                        child: const Icon(
                          Icons.delete,
                          size: 30,
                        ),
                        style: ButtonStyle(
                            backgroundColor: MaterialStateColor.resolveWith(
                                (states) => Colors.red)),
                      ),
                    ),
                  )
                : Container(),
          ],
        ),
        Container(
          margin: const EdgeInsets.only(top: 16),
          width: double.infinity,
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 8),
            child: ElevatedButton(
              onPressed: () {
                widget.backButton();
              },
              child: Row(
                mainAxisAlignment: MainAxisAlignment.center,
                children: const [
                  Icon(
                    Icons.arrow_back,
                    size: 30,
                  ),
                  Padding(
                    padding: EdgeInsets.only(left: 8),
                    child: Text(
                      "VOLTAR",
                      style:
                          TextStyle(fontWeight: FontWeight.bold, fontSize: 16),
                    ),
                  )
                ],
              ),
              style: ButtonStyle(
                  backgroundColor:
                      MaterialStateColor.resolveWith((states) => Colors.grey)),
            ),
          ),
        )
      ],
    );
  }
}
