import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/auth/domain/enums/login_status.dart';
import 'package:rufino/modules/auth/presentation/login/bloc/login_bloc.dart';
import 'package:rufino/shared/components/error_message_components.dart';

class LoginPage extends StatefulWidget {
  const LoginPage({super.key});

  @override
  State<LoginPage> createState() => _LoginPageState();
}

class _LoginPageState extends State<LoginPage> {
  final bloc = Modular.get<LoginBloc>();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: BlocListener<LoginBloc, LoginState>(
        bloc: bloc,
        listener: (context, state) {
          if (state.status == LoginStatus.failure) {
            ErrorMessageComponent.showSnackBar(
                context, 'Falha na autenticação.');
            bloc.add(SnackMessageWasShow());
          } else if (state.status == LoginStatus.success) {
            Modular.to.navigate("/");
          }
        },
        child: Center(
          child: SingleChildScrollView(
            child: ConstrainedBox(
              constraints: const BoxConstraints(
                  maxWidth: 450), // Limita a largura máxima
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: Column(
                  mainAxisAlignment: MainAxisAlignment.center,
                  children: [
                    const Text(
                      'Login',
                      style: TextStyle(
                        fontSize: 32,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 40),

                    // Campo de entrada para usuário
                    TextField(
                      decoration: InputDecoration(
                        labelText: 'Usuário',
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(30.0),
                        ),
                      ),
                      onChanged: (user) => bloc.add(LoginUsernameChanged(user)),
                    ),
                    const SizedBox(height: 20),

                    // Campo de entrada para senha
                    TextField(
                      obscureText: true,
                      decoration: InputDecoration(
                        labelText: 'Senha',
                        border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(30.0),
                        ),
                      ),
                      onChanged: (password) =>
                          bloc.add(LoginPasswordChanged(password)),
                    ),

                    const SizedBox(height: 20),

                    // Botão de "Esqueci a Senha"
                    Align(
                      alignment: Alignment.centerRight,
                      child: TextButton(
                        onPressed: () {
                          //TODO: Esqueci a senha
                        },
                        child: const Text('Esqueci a senha?'),
                      ),
                    ),
                    const SizedBox(height: 20),

                    // Botão de Login

                    BlocBuilder<LoginBloc, LoginState>(
                      bloc: bloc,
                      builder: (context, state) {
                        if (state.status == LoginStatus.inProgress) {
                          return const CircularProgressIndicator();
                        }
                        return SizedBox(
                          width: double
                              .infinity, // Botão ocupa toda a largura possível
                          height: 50.0, // Altura do botão
                          child: FilledButton(
                            onPressed: () {
                              bloc.add(const LoginSubmitted());
                            },
                            child: const Text(
                              'Login',
                              style: TextStyle(fontSize: 18),
                            ),
                          ),
                        );
                      },
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
