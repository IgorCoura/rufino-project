import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';

class StockOrderPage extends StatelessWidget {
  const StockOrderPage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: BackButton(
          onPressed: () => Modular.to.navigate('/stock'),
        ),
        title: const Text("Ordem de retirada"),
        actions: [
          IconButton(
            onPressed: () {},
            icon: const Icon(
              Icons.more_vert,
              size: 26,
            ),
          ),
        ],
      ),
      body: ListView(
        children: [
          _itemTileWidget("Tubo de concreto"),
          _itemTileWidget("Tubo de concreto"),
          _buttonsWidgt(context),
        ],
      ),
    );
  }

  Widget _itemTileWidget(String title) {
    return ListTile(
      leading: const Icon(
        Icons.download,
        color: Colors.red,
      ),
      title: TextButton(
        style: const ButtonStyle(alignment: Alignment.bottomLeft),
        onPressed: () {},
        child: Text(
          title,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(fontSize: 18),
        ),
      ),
      trailing: Row(mainAxisSize: MainAxisSize.min, children: [
        IconButton(
          onPressed: () {},
          icon: const Icon(Icons.add),
        ),
        const Text("1000 metros"),
        IconButton(
          onPressed: () {},
          icon: const Icon(Icons.remove),
        ),
        IconButton(
            onPressed: () {},
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
              onPressed: () {},
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
                          TextButton(
                            onPressed: () => Navigator.pop(context, 'Cancel'),
                            child: const Text(
                              'Cancel',
                              style: TextStyle(fontSize: 24),
                            ),
                          ),
                          TextButton(
                            onPressed: () => Navigator.pop(context, 'OK'),
                            child: const Text(
                              'OK',
                              style: TextStyle(fontSize: 24),
                            ),
                          ),
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
}
