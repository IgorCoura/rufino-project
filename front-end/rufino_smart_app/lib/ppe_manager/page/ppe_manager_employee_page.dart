import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/ppe_manager/model/worker_model.dart';
import 'package:rufino_smart_app/utils/constants.dart';

class PpeManagerEmployeePage extends StatelessWidget {
  final int workerId;

  const PpeManagerEmployeePage({Key? key, required this.workerId})
      : super(key: key);

  @override
  Widget build(BuildContext context) {
    Size size = MediaQuery.of(context).size;
    return Scaffold(
      backgroundColor: kBackGroundColor,
      appBar: AppBar(
        actions: [
          Padding(
            padding: const EdgeInsets.all(8.0),
            child: IconButton(
                onPressed: () {}, icon: const Icon(Icons.article_outlined)),
          )
        ],
        backgroundColor: kPrimaryColor,
        title: Text("Ficha de Registro de Equipamentos de Proteção Individual"),
        leading: BackButton(
          onPressed: () => Modular.to.navigate('/ppemanagerhome'),
        ),
      ),
      body: Column(
        children: [
          _info(context),
          Expanded(
            child: size.width < 200
                ? Container()
                : ListView(
                    children: [
                      _equipment(),
                      _equipment(),
                    ],
                  ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        onPressed: () {},
        backgroundColor: kPrimaryColor,
        child: Icon(Icons.download),
      ),
    );
  }

  Widget _info(context) {
    Size size = MediaQuery.of(context).size;
    return Padding(
      padding: EdgeInsets.all(12),
      child: Row(
        children: [
          SizedBox(
              width: size.width < 750
                  ? size.width < 280
                      ? 60
                      : size.width * 0.2
                  : 150,
              child: ClipRRect(
                borderRadius: BorderRadius.circular(180),
                child: const Image(
                    fit: BoxFit.contain,
                    image: AssetImage("img/default_profile_picture.png")),
              )),
          SizedBox(
            width: size.width * 0.05,
          ),
          Flexible(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  "Nome: Jose Rufino Coura",
                  style: TextStyle(fontSize: 22),
                  overflow: TextOverflow.ellipsis,
                ),
                Text(
                  "Função: Encarregado de Elétrica",
                  style: TextStyle(fontSize: 16),
                  overflow: TextOverflow.ellipsis,
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _foot(context) {
    Size size = MediaQuery.of(context).size;
    return Container(
        width: double.infinity,
        color: kPrimaryColor,
        child: Padding(
          padding: const EdgeInsets.all(8.0),
          child: Row(
            children: [
              ElevatedButton(
                onPressed: () {},
                style: ButtonStyle(
                  backgroundColor: MaterialStateColor.resolveWith((states) {
                    return Colors.white;
                  }),
                ),
                child: const Text(
                  "GERAR PDF",
                  style: TextStyle(color: kPrimaryColor),
                ),
              ),
              ElevatedButton(
                onPressed: () {},
                style: ButtonStyle(
                  backgroundColor: MaterialStateColor.resolveWith((states) {
                    return Colors.white;
                  }),
                ),
                child: const Text(
                  "Download Fichas",
                  style: TextStyle(color: kPrimaryColor),
                ),
              )
            ],
          ),
        ));
  }

  Widget _equipment() {
    return Card(
      child: ListTile(
        leading: Checkbox(
          value: true,
          onChanged: (value) {},
          activeColor: kPrimaryColor,
        ),
        title: Text(
          "Óculos de Proteção incolor",
          overflow: TextOverflow.ellipsis,
        ),
        subtitle: Text(
          "CA: 12345",
          overflow: TextOverflow.ellipsis,
        ),
        trailing: Text(
          "22/05/2020",
        ),
      ),
    );
  }
}
