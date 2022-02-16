import 'package:dropdown_search/dropdown_search.dart';
import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/stock/bloc/stock_home_bloc.dart';
import 'package:rufino_app/src/stock/db/dao/worker_dao.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';
import 'package:uuid/uuid.dart';

class FinishedOrderDialog extends StatelessWidget {
  FinishedOrderDialog({Key? key}) : super(key: key);
  var idResposible = const Uuid().v4();
  var idTaker = "";
  var bloc = Modular.get<StockHomeBloc>();
  var workerDao = Modular.get<WorkerDao>();

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      title: Text(
        "Finalizar ordem de retirada",
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
                    "Igor de Brito Coura",
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
              DropdownSearch<Worker>(
                mode: Mode.BOTTOM_SHEET,
                showSearchBox: true,
                onFind: (String? filter) => workerDao.getAll(),
                itemAsString: (Worker? w) => w == null ? "" : w.name,
                onChanged: (worker) {
                  idTaker = worker == null ? "" : worker.id;
                },
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
                                        idTaker, idResposible, true));
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
