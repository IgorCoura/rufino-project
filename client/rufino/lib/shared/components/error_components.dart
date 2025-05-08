import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/shared/errors/aplication_errors.dart';

class ErrorComponent {
  static void showException(BuildContext context, AplicationException exception,
      Function defaultCallBack) {
    if (exception.callBackPage.isNotEmpty) {
      showAlertDialog(context, exception,
          () => Modular.to.navigate(exception.callBackPage));
    } else {
      showAlertDialog(context, exception, () => defaultCallBack());
    }
  }

  static void showAlertDialog(
      BuildContext context, AplicationException exception, Function onPressed) {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      showDialog(
          barrierDismissible: false,
          useRootNavigator: false,
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
        TextButton(
            onPressed: () {
              Navigator.of(context).pop();
              onPressed();
            },
            child: const Text("OK"))
      ],
    );
  }
}
