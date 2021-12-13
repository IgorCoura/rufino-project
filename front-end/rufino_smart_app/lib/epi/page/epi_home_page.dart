import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/epi/page/components/tile_component.dart';
import 'package:rufino_smart_app/utils/constants.dart';

class EpiHomePage extends StatelessWidget {
  const EpiHomePage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: kBackGroundColor,
      appBar: AppBar(
        actions: [
          IconButton(onPressed: () {}, icon: Icon(Icons.search)),
        ],
        backgroundColor: kPrimaryColor,
        title: const Text("EPI's"),
        leading: BackButton(
          onPressed: () => Modular.to.navigate('/home'),
        ),
      ),
      body: Center(
        child: SizedBox(
            width: 700,
            child: Padding(
              padding: const EdgeInsets.all(8.0),
              child: ListView(
                children: [
                  TileComponent(
                    name: "Jose Rufino",
                    days: -5,
                    onTap: () {
                      Modular.to.navigate("/epiemployee");
                    },
                  )
                ],
              ),
            )),
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {},
        backgroundColor: kPrimaryColor,
        child: Icon(Icons.download),
      ),
    );
  }
}
