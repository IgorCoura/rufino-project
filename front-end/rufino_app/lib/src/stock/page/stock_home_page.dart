import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/stock/bloc/stock_home_bloc.dart';
import 'package:rufino_app/src/stock/components/edit_item_dialog_component.dart';
import 'package:rufino_app/src/stock/db/dao/product_dao.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';
import 'package:rufino_app/src/stock/model/stock_order_item_model.dart';
import 'package:uuid/uuid.dart';

class StockHomePage extends StatelessWidget {
  final searchController = TextEditingController();
  final bloc = Modular.get<StockHomeBloc>();
  final productDao = Modular.get<ProductDao>();
  StockHomePage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: BackButton(
          onPressed: () => Modular.to.navigate('/home'),
        ),
        title: const Text("Estoque"),
        actions: [
          PopupMenuButton(
            color: Theme.of(context).backgroundColor,
            onSelected: (handleClick) {
              switch (handleClick) {
                case 0:
                  Modular.to.navigate("/stock/devolucion");
                  break;
                case 1:
                  Modular.to.navigate('/stock/history');
                  break;
              }
            },
            itemBuilder: (BuildContext context) {
              var listString = ['Devolução', 'Historico', 'Settings', 'teste '];
              List<PopupMenuEntry> list = [];
              for (int i = 0; i < listString.length; i++) {
                list.add(
                  PopupMenuItem(
                    child: Text(listString[i]),
                    value: i,
                  ),
                );
                if (i >= listString.length - 1) {
                  continue;
                }
                list.add(
                  const PopupMenuDivider(
                    height: 10,
                  ),
                );
              }

              return list;
            },
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(8.0),
            child: Row(
              children: [
                _searchBarWidget(context),
                _qrCodeButtonWidget(context),
              ],
            ),
          ),
          Expanded(
            child: BlocBuilder<StockHomeBloc, StockHomeState>(
              bloc: bloc,
              builder: (context, state) {
                return StreamBuilder<List<Product>>(
                  initialData: const [],
                  stream: productDao.getFiltered(state.search),
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
                                    return EditItemDialogComponent(
                                      item: newItem,
                                      returnFunction:
                                          (StockOrderItemModel item) {
                                        bloc.add(AddItemEvent(item));
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
              },
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {
          Modular.to.navigate("/stock/withdraw");
        },
        child: const Icon(Icons.shopping_cart),
      ),
    );
  }

  Widget _searchBarWidget(BuildContext context) {
    return Expanded(
      child: Container(
        decoration: BoxDecoration(
          color: Theme.of(context).backgroundColor,
          borderRadius: BorderRadius.circular(30),
          border: Border.all(
            color: Theme.of(context).primaryColor,
            width: 3,
          ),
        ),
        child: Row(
          children: [
            const Padding(
              padding: EdgeInsets.symmetric(horizontal: 16),
              child: Icon(Icons.search),
            ),
            Expanded(
              child: TextField(
                controller: searchController,
                onChanged: (value) {
                  bloc.add(SearchEvent(value));
                },
                decoration: const InputDecoration(
                    hintText: 'Search', border: InputBorder.none),
                // onChanged: onSearchTextChanged,
              ),
            ),
            Padding(
              padding: const EdgeInsets.symmetric(horizontal: 8),
              child: IconButton(
                icon: const Icon(Icons.cancel),
                onPressed: () {
                  searchController.text = "";
                  bloc.add(const SearchEvent(""));
                },
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _qrCodeButtonWidget(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.all(8.0),
      child: Container(
        decoration: BoxDecoration(
          color: Theme.of(context).backgroundColor,
          borderRadius: BorderRadius.circular(10),
          border: Border.all(
            color: Theme.of(context).primaryColor,
            width: 3,
          ),
        ),
        child: IconButton(
          onPressed: () {},
          icon: const Icon(
            Icons.qr_code_scanner_outlined,
            size: 26,
          ),
        ),
      ),
    );
  }
}
