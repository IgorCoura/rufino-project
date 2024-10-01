import 'package:flutter/material.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

class ErrorMessageComponent {
  static void showAlertDialog(
      BuildContext context, AplicationException exception, Function onPressed) {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      showDialog(
          barrierDismissible: false,
          context: context,
          builder: (_) {
            return _dialogAlertUniqueMessage(context, exception, onPressed);
          });
    });
  }

  static void showSnackBar(BuildContext context, String message) {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(
        content: Text(message),
      ));
    });
  }

  static Widget _dialogAlertUniqueMessage(
      BuildContext context, AplicationException error, Function onPressed) {
    return AlertDialog(
      title: Text("Error: ${error.code}"),
      content: Text(error.message),
      actions: [
        TextButton(onPressed: () => onPressed(), child: const Text("OK"))
      ],
    );
  }
}
