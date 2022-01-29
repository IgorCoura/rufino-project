import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/components/card_component.dart';
import 'package:rufino_smart_app/home/model/card_model.dart';
import 'package:rufino_smart_app/utils/constants.dart';

class HomePage extends StatelessWidget {
  HomePage({Key? key}) : super(key: key);

  final List<CardModel> cards = [
    CardModel(
        id: 1,
        title: "EPI",
        icon: Icons.add_chart_sharp,
        onTap: () {
          Modular.to.navigate("/ppemanagerhome");
        }),
    CardModel(id: 2, title: "DOCS", icon: Icons.add_box, onTap: () {}),
    CardModel(
        id: 3,
        title: "Estoque",
        icon: Icons.add_shopping_cart,
        onTap: () {
          Modular.to.navigate("/storage");
        })
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: kBackGroundColor,
      appBar: AppBar(
        title: const Text(
          "Home",
          style: TextStyle(
              color: kTextPrimaryColor,
              fontWeight: FontWeight.bold,
              fontSize: 24),
        ),
        backgroundColor: kPrimaryColor,
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
