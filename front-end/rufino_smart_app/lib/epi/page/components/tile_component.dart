import 'package:flutter/material.dart';

class TileComponent extends StatelessWidget {
  final ImageProvider<Object> img;
  final String name;
  final int days;
  final Function() onTap;
  const TileComponent({
    Key? key,
    required this.name,
    required this.days,
    required this.onTap,
    this.img = const AssetImage("img/default_profile_picture.png"),
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        onTap: onTap,
        minVerticalPadding: 16,
        leading: _image(),
        title: Text(
          name,
          overflow: TextOverflow.ellipsis,
          style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
        ),
        trailing: days <= 0
            ? const Text("Vencido",
                style: TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                    color: Colors.red))
            : Text("$days",
                style:
                    const TextStyle(fontSize: 18, fontWeight: FontWeight.bold)),
      ),
    );
  }

  Widget _image() {
    return ClipRRect(
      borderRadius: BorderRadius.circular(30),
      child: Image(fit: BoxFit.contain, image: img),
    );
  }
}
