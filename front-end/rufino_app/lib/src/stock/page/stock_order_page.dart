import 'package:dropdown_search/dropdown_search.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/stock/bloc/stock_home_bloc.dart';
import 'package:rufino_app/src/stock/components/edit_item_dialog_widget.dart';
import 'package:rufino_app/src/stock/components/qr_code_button_widget.dart';
import 'package:rufino_app/src/stock/db/dao/worker_dao.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';
import 'package:rufino_app/src/stock/delegates/order_item_search_delegate.dart';
import 'package:rufino_app/src/stock/model/stock_order_item_model.dart';

class StockOrderPage extends StatelessWidget {
  final TextEditingController controller = TextEditingController();
  final bool withdraw;
  final String title;
  late final StockHomeBloc bloc;
  StockOrderPage({
    Key? key,
    this.withdraw = false,
    required this.title,
    StockHomeBloc? bloc,
  }) : super(key: key) {
    this.bloc = bloc ?? Modular.get<StockHomeBloc>();
  }

  final workerDao = Modular.get<WorkerDao>();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
        appBar: AppBar(
          leading: BackButton(
            onPressed: () => Modular.to.navigate('/stock'),
          ),
          title: Text(title),
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
                      style:
                          TextStyle(fontWeight: FontWeight.bold, fontSize: 24),
                    ),
                  ),
                );
              },
            );
          },
        ),
        floatingActionButton: withdraw
            ? Container()
            : FloatingActionButton(
                onPressed: () {
                  showSearch(
                      context: context,
                      delegate: OrderItemSearchDelegate(bloc));
                },
                child: const Icon(Icons.add),
              ));
  }

  Widget _itemTileWidget(
      BuildContext context, int index, StockOrderItemModel model) {
    return ListTile(
      leading: withdraw
          ? const Icon(
              Icons.download,
              color: Colors.red,
            )
          : const Icon(
              Icons.upload,
              color: Colors.green,
            ),
      title: TextButton(
        style: const ButtonStyle(alignment: Alignment.bottomLeft),
        onPressed: () {
          showDialog(
              context: context,
              builder: (context) {
                return EditItemDialogWidget(
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
            padding: const EdgeInsets.only(left: 16),
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
              onPressed: () => showDialog(
                    context: context,
                    builder: (context) {
                      return _finishedOrderDialog(context);
                    },
                  ),
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

  Widget _finishedOrderDialog(BuildContext context) {
    var idResposible = "6eec8ea4-477a-4dbd-ae84-134b38baabee";
    var idTaker = "";
    return AlertDialog(
      title: Text(
        "Finalizar ordem",
        style: Theme.of(context).textTheme.titleLarge,
      ),
      actions: [
        Padding(
          padding: const EdgeInsets.all(16.0),
          child: Column(
            children: [
              Container(
                padding: const EdgeInsets.symmetric(vertical: 4),
                width: double.infinity,
                child: Text(
                  "Reponsavel:",
                  style: Theme.of(context).textTheme.titleMedium,
                ),
              ),
              Container(
                width: double.infinity,
                height: 60,
                alignment: Alignment.centerLeft,
                decoration: BoxDecoration(
                  border: Border.all(
                    width: 1,
                    color: Theme.of(context).primaryColor,
                  ),
                  borderRadius: BorderRadius.circular(5),
                ),
                child: Padding(
                  padding: const EdgeInsets.all(8.0),
                  child: Text(
                    "DEFAULT NAME",
                    style: Theme.of(context).textTheme.bodyLarge,
                  ),
                ),
              ),
              const SizedBox(
                height: 30,
              ),
              Container(
                padding: const EdgeInsets.symmetric(vertical: 4),
                width: double.infinity,
                child: Text(
                  "Tomador:",
                  style: Theme.of(context).textTheme.titleMedium,
                ),
              ),
              Row(
                children: [
                  Expanded(
                    child: DropdownSearch<Worker>(
                      mode: Mode.BOTTOM_SHEET,
                      showSearchBox: true,
                      onFind: (String? filter) => workerDao.getAll(),
                      itemAsString: (Worker? w) => w == null ? "" : w.name,
                      onChanged: (worker) {
                        idTaker = worker == null ? "" : worker.id;
                      },
                    ),
                  ),
                  Padding(
                    padding: const EdgeInsets.only(left: 8),
                    child: QrCodeButtonWidget(function: () {}),
                  ),
                ],
              ),
              Padding(
                padding: const EdgeInsets.only(top: 32.0),
                child: BlocBuilder<StockHomeBloc, StockHomeState>(
                  bloc: bloc,
                  builder: (context, state) {
                    if (state is LoadOrderState) {
                      return const SizedBox(
                        height: 50,
                        width: 50,
                        child: CircularProgressIndicator(),
                      );
                    }

                    if (state is FinishedOrderState) {
                      Navigator.pop(context);
                      return Container();
                    }

                    return Row(
                      children: [
                        Expanded(
                            child: Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 8.0),
                          child: SizedBox(
                            height: 50,
                            child: ElevatedButton(
                                child: const Text("FINALIZAR"),
                                onPressed: () {
                                  if (idTaker != "" && idResposible != "") {
                                    bloc.add(FinishedStockOrderEvent(
                                        idTaker, idResposible, withdraw));
                                  }
                                }),
                          ),
                        )),
                        Expanded(
                            child: Padding(
                          padding: const EdgeInsets.symmetric(horizontal: 8.0),
                          child: SizedBox(
                            height: 50,
                            child: ElevatedButton(
                                child: const Text("CANCELAR"),
                                onPressed: () {
                                  Navigator.pop(context);
                                }),
                          ),
                        )),
                      ],
                    );
                  },
                ),
              ),
            ],
          ),
        )
      ],
    );
  }
}
