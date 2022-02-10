import 'package:flutter/material.dart';

class CardModel {
  int id;
  String title;
  IconData icon;
  Function() onTap;

  CardModel(
      {required this.id,
      required this.title,
      required this.icon,
      required this.onTap});
}
