import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/stock/bloc/stock_home_bloc.dart';
import 'package:rufino_app/src/stock/components/edit_item_dialog_widget.dart';
import 'package:rufino_app/src/stock/db/dao/product_dao.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';
import 'package:rufino_app/src/stock/model/stock_order_item_model.dart';

class OrderItemSearchDelegate extends SearchDelegate {
  var productDao = Modular.get<ProductDao>();
  final StockHomeBloc bloc;
  OrderItemSearchDelegate(this.bloc);
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
    return StreamBuilder<List<Product>>(
      initialData: const [],
      stream: productDao.getFiltered(query),
      builder: (context, snapshot) {
        if (snapshot.hasData) {
          var data = snapshot.data;
          return ListView.builder(
            itemCount: data!.length,
            itemBuilder: (BuildContext context, int index) {
              return ListTile(
                onTap: () {
                  var newItem = StockOrderItemModel(
                    data[index].id,
                    data[index].name,
                    data[index].description,
                    0,
                    data[index].unity,
                  );
                  showDialog(
                      context: context,
                      builder: (BuildContext context) {
                        return EditItemDialogWidget(
                          item: newItem,
                          returnFunction: (StockOrderItemModel item) {
                            bloc.add(AddItemEvent(item));
                            close(context, null);
                          },
                        );
                      });
                },
                title: Text(
                  data[index].name,
                  overflow: TextOverflow.ellipsis,
                ),
                trailing: Text(
                  "${data[index].quantity} ${data[index].unity}",
                ),
              );
            },
          );
        }
        return Container();
      },
    );
  }
}
