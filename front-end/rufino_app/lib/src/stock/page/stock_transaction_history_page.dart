import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:intl/intl.dart';
import 'package:rufino_app/src/stock/db/dao/product_transaction_dao.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';

class StockTransactionHistoryPage extends StatelessWidget {
  StockTransactionHistoryPage({Key? key}) : super(key: key);
  final productTransactionDao = Modular.get<ProductTransactionDao>();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text("Historico de transações"),
        leading: BackButton(
          onPressed: () => Modular.to.navigate('/stock'),
        ),
      ),
      body: StreamBuilder<List<ProductTransaction>>(
          stream: productTransactionDao.getAll(),
          builder: (context, snapshot) {
            if (!snapshot.hasData) {
              return Container();
            }
            var data = snapshot.data!;
            return ListView.builder(
                itemCount: data.length,
                itemBuilder: ((context, index) {
                  return ListTile(
                    leading: Text(
                      DateFormat('yMd', 'pt_Br')
                          .add_jm()
                          .format(data[index].date),
                    ),
                    title: Text(data[index].idProduct),
                    subtitle: Text(data[index].idTransactionServer ?? "NULL"),
                    trailing: Text('${data[index].quantityVariation}'),
                  );
                }));
          }),
    );
  }
}
