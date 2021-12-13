import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/home/model/card_model.dart';
import 'package:rufino_smart_app/utils/constants.dart';

import 'components/card_component.dart';

class HomePage extends StatelessWidget {
  HomePage({Key? key}) : super(key: key);

  final List<CardModel> cards = [
    CardModel(
        id: 1,
        title: "EPI",
        icon: Icons.add_chart_sharp,
        onTap: () {
          Modular.to.navigate("/epihome");
        }),
    CardModel(id: 2, title: "DOCS", icon: Icons.add_box, onTap: () {})
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
