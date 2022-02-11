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
          IconButton(
            onPressed: () {
              var idGeneration = const Uuid().v4();
              productDao.add(Product(
                  id: idGeneration,
                  name: "Tubo de PVC 100 mm para esgoto $idGeneration",
                  description:
                      "Se o conteúdo for muito grande para caber na tela verticalmente, a caixa de diálogo exibirá o título e as ações e deixará o conteúdo transbordar, o que raramente é desejado. Considere usar um widget de rolagem para conteúdo , como SingleChildScrollView , para evitar estouro. (No entanto, esteja ciente de que, como AlertDialog tenta se dimensionar usando as dimensões intrínsecas de seus filhos, widgets como ListView , GridView e CustomScrollView , que usam viewports preguiçosas, não funcionarão. Se isso for um problema, considere usar o Dialog diretamente. )",
                  section: "section",
                  category: "category",
                  unity: "unity",
                  quantity: 10000));
            },
            icon: const Icon(
              Icons.more_vert,
              size: 26,
            ),
          ),
        ],
      ),
      body: Column(
        children: [
          Padding(
            padding: const EdgeInsets.all(8.0),
            child: Row(
              children: [
                _searchBarWidget(),
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
          Modular.to.navigate("/stock/order");
        },
        child: const Icon(Icons.shopping_cart),
      ),
    );
  }

  Widget _searchBarWidget() {
    return Expanded(
      child: Container(
        decoration: BoxDecoration(
          borderRadius: BorderRadius.circular(30),
          border: Border.all(
            color: Colors.blueGrey.shade300,
            width: 2,
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
          borderRadius: BorderRadius.circular(10),
          border: Border.all(
            color: Colors.blueGrey.shade300,
            width: 2,
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
