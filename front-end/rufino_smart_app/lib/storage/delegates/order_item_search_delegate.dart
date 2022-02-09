import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/storage/bloc/order/storage_order_bloc.dart';
import 'package:rufino_smart_app/storage/components/item_details_component.dart';
import 'package:rufino_smart_app/storage/model/storage_order_item_model.dart';
import 'package:rufino_smart_app/utils/constants.dart';

class OrderItemSearchDelegate extends SearchDelegate {
  final bloc = Modular.get<StorageOrderBloc>();

  @override
  List<Widget>? buildActions(BuildContext context) {
    return [
      IconButton(
        onPressed: () {
          query = "";
        },
        icon: const Icon(Icons.clear),
      ),
    ];
  }

  @override
  Widget? buildLeading(BuildContext context) {
    return IconButton(
      onPressed: () {
        bloc.add(ReturnInitialEvent());
        close(context, null);
      },
      icon: AnimatedIcon(
        icon: AnimatedIcons.menu_arrow,
        progress: transitionAnimation,
      ),
    );
  }

  @override
  Widget buildResults(BuildContext context) {
    return Container();
  }

  @override
  Widget buildSuggestions(BuildContext context) {
    if (query.isEmpty) {
      return Container();
    } else {
      return Stack(
        children: [
          ListView(
            children: [
              _item(
                  context,
                  StorageOrderItemModel(
                      "a", " name", "description", 100, "unity"), () {
                bloc.add(CreateItemEvent(StorageOrderItemModel(
                    "a", " name", "description", 100, "unity")));
              })
            ],
          ),
          BlocBuilder<StorageOrderBloc, StorageOrderState>(
              bloc: bloc,
              builder: (context, state) {
                if (state is CreateItemState) {
                  return ItemDetailsComponent(
                    item: state.item,
                    checkButton: () {
                      bloc.add(ReturnInitialEvent());
                      close(context, state.item);
                    },
                    backButton: () {
                      bloc.add(ReturnInitialEvent());
                    },
                  );
                } else {
                  return Container();
                }
              }),
        ],
      );
    }
  }

  _item(
      BuildContext context, StorageOrderItemModel model, Function() onPressed) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(8, 8, 8, 0),
      child: ElevatedButton(
        style: ButtonStyle(
            backgroundColor:
                MaterialStateColor.resolveWith((states) => Colors.white)),
        onPressed: onPressed,
        child: ListTile(
          title: Text(
            model.name,
            overflow: TextOverflow.ellipsis,
            style: const TextStyle(fontSize: 18),
          ),
          trailing: Text(
            "Estoque: ${model.quantityInStorage.toString()} ${model.unity}",
            style: const TextStyle(color: kPrimaryColor),
          ),
        ),
      ),
    );
  }
}
