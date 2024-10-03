import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/employee/domain/model/name.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/bloc/bloc/employee_profile_bloc.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/form_component.dart';
import 'package:rufino/shared/components/error_message_components.dart';

class EmployeeProfilePage extends StatelessWidget {
  final _formKey = GlobalKey<FormState>();
  final bloc = Modular.get<EmployeeProfileBloc>();
  final String employeeId;

  EmployeeProfilePage({required this.employeeId, super.key}) {
    bloc.add(InitialEmployeeProfileEvent(employeeId));
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: _appBar(),
      body: SingleChildScrollView(
        child: Center(
            child: SizedBox(
          width: 1000,
          child: Padding(
            padding: const EdgeInsets.all(16.0),
            child: BlocBuilder<EmployeeProfileBloc, EmployeeProfileState>(
              bloc: bloc,
              builder: (context, state) {
                if (state.exception != null) {
                  ErrorMessageComponent.showAlertDialog(context,
                      state.exception!, () => Modular.to.navigate("/home/"));
                }

                if (state.snackMessage != null &&
                    state.snackMessage!.isNotEmpty) {
                  WidgetsBinding.instance.addPostFrameCallback((_) {
                    ScaffoldMessenger.of(context).showSnackBar(SnackBar(
                      content: Text(state.snackMessage!),
                    ));
                    bloc.add(SnackMessageWasShow());
                  });
                }

                return state.isLoading
                    ? const Center(
                        child: CircularProgressIndicator(),
                      )
                    : Column(
                        children: [
                          Text(
                            state.employee.registration,
                            style: const TextStyle(
                              fontWeight: FontWeight.bold,
                              fontSize: 24,
                            ),
                          ),
                          const SizedBox(
                            height: 16,
                          ),
                          const CircleAvatar(
                            radius: 80,
                            backgroundImage:
                                AssetImage("assets/img/avatar_default.png"),
                          ),
                          const SizedBox(
                            height: 16,
                          ),
                          Text(
                            "Status: ${state.employee.status.name}",
                            style: const TextStyle(
                              fontSize: 18,
                            ),
                          ),
                          const SizedBox(
                            height: 16,
                          ),
                          _employeeName(context, state.isEditingName,
                              state.isSavingData, state.employee.name),
                          const SizedBox(
                            height: 16,
                          ),
                          FormComponent(
                            modelBase: state.contact,
                            saveFunction: () => bloc.add(SaveContactChanges()),
                            loadingFormData: () =>
                                bloc.add(LoadingContactEvent()),
                            formName: "Contato",
                            isSavingData: state.isSavingData,
                          ),
                          const SizedBox(
                            height: 16,
                          ),
                          FormComponent(
                            modelBase: state.address,
                            saveFunction: () => bloc.add(SaveAddressEvent()),
                            loadingFormData: () =>
                                bloc.add(LoadingAddressEvent()),
                            formName: "Endereço",
                            isSavingData: state.isSavingData,
                          ),
                        ],
                      );
              },
            ),
          ),
        )),
      ),
    );
  }

  AppBar _appBar() {
    return AppBar(
      leading: IconButton(
        icon: const Icon(Icons.arrow_back),
        onPressed: () {
          Modular.to.navigate("/employee/list");
        },
      ),
      title: BlocBuilder<EmployeeProfileBloc, EmployeeProfileState>(
        bloc: bloc,
        builder: (context, state) {
          return Row(
            children: [
              const CircleAvatar(
                backgroundImage: AssetImage("assets/img/company_default.jpg"),
              ),
              const SizedBox(width: 10),
              Expanded(
                child: Text(
                  state.company != null
                      ? state.company!.fantasyName
                      : "Perfil do Funcionario",
                  overflow: TextOverflow.ellipsis,
                ),
              ),
            ],
          );
        },
      ),
    );
  }

  Widget _employeeName(
      BuildContext context, bool isEditing, bool isSavingData, Name name) {
    return Row(
      children: [
        Expanded(
          child: Form(
            key: _formKey,
            child: TextFormField(
              controller: TextEditingController(text: name.value),
              enabled: isEditing,
              style: TextStyle(
                color: Theme.of(context).colorScheme.onSurface,
              ),
              decoration: InputDecoration(
                labelText: name.displayName,
                border: const OutlineInputBorder(),
              ),
              onSaved: (newValue) {
                if (newValue != null) {
                  bloc.add(ChangeNameEvent(newValue));
                }
              },
            ),
          ),
        ),
        const SizedBox(
          width: 8,
        ),
        isSavingData
            ? const Center(child: CircularProgressIndicator())
            : isEditing
                ? FilledButton(
                    onPressed: () {
                      if (_formKey.currentState != null &&
                          _formKey.currentState!.validate()) {
                        _formKey.currentState!.save();
                        bloc.add(SaveNewNameEvent());
                      }
                    },
                    child: const Text("Salvar"),
                  )
                : TextButton(
                    onPressed: () => bloc.add(EditNameEvent()),
                    child: const Text("Editar"),
                  ),
      ],
    );
  }
}
