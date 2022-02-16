import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';

class StockDevolucionPage extends StatelessWidget {
  const StockDevolucionPage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text("Devolução"),
        leading: BackButton(
          onPressed: () => Modular.to.navigate("/stock"),
        ),
      ),
    );
  }
}
