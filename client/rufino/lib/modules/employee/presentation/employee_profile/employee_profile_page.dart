import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/employee/domain/model/name.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/bloc/employee_profile_bloc.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/enumeration_list_view_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/enumeration_view_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/props_container_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/text_edit_component.dart';
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
                          const SizedBox(
                            height: 16,
                          ),
                          PropsContainerComponent(
                            containerName: "Contatos Container",
                            isSavingData: state.isSavingData,
                            loadingContainerData: () =>
                                bloc.add(LoadingContactEvent()),
                            saveContainerData: (changes) =>
                                bloc.add(SaveContactChanges()),
                            children: state.contact.props
                                .map((prop) => TextEditComponent(
                                      textProp: prop,
                                      isLoading: state.contact.isLoading,
                                    ))
                                .toList(),
                          ),
                          const SizedBox(
                            height: 16,
                          ),
                          PropsContainerComponent(
                            containerName: "Informações Pessoais",
                            isSavingData: state.isSavingData,
                            loadingContainerData: () =>
                                bloc.add(LoadingPersonalInfoEvent()),
                            saveContainerData: (changes) =>
                                bloc.add(SavePersonalInfoEvent(changes)),
                            loadingLazyContainerData: () =>
                                bloc.add(LazyLoadingPersonalInfoEvent()),
                            isLoading: state.personalInfo.isLoading,
                            children: [
                              EnumerationListViewComponent(
                                enumerationsList: state.personalInfo.deficiency,
                                enumerationOptionsList:
                                    state.optionsDisability ?? [],
                              ),
                              TextEditComponent(
                                textProp:
                                    state.personalInfo.deficiency.observation,
                              ),
                              EnumerationViewComponent(
                                  enumeration: state.personalInfo.maritalStatus,
                                  listEnumerationOptions:
                                      state.optionsMaritalStatus ?? [])
                            ],
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
                ? Row(
                    children: [
                      TextButton(
                          onPressed: () => bloc.add(const EditNameEvent(false)),
                          child: const Text("Cancelar")),
                      FilledButton(
                        onPressed: () {
                          if (_formKey.currentState != null &&
                              _formKey.currentState!.validate()) {
                            _formKey.currentState!.save();
                            bloc.add(SaveNewNameEvent());
                          }
                        },
                        child: const Text("Salvar"),
                      )
                    ],
                  )
                : TextButton(
                    onPressed: () => bloc.add(const EditNameEvent(true)),
                    child: const Text("Editar"),
                  ),
      ],
    );
  }
}
