import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';

class StockManagementHomePage extends StatelessWidget {
  const StockManagementHomePage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text("Gerenciamento de estoque"),
        leading: BackButton(
          onPressed: () => Modular.to.navigate('/home'),
        ),
      ),
      body: ListView(children: [
        Padding(
          padding: const EdgeInsets.all(8.0),
          child: ElevatedButton(
            onPressed: () => Modular.to.navigate("/stockManagement/audit/"),
            child: Padding(
              padding: const EdgeInsets.all(16.0),
              child: Text(
                "Auditoria de estoque",
                style: Theme.of(context).textTheme.titleLarge,
              ),
            ),
          ),
        )
      ]),
    );
  }
}
