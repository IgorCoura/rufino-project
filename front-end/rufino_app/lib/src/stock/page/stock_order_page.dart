import 'dart:ffi';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/stock/bloc/stock_home_bloc.dart';
import 'package:rufino_app/src/stock/components/edit_item_dialog_component.dart';
import 'package:rufino_app/src/stock/model/stock_order_item_model.dart';

class StockOrderPage extends StatelessWidget {
  StockOrderPage({Key? key}) : super(key: key);

  final bloc = Modular.get<StockHomeBloc>();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: BackButton(
          onPressed: () => Modular.to.navigate('/stock'),
        ),
        title: const Text("Ordem de retirada"),
        actions: [
          IconButton(
            onPressed: () {},
            icon: const Icon(
              Icons.more_vert,
              size: 26,
            ),
          ),
        ],
      ),
      body: BlocBuilder<StockHomeBloc, StockHomeState>(
        bloc: bloc,
        builder: (context, state) {
          return ListView.builder(
            itemCount: state.items.length + 1,
            itemBuilder: (context, index) {
              if (index == state.items.length && state.items.isNotEmpty) {
                return _buttonsWidgt(context);
              }
              if (state.items.isNotEmpty) {
                return _itemTileWidget(context, index, state.items[index]);
              }
              return const Center(
                child: Padding(
                  padding: EdgeInsets.all(16.0),
                  child: Text(
                    "Vaziou",
                    style: TextStyle(fontWeight: FontWeight.bold, fontSize: 24),
                  ),
                ),
              );
            },
          );
        },
      ),
    );
  }

  Widget _itemTileWidget(
      BuildContext context, int index, StockOrderItemModel model) {
    return ListTile(
      leading: const Icon(
        Icons.download,
        color: Colors.red,
      ),
      title: TextButton(
        style: const ButtonStyle(alignment: Alignment.bottomLeft),
        onPressed: () {
          showDialog(
              context: context,
              builder: (context) {
                return EditItemDialogComponent(
                    item: model,
                    returnFunction: (returnItem) {
                      bloc.add(ChangeItemEvent(index, returnItem));
                    });
              });
        },
        child: Text(
          model.name,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(fontSize: 18),
        ),
      ),
      trailing: Row(mainAxisSize: MainAxisSize.min, children: [
        IconButton(
          onPressed: () {
            bloc.add(AddQuantiyItemEvent(index));
          },
          icon: const Icon(Icons.add),
        ),
        Text("${model.quantityVariation} ${model.unity}"),
        IconButton(
          onPressed: () {
            bloc.add(SubtractQuantiyItemEvent(index));
          },
          icon: const Icon(Icons.remove),
        ),
        IconButton(
            onPressed: () {
              bloc.add(RemoveItemEvent(index));
            },
            icon: const Icon(
              Icons.delete,
              color: Colors.red,
            ))
      ]),
    );
  }

  Widget _buttonsWidgt(BuildContext context) {
    return Row(
      children: [
        Expanded(
            child: Padding(
          padding: const EdgeInsets.all(8.0),
          child: ElevatedButton(
              style: ButtonStyle(
                  backgroundColor:
                      MaterialStateColor.resolveWith((states) => Colors.green)),
              onPressed: () {},
              child: const Padding(
                padding: EdgeInsets.all(8.0),
                child: Icon(
                  Icons.check,
                  size: 32,
                ),
              )),
        )),
        Expanded(
            child: Padding(
          padding: const EdgeInsets.all(8.0),
          child: ElevatedButton(
              style: ButtonStyle(
                  backgroundColor:
                      MaterialStateColor.resolveWith((states) => Colors.red)),
              onPressed: () => showDialog(
                  context: context,
                  builder: (BuildContext context) => AlertDialog(
                        title: const Text(
                          "Certeza que desejar remover todos os itens",
                          style: TextStyle(fontSize: 24),
                        ),
                        actions: [
                          Row(
                            children: [
                              Expanded(
                                child: TextButton(
                                  onPressed: () {
                                    Navigator.pop(context, 'Cancel');
                                    bloc.add(RemoveAllItemsEvent());
                                  },
                                  child: const Text(
                                    'SIM',
                                    style: TextStyle(fontSize: 24),
                                  ),
                                ),
                              ),
                              Expanded(
                                child: TextButton(
                                  onPressed: () => Navigator.pop(context, 'OK'),
                                  child: const Text(
                                    'NÃO',
                                    style: TextStyle(fontSize: 24),
                                  ),
                                ),
                              ),
                            ],
                          )
                        ],
                      )),
              child: const Padding(
                padding: EdgeInsets.all(8.0),
                child: Icon(
                  Icons.delete_forever,
                  size: 32,
                ),
              )),
        )),
      ],
    );
  }
}
