import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/storage/bloc/order/storage_order_bloc.dart'
    as bloc;
import 'package:rufino_smart_app/storage/components/item_details_component.dart';
import 'package:rufino_smart_app/storage/delegates/order_item_search_delegate.dart';
import 'package:rufino_smart_app/storage/model/storage_order_item_model.dart';
import 'package:rufino_smart_app/utils/constants.dart';

class StorageOrderPage extends StatelessWidget {
  StorageOrderPage({Key? key}) : super(key: key);

  final storageBloc = Modular.get<bloc.StorageOrderBloc>();

  List tempList = [
    StorageOrderItemModel("", "Tubo PVC", "description", 100, "m")
  ];

  @override
  Widget build(BuildContext context) {
    Size size = MediaQuery.of(context).size;
    return Scaffold(
      backgroundColor: kBackGroundColor,
      appBar: AppBar(
        backgroundColor: kPrimaryColor,
        title: const Text("Ordem de retirada"),
        leading: BackButton(
          onPressed: () => Modular.to.navigate('/storage'),
        ),
      ),
      body: Center(
        child: BlocBuilder<bloc.StorageOrderBloc, bloc.StorageOrderState>(
          bloc: storageBloc,
          builder: (context, state) {
            return Stack(
              children: [
                Center(
                  child: Container(
                    constraints: const BoxConstraints(maxWidth: 1000),
                    child: ListView.builder(
                      itemCount:
                          state.itens.isEmpty ? 1 : state.itens.length + 1,
                      itemBuilder: (BuildContext context, int index) {
                        if (state.itens.length > index) {
                          return _item(size, index, state.itens[index]);
                        } else {
                          return _buttons(context);
                        }
                      },
                    ),
                  ),
                ),
                if (state is bloc.EditItemState)
                  ItemDetailsComponent(
                    item: state.itens[state.index],
                    checkButton: () {
                      storageBloc.add(bloc.ReturnInitialEvent());
                    },
                    backButton: () {
                      storageBloc.add(bloc.ReturnInitialEvent());
                    },
                    removeButton: () {
                      storageBloc.add(bloc.RemoveItemEvent(state.index));
                    },
                  ),
              ],
            );
          },
        ),
      ),
    );
  }

  _buttons(BuildContext context) {
    Size size = MediaQuery.of(context).size;
    return Column(
      children: [
        Row(
          children: [
            Expanded(
                child: Padding(
              padding: const EdgeInsets.all(8.0),
              child: ElevatedButton(
                style: ButtonStyle(
                    backgroundColor: MaterialStateColor.resolveWith(
                        (states) => kPrimaryColor)),
                onPressed: () async {
                  var item = await showSearch(
                      context: context, delegate: OrderItemSearchDelegate());
                  if (item != null) {
                    storageBloc.add(bloc.InsertItemInListEvent(item));
                  }
                },
                child: const SizedBox(
                  height: 35,
                  child: Icon(Icons.add),
                ),
              ),
            )),
            Expanded(
              child: Padding(
                padding: const EdgeInsets.all(8.0),
                child: ElevatedButton(
                  style: ButtonStyle(
                      backgroundColor: MaterialStateColor.resolveWith(
                          (states) => kPrimaryColor)),
                  onPressed: () {},
                  child: const SizedBox(
                    height: 35,
                    child: Icon(Icons.qr_code),
                  ),
                ),
              ),
            ),
          ],
        ),
        Padding(
          padding: const EdgeInsets.all(8.0),
          child: SizedBox(
            height: 45,
            width: size.width >= 400 ? 400 : size.width,
            child: ElevatedButton(
              style: ButtonStyle(
                  backgroundColor: MaterialStateColor.resolveWith(
                      (states) => kPrimaryColor)),
              onPressed: () {},
              child: const Text(
                "FINALIZAR",
                style: TextStyle(
                    color: Color(0xFFD32F2F),
                    fontWeight: FontWeight.bold,
                    overflow: TextOverflow.ellipsis,
                    fontSize: 18,
                    shadows: [
                      Shadow(
                          // bottomLeft
                          offset: Offset(-1, -1),
                          color: Colors.black),
                      Shadow(
                          // bottomRight
                          offset: Offset(1, -1),
                          color: Colors.black),
                      Shadow(
                          // topRight
                          offset: Offset(1, 1),
                          color: Colors.black),
                      Shadow(
                          // topLeft
                          offset: Offset(-1, 1),
                          color: Colors.black),
                    ]),
              ),
            ),
          ),
        ),
      ],
    );
  }

  _item(Size size, int index, StorageOrderItemModel model) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(8, 8, 8, 0),
      child: ListTile(
        tileColor: Colors.white,
        onTap: () {
          storageBloc.add(bloc.EditItemEvent(index));
        },
        leading: const Icon(
          Icons.download,
          color: Colors.red,
        ),
        title: Text(
          model.name,
          overflow: TextOverflow.ellipsis,
        ),
        trailing: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            size.width < 500
                ? Container()
                : IconButton(
                    onPressed: () {
                      model.quantity += 1;
                      storageBloc.add(const bloc.ChangeQuantityItemEvent());
                    },
                    icon: Icon(Icons.add),
                  ),
            BlocBuilder<bloc.StorageOrderBloc, bloc.StorageOrderState>(
              bloc: storageBloc,
              builder: (context, state) {
                return Text(
                  "${state.itens[index].quantity.toString()} ${state.itens[index].unity}",
                  style: const TextStyle(color: kPrimaryColor),
                );
              },
            ),
            size.width < 500
                ? Container()
                : IconButton(
                    onPressed: () {
                      model.quantity -= 1;
                      storageBloc.add(const bloc.ChangeQuantityItemEvent());
                    },
                    icon: const Icon(Icons.remove),
                  ),
            size.width < 500
                ? Container()
                : const SizedBox(
                    width: 10,
                  ),
            size.width < 500
                ? Container()
                : IconButton(
                    onPressed: () {
                      storageBloc.add(bloc.RemoveItemEvent(index));
                    },
                    icon: const Icon(
                      Icons.delete,
                      color: Colors.red,
                    ))
          ],
        ),
      ),
    );
  }
}
