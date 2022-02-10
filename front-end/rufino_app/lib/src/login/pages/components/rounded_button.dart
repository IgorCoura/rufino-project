import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:rufino_app/src/login/pages/bloc/login_bloc.dart';

class RoundedButton extends StatelessWidget {
  final String text;
  final Function() press;
  final Bloc bloc;
  RoundedButton({
    Key? key,
    required this.text,
    required this.press,
    required this.bloc,
  }) : super(key: key);

  @override
  Widget build(BuildContext context) {
    Size size = MediaQuery.of(context).size;
    return BlocBuilder(
        bloc: bloc,
        builder: (context, state) {
          if (state is LoginLoadingState) {
            return const CircularProgressIndicator();
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
                    ? const Text(
                        "LOGIN",
                      )
                    : null,
                style: ElevatedButton.styleFrom(
                    padding: const EdgeInsets.symmetric(
                        horizontal: 40, vertical: 20),
                    textStyle: const TextStyle(
                        fontSize: 14, fontWeight: FontWeight.w500)),
              ),
            ),
          );
        });
  }
}
