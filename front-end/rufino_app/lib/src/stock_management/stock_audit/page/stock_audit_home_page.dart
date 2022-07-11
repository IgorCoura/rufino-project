import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:path/path.dart';
import 'package:rufino_app/src/stock_management/stock_audit/models/product_model.dart';
import 'package:rufino_app/src/stock_management/stock_audit/repository/product_repository.dart';

class StockAuditHomePage extends StatelessWidget {
  StockAuditHomePage({Key? key}) : super(key: key);
  final productRepository = Modular.get<ProductRepository>();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text("Auditoria de estoque"),
        leading: BackButton(
          onPressed: () => Modular.to.navigate('/stockManagement/'),
        ),
      ),
      body: FutureBuilder<List<ProductModel>>(
        future: productRepository.getAllProducts(),
        builder: (context, snapshot) {
          if (snapshot.hasData) {
            var data = snapshot.data!;
            return Column(
              children: [
                ListTile(
                  leading: Container(
                      alignment: Alignment.center,
                      height: 40,
                      width: 40,
                      child: const Text("Check")),
                  title: const Text("Name"),
                  trailing: const Text("Exp/Real"),
                ),
                Expanded(
                  child: ListView.builder(
                      itemCount: data.length,
                      itemBuilder: ((context, index) {
                        return ListTile(
                          title: Text(data[index].name),
                          leading: index % 2 == 0
                              ? Icon(
                                  Icons.check,
                                  color: Colors.green,
                                )
                              : Icon(
                                  Icons.clear,
                                  color: Colors.red,
                                ),
                          trailing: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              Text(
                                  "${data[index].quantity} / 15 ${data[index].unity}"),
                            ],
                          ),
                        );
                      })),
                ),
              ],
            );
          }
          if (snapshot.hasError) {
            var erro = snapshot.error;
            return AlertDialog(
              title: const Text("Erro"),
              content: Text(erro.toString()),
            );
          }
          return const Center(
            child: CircularProgressIndicator(),
          );
        },
      ),
    );
  }
}
