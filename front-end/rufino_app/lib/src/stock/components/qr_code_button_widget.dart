import 'package:flutter/material.dart';

class QrCodeButtonWidget extends StatelessWidget {
  final Function() function;
  const QrCodeButtonWidget({Key? key, required this.function})
      : super(key: key);

  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(
        color: Theme.of(context).backgroundColor,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(
          color: Theme.of(context).primaryColor,
          width: 1,
        ),
      ),
      child: IconButton(
        onPressed: function,
        icon: const Icon(
          Icons.qr_code_scanner_outlined,
          size: 26,
        ),
      ),
    );
  }
}
