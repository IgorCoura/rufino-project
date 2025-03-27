import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/domain/model/company.dart';
import 'package:rufino/modules/employee/domain/model/name.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/disability.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/bloc/employee_profile_bloc.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/base_edit_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/contracts_view_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/dependents_list_component/dependents_list_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/enumeration_list_view_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/enumeration_view_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/props_container_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/text_componet.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/text_edit_component.dart';
import 'package:rufino/shared/components/error_components.dart';

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
                  ErrorComponent.showException(context, state.exception!,
                      () => Modular.to.navigate("/home"));
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
                          PropsContainerComponent(
                            containerName: "Contatos",
                            isSavingData: state.isSavingData,
                            loadingContainerData: () =>
                                bloc.add(LoadingContactEvent()),
                            saveContainerData: (changes) =>
                                bloc.add(SaveContactChanges(changes)),
                            isLoading: state.contact.isLoading,
                            isLazyLoading: state.contact.isLazyLoading,
                            children: state.contact.textProps
                                .map(
                                    (prop) => TextEditComponent(textProp: prop))
                                .toList(),
                          ),
                          const SizedBox(
                            height: 16,
                          ),
                          PropsContainerComponent(
                            containerName: "Endereço",
                            isSavingData: state.isSavingData,
                            isLoading: state.address.isLoading,
                            isLazyLoading: state.address.isLazyLoading,
                            loadingContainerData: () =>
                                bloc.add(LoadingAddressEvent()),
                            saveContainerData: (changes) =>
                                bloc.add(SaveAddressEvent(changes)),
                            children: state.address.textProps
                                .map(
                                    (prop) => TextEditComponent(textProp: prop))
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
                            children: BaseEditComponent.combineList([
                              state.personalInfo.propsEnumeration
                                  .map((prop) => EnumerationViewComponent(
                                      enumeration: prop,
                                      listEnumerationOptions: state
                                          .personalInfoSeletionOptions
                                          .getSelectionOptions(
                                              prop.runtimeType)))
                                  .toList(),
                              [
                                EnumerationListViewComponent(
                                  enumerationsList:
                                      state.personalInfo.deficiency,
                                  enumerationOptionsList: state
                                      .personalInfoSeletionOptions
                                      .getSelectionOptions(Disability),
                                ),
                                TextEditComponent(
                                  textProp:
                                      state.personalInfo.deficiency.observation,
                                ),
                              ],
                            ]),
                          ),
                          const SizedBox(
                            height: 16,
                          ),
                          PropsContainerComponent(
                            containerName: "Identidade",
                            isSavingData: state.isSavingData,
                            loadingContainerData: () =>
                                bloc.add(LoadingIdCardEvent()),
                            saveContainerData: (changes) =>
                                bloc.add(SaveIdCardEvent(changes)),
                            isLoading: state.idCard.isLoading,
                            isLazyLoading: state.idCard.isLazyLoading,
                            children: state.idCard.textProps
                                .map(
                                    (prop) => TextEditComponent(textProp: prop))
                                .toList(),
                          ),
                          const SizedBox(
                            height: 16,
                          ),
                          PropsContainerComponent(
                            containerName: "Título de eleitor",
                            isSavingData: state.isSavingData,
                            loadingContainerData: () =>
                                bloc.add(LoadingVoteIdEvent()),
                            saveContainerData: (changes) =>
                                bloc.add(SaveVoteIdEvent(changes)),
                            isLoading: state.voteId.isLoading,
                            isLazyLoading: state.voteId.isLazyLoading,
                            children: [
                              TextEditComponent(textProp: state.voteId.number)
                            ],
                          ),
                          state.militaryDocument.isRequired
                              ? Column(
                                  children: [
                                    const SizedBox(
                                      height: 16,
                                    ),
                                    PropsContainerComponent(
                                      containerName: "Documento Militar",
                                      isSavingData: state.isSavingData,
                                      loadingContainerData: () => bloc
                                          .add(LoadingMilitaryDocumentEvent()),
                                      saveContainerData: (changes) => bloc.add(
                                          SaveMilitaryDocumentEvent(changes)),
                                      isLoading:
                                          state.militaryDocument.isLoading,
                                      isLazyLoading:
                                          state.militaryDocument.isLazyLoading,
                                      children: state.militaryDocument.textProps
                                          .map((prop) =>
                                              TextEditComponent(textProp: prop))
                                          .toList(),
                                    ),
                                  ],
                                )
                              : Container(),
                          const SizedBox(
                            height: 16,
                          ),
                          DependentsListComponent(
                            companyId: state.company.id,
                            employeeId: state.employee.id,
                          ),
                          const SizedBox(
                            height: 16,
                          ),
                          PropsContainerComponent(
                            containerName: "Exame Medico Admissional",
                            isSavingData: state.isSavingData,
                            saveContainerData: (changes) => bloc
                                .add(SaveMedicalAdmissionExamEvent(changes)),
                            loadingContainerData: () =>
                                bloc.add(LoadingMedicalAdmissionExamEvent()),
                            isLoading: state.medicalAdmissionExam.isLoading,
                            children: state.medicalAdmissionExam.textProps
                                .map(
                                    (prop) => TextEditComponent(textProp: prop))
                                .toList(),
                          ),
                          const SizedBox(
                            height: 16,
                          ),
                          PropsContainerComponent(
                              containerName: "Informações de Função",
                              isSavingData: state.isSavingData,
                              saveContainerData: (changes) =>
                                  bloc.add(SaveRoleInfoEvent()),
                              loadingContainerData: () =>
                                  bloc.add(LoadingRoleEvent()),
                              loadingLazyContainerData: () =>
                                  bloc.add(LazyLoadingRoleInfoEvent()),
                              isLazyLoading: state.roleInfo.isLazyLoading,
                              isLoading: state.roleInfo.isLoading,
                              children: [
                                EnumerationViewComponent(
                                  onChanged: (change) =>
                                      bloc.add(ChangeRoleInfoEvent(change)),
                                  enumeration: state.roleInfo.department,
                                  listEnumerationOptions: state.listDepartment,
                                ),
                                TextComponet(
                                  textBase: state.roleInfo.department
                                      .depertmentDescription,
                                ),
                                EnumerationViewComponent(
                                  onChanged: (change) =>
                                      bloc.add(ChangeRoleInfoEvent(change)),
                                  enumeration: state.roleInfo.position,
                                  listEnumerationOptions: state.listPosition,
                                ),
                                TextComponet(
                                  textBase: state
                                      .roleInfo.position.positionDescription,
                                ),
                                TextComponet(
                                  textBase: state.roleInfo.position.positionCbo,
                                ),
                                EnumerationViewComponent(
                                  onChanged: (change) =>
                                      bloc.add(ChangeRoleInfoEvent(change)),
                                  enumeration: state.roleInfo.role,
                                  listEnumerationOptions: state.listRolen,
                                ),
                                TextComponet(
                                  textBase: state.roleInfo.role.roleDescription,
                                ),
                                TextComponet(
                                  textBase: state.roleInfo.role.roleCbo,
                                ),
                                TextComponet(
                                  textBase: state.roleInfo.role.salary,
                                ),
                                TextComponet(
                                  textBase:
                                      state.roleInfo.role.salaryDescription,
                                ),
                              ]),
                          const SizedBox(
                            height: 16,
                          ),
                          PropsContainerComponent(
                            containerName: "Local de trabalho",
                            isSavingData: state.isSavingData,
                            saveContainerData: (changes) =>
                                bloc.add(SaveWorkplaceEvent()),
                            loadingContainerData: () =>
                                bloc.add(LoadingWorkplaceEvent()),
                            loadingLazyContainerData: () =>
                                bloc.add(LazyLoadingWorkplaceEvent()),
                            isLoading: state.isLoading,
                            children: [
                              EnumerationViewComponent(
                                  onChanged: (obj) =>
                                      bloc.add(ChangeWorkplaceEvent(obj)),
                                  enumeration: state.workplace,
                                  listEnumerationOptions: state.listWorkplace),
                              TextComponet(
                                textBase: state.workplace.address,
                              ),
                            ],
                          ),
                          const SizedBox(
                            height: 16,
                          ),
                          ContractsViewComponent(
                            loadingContainerData: () =>
                                bloc.add(LoadingContractsEvent()),
                            onFineshedContract: (finalDate) =>
                                bloc.add(FinishedContractEvent(finalDate)),
                            onInitContract: (initDate, contractTypeId) =>
                                bloc.add(
                                    NewContractEvent(initDate, contractTypeId)),
                            contracts: state.listContracts,
                            listContractTypeOptions: state.listContractTypes,
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
                  state.company == const Company.empty()
                      ? state.company.fantasyName
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
