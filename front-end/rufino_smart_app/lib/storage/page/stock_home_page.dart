import 'dart:html';

import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/storage/database/stock_db.dart';
import 'package:rufino_smart_app/utils/constants.dart';

class StockHomePage extends StatelessWidget {
  StockHomePage({Key? key}) : super(key: key);
  @override
  Widget build(BuildContext context) {
    return Scaffold(
        backgroundColor: kBackGroundColor,
        appBar: AppBar(
          leading: BackButton(
            onPressed: () => Modular.to.navigate('/home'),
          ),
          title: const Text("Estoque"),
          backgroundColor: kPrimaryColor,
          actions: [
            IconButton(onPressed: () {}, icon: const Icon(Icons.search)),
            IconButton(
                onPressed: () {},
                icon: const Icon(Icons.qr_code_scanner_rounded)),
          ],
        ),
        body: StreamBuilder<List<Product>>(
          stream: StockDb.instance.productDao.listAll(),
          builder: (context, snapshot) {
            if (snapshot.hasData) {
              var data = snapshot.data;
              return ListView.builder(
                itemCount: data!.length,
                itemBuilder: (BuildContext context, int index) {
                  return ListTile(
                    title: Text(data[index].name),
                  );
                },
              );
            }
            return Container();
          },
        ));
  }
}
