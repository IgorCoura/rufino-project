import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/home/components/card_component.dart';
import 'package:rufino_app/src/home/model/card_model.dart';

class HomePage extends StatelessWidget {
  HomePage({Key? key}) : super(key: key);

  final List<CardModel> cards = [
    CardModel(
        id: 1,
        title: "EPI",
        icon: Icons.add_chart_sharp,
        onTap: () {
          Modular.to.navigate("/");
        }),
    CardModel(id: 2, title: "DOCS", icon: Icons.add_box, onTap: () {}),
    CardModel(
        id: 3,
        title: "Estoque",
        icon: Icons.add_shopping_cart,
        onTap: () {
          Modular.to.navigate("/stock/");
        })
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text(
          "Home",
          style: TextStyle(fontWeight: FontWeight.bold, fontSize: 24),
        ),
      ),
      body: Padding(
        padding: const EdgeInsets.all(8.0),
        child: GridView.builder(
            gridDelegate: const SliverGridDelegateWithMaxCrossAxisExtent(
                maxCrossAxisExtent: 180,
                childAspectRatio: 0.95,
                crossAxisSpacing: 8,
                mainAxisSpacing: 8),
            itemCount: cards.length,
            itemBuilder: (BuildContext ctx, index) {
              return CardComponent(
                icon: cards[index].icon,
                title: cards[index].title,
                onTap: cards[index].onTap,
              );
            }),
      ),
    );
  }
}
