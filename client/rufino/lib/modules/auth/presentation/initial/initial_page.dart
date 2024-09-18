import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/auth/domain/enums/authentication_status.dart';
import 'package:rufino/modules/auth/presentation/initial/bloc/authentication_bloc.dart';
import 'package:flutter_bloc/flutter_bloc.dart';

class InitialPage extends StatefulWidget {
  const InitialPage({super.key});

  @override
  State<InitialPage> createState() => _InitialPageState();
}

class _InitialPageState extends State<InitialPage> {
  final bloc = Modular.get<AuthenticationBloc>();
  @override
  void initState() {
    bloc.add(AuthenticationSubscriptionRequested());
    super.initState();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
        body: BlocListener<AuthenticationBloc, AuthenticationState>(
      bloc: bloc,
      listener: (context, state) {
        switch (state.status) {
          case AuthenticationStatus.authenticated:
            Modular.to.navigate('/home');
          case AuthenticationStatus.unauthenticated:
            Modular.to.navigate('/login');
          case AuthenticationStatus.unknown:
            break;
        }
      },
      child: const Center(child: CircularProgressIndicator()),
    ));
  }
}
