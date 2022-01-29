import 'package:flutter/cupertino.dart';
import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/utils/constants.dart';

class StorageHomePage extends StatelessWidget {
  const StorageHomePage({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    Size size = MediaQuery.of(context).size;
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
      body: Column(
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              _cardOptions(size, "Retirada", Icons.download,
                  () => Modular.to.navigate("/storage/withdrawal")),
              _cardOptions(size, "Devolução", Icons.upload, () {}),
              _cardOptions(size, "Estoque", Icons.add_shopping_cart, () {}),
              _cardOptions(size, "Ordens", Icons.paste, () {}),
            ],
          ),
          const Padding(
            padding: EdgeInsets.all(8.0),
            child: Text(
              "Ultimas ordens",
              style: TextStyle(fontSize: 18, fontWeight: FontWeight.w600),
            ),
          ),
          Expanded(
            child: ListView(
              children: [
                _ordersModel("Jose Marinho Augusto", "51467", () {}),
              ],
            ),
          ),
        ],
      ),
    );
  }

  _ordersModel(String title, String trailing, Function() func) {
    return Card(
      elevation: 3,
      child: ListTile(
        onTap: func,
        title: Text(
          title,
          style: const TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.w600,
            overflow: TextOverflow.ellipsis,
          ),
        ),
        trailing: Text(
          trailing,
          style: const TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.w600,
          ),
        ),
      ),
    );
  }

  _cardOptions(Size size, String text, IconData icon, Function() func) {
    return SizedBox(
        height: size.width >= 500 ? 500 * 0.25 : size.width * 0.25,
        width: size.width >= 500 ? 500 * 0.25 : size.width * 0.25,
        child: Padding(
          padding: const EdgeInsets.all(8.0),
          child: ElevatedButton(
            onPressed: func,
            style: ButtonStyle(
              shape: MaterialStateProperty.all<RoundedRectangleBorder>(
                RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(18.0),
                ),
              ),
              backgroundColor:
                  MaterialStateColor.resolveWith((states) => Colors.white),
            ),
            child: Column(
              children: [
                Expanded(
                  child: Icon(
                    icon,
                    color: kPrimaryColor,
                    size: size.width >= 500 ? 500 * 0.10 : size.width * 0.10,
                  ),
                ),
                Text(
                  text,
                  style: TextStyle(
                    color: kPrimaryColor,
                    fontWeight: FontWeight.bold,
                    fontSize:
                        size.width >= 500 ? 500 * 0.03 : size.width * 0.03,
                    overflow: TextOverflow.ellipsis,
                  ),
                )
              ],
            ),
          ),
        ));
  }
}
