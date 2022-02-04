import 'package:flutter/material.dart';
import 'package:rufino_smart_app/utils/constants.dart';

class CardComponent extends StatelessWidget {
  final Function() onTap;
  final String title;
  final IconData icon;
  const CardComponent({
    Key? key,
    required this.onTap,
    required this.title,
    required this.icon,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return GestureDetector(
      onTap: () => onTap(),
      child: Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(20),
        ),
        child: Column(
          children: [
            Expanded(
                child: Icon(
              icon,
              size: 60,
              color: kPrimaryDarkColor,
            )),
            Container(
              child: Padding(
                padding: const EdgeInsets.all(4.0),
                child: Text(
                  title,
                  style: const TextStyle(
                      fontSize: 16,
                      fontWeight: FontWeight.bold,
                      color: kPrimaryDarkColor),
                ),
              ),
            )
          ],
        ),
      ),
    );
  }
}
