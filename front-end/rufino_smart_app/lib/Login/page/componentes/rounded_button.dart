import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_smart_app/login/bloc/login_bloc.dart';
import 'package:rufino_smart_app/login/bloc/login_state.dart';
import 'package:rufino_smart_app/utils/constants.dart';

class RoundedButton extends StatelessWidget {
  final String text;
  final Function() press;
  final Bloc bloc;
  final Color color, textColor;
  RoundedButton(
      {Key? key,
      required this.text,
      required this.press,
      required this.bloc,
      this.color = kPrimaryColor,
      this.textColor = kTextPrimaryColor})
      : super(key: key);

  @override
  Widget build(BuildContext context) {
    Size size = MediaQuery.of(context).size;
    return BlocBuilder(
        bloc: bloc,
        builder: (context, state) {
          if (state is LoginLoadingState) {
            return const CircularProgressIndicator(
              color: kPrimaryColor,
            );
          }
          return Container(
            margin: const EdgeInsets.symmetric(vertical: 10),
            width: size.width <= 600 ? size.width * 0.4 : 250,
            child: ClipRRect(
              borderRadius: BorderRadius.circular(29),
              child: ElevatedButton(
                onPressed: () {
                  press();
                },
                child: size.width > 310
                    ? Text(
                        "LOGIN",
                        style: TextStyle(color: textColor),
                      )
                    : null,
                style: ElevatedButton.styleFrom(
                    primary: color,
                    padding: const EdgeInsets.symmetric(
                        horizontal: 40, vertical: 20),
                    textStyle: TextStyle(
                        color: textColor,
                        fontSize: 14,
                        fontWeight: FontWeight.w500)),
              ),
            ),
          );
        });
  }
}
