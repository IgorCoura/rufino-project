import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/domain/enum/auth_status.dart';
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
          case AuthStatus.unSelectedCompany:
            Modular.to.navigate('/company-selection');
          case AuthStatus.authenticated:
            Modular.to.navigate('/home/');
          case AuthStatus.unauthenticated:
            Modular.to.navigate('/login');
          case AuthStatus.failure:
            ScaffoldMessenger.of(context)
              ..hideCurrentSnackBar()
              ..showMaterialBanner(MaterialBanner(
                content: Text(
                    "Error: ${state.exception!.code} - ${state.exception!.message}"),
                actions: [],
              ));
          case AuthStatus.unknown:
            break;
        }
      },
      child: const Center(child: CircularProgressIndicator()),
    ));
  }
}
