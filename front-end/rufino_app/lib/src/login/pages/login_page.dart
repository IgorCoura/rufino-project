import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino_app/src/login/pages/bloc/login_bloc.dart';
import 'package:rufino_app/src/login/pages/components/remember_me_and_forgot_password_field.dart';
import 'package:rufino_app/src/login/pages/components/rounded_button.dart';
import 'package:rufino_app/src/login/pages/components/rounded_input_field.dart';
import 'package:rufino_app/src/login/pages/components/rounded_password_field.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class LoginPage extends StatelessWidget {
  final TextEditingController _cpfController = TextEditingController();
  final TextEditingController _passwordController = TextEditingController();

  LoginPage({Key? key}) : super(key: key) {
    Modular.get<LoginBloc>().add(LoginVerifiedSessionEvent());
  }

  @override
  Widget build(BuildContext context) {
    Size size = MediaQuery.of(context).size;
    return Scaffold(
      body: SizedBox(
        width: double.infinity,
        height: size.height,
        child: Stack(
          alignment: Alignment.center,
          children: [
            SingleChildScrollView(
                child: Center(
                    child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Text(
                  "LOGIN",
                  style: TextStyle(
                    fontWeight: FontWeight.bold,
                    fontSize: 24,
                  ),
                ),
                SizedBox(height: size.height * 0.03),
                RoundedInputField(
                  hintText: "CPF",
                  controller: _cpfController,
                ),
                RoundedPasswordField(
                  controller: _passwordController,
                ),
                BlocBuilder<LoginBloc, LoginState>(
                  bloc: Modular.get<LoginBloc>(),
                  builder: (context, state) {
                    if (state is LoginErrorState) {
                      return Text(
                        state.message,
                        style: const TextStyle(color: Colors.red),
                      );
                    }
                    return Container();
                  },
                ),
                RoundedButton(
                    bloc: Modular.get<LoginBloc>(),
                    text: "LOGIN",
                    press: () {
                      Modular.get<LoginBloc>().add(LoginSendEvent(
                          _cpfController.text, _passwordController.text));
                    }),
                RememberMeAndForgotPasswordField(
                  press: () {
                    Modular.to.navigate('/forgotpassword');
                  },
                  checkBox: () {
                    Modular.get<LoginBloc>().add(LoginSelectRememberMeEvent());
                  },
                ),
                SizedBox(height: size.height * 0.15),
              ],
            ))),
          ],
        ),
      ),
    );
  }
}
