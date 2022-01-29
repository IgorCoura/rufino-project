import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/login/page/componentes/text_field_container.dart';
import 'package:rufino_smart_app/utils/constants.dart';

class StorageWithdrawalPage extends StatelessWidget {
  const StorageWithdrawalPage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    Size size = MediaQuery.of(context).size;
    return Scaffold(
      backgroundColor: kBackGroundColor,
      appBar: AppBar(
        backgroundColor: kPrimaryColor,
        title: const Text("Ordem de retirada"),
        leading: BackButton(
          onPressed: () => Modular.to.navigate('/storage'),
        ),
      ),
      body: ListView(
        children: [_item(size)],
      ),
    );
  }

  _item(Size size) {
    return Card(
      margin: const EdgeInsets.all(8),
      child: ListTile(
        title: const Text(
          "Tubo de pvc para esgoto 100mm",
          overflow: TextOverflow.ellipsis,
        ),
        trailing: size.width < 00
            ? null
            : Row(
                mainAxisSize: MainAxisSize.min,
                children: [
                  IconButton(
                    onPressed: () {},
                    icon: Icon(Icons.add),
                  ),
                  TextButton(
                    onPressed: () {},
                    child: const Text(
                      "100 metro",
                      style: TextStyle(color: kPrimaryColor),
                    ),
                  ),
                  IconButton(
                    onPressed: () {},
                    icon: Icon(Icons.remove),
                  ),
                ],
              ),
      ),
    );
  }
}
