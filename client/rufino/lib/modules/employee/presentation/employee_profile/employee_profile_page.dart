import 'dart:typed_data';

import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:mask_text_input_formatter/mask_text_input_formatter.dart';
import 'package:rufino/domain/model/company.dart';
import 'package:rufino/modules/employee/domain/model/document_signing_options.dart';
import 'package:rufino/modules/employee/domain/model/employee.dart';
import 'package:rufino/modules/employee/domain/model/employee_contract.dart';
import 'package:rufino/modules/employee/domain/model/employee_contract_type.dart';
import 'package:rufino/modules/employee/domain/model/name.dart';
import 'package:rufino/modules/employee/domain/model/personal_info/disability.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/bloc/employee_profile_bloc.dart';
import 'package:rufino/modules/employee/presentation/components/base_edit_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/contracts_view_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/dependents_list_component/dependents_list_component.dart';
import 'package:rufino/modules/employee/presentation/components/enumeration_list_view_component.dart';
import 'package:rufino/modules/employee/presentation/components/enumeration_view_component.dart';
import 'package:rufino/modules/employee/presentation/components/props_container_component.dart';
import 'package:rufino/modules/employee/presentation/components/text_componet.dart';
import 'package:rufino/modules/employee/presentation/components/text_edit_component.dart';
import 'package:rufino/modules/employee/presentation/employee_profile/components/documents_component/documents_component.dart';
import 'package:rufino/shared/components/error_components.dart';

class EmployeeProfilePage extends StatelessWidget {
  final _formKey = GlobalKey<FormState>();
  final _formDocumentSigningOptionsKey = GlobalKey<FormState>();
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
                      () => Modular.to.popUntil((route) => route.isFirst));
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
                          Stack(
                            children: [
                              CircleAvatar(
                                radius: 80,
                                backgroundImage: state.employeeImage != null
                                    ? MemoryImage(Uint8List.fromList(
                                        state.employeeImage!))
                                    : const AssetImage(
                                            "assets/img/avatar_default.png")
                                        as ImageProvider,
                              ),
                              Positioned(
                                bottom: 0,
                                right: 0,
                                child: Container(
                                  decoration: BoxDecoration(
                                    color: Theme.of(context).primaryColor,
                                    shape: BoxShape.circle,
                                    border: Border.all(
                                      color: Colors.white,
                                      width: 2,
                                    ),
                                  ),
                                  child: IconButton(
                                    onPressed: () {
                                      bloc.add(EditAvatarImageEvent());
                                    },
                                    icon: const Icon(
                                      Icons.edit,
                                      size: 20,
                                      color: Colors.white,
                                    ),
                                    padding: const EdgeInsets.all(8.0),
                                    constraints: const BoxConstraints(),
                                  ),
                                ),
                              ),
                            ],
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
                          _hiredButton(context, state),
                          _employeeName(context, state.isEditingName,
                              state.isSavingData, state.employee.name),
                          const SizedBox(
                            height: 16,
                          ),
                          _documentSigningOptions(context, state),
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
                            contracts: state.listContracts,
                            listContractTypeOptions: state.listContractTypes,
                            isSavingData: state.isSavingData,
                          ),
                          const SizedBox(
                            height: 16,
                          ),
                          DocumentsComponent(
                            companyId: state.company.id,
                            employeeId: state.employee.id,
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
          Modular.to.pop();
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

  Widget _documentSigningOptions(
      BuildContext context, EmployeeProfileState state) {
    return Row(
      children: [
        Expanded(
          child: Form(
            key: _formDocumentSigningOptionsKey,
            child: EnumerationViewComponent(
              isEditing: state.isDocumentSigningOptionsName,
              isLoading: state.isLoading,
              enumeration: state.employee.documentSigningOptions,
              listEnumerationOptions: state.listDocumentSigningOptions,
              onChanged: (change) => bloc.add(ChangeDocumentSigningOptionsEvent(
                  change as DocumentSigningOptions)),
            ),
          ),
        ),
        const SizedBox(
          width: 8,
        ),
        state.isSavingData
            ? const Center(child: CircularProgressIndicator())
            : state.isDocumentSigningOptionsName
                ? Row(
                    children: [
                      TextButton(
                          onPressed: () => bloc.add(
                              const EditDocumentSigningOptionsEvent(false)),
                          child: const Text("Cancelar")),
                      FilledButton(
                        onPressed: () {
                          if (_formDocumentSigningOptionsKey.currentState !=
                                  null &&
                              _formDocumentSigningOptionsKey.currentState!
                                  .validate()) {
                            _formDocumentSigningOptionsKey.currentState!.save();
                            bloc.add(SaveDocumentSigningOptionsEvent());
                          }
                        },
                        child: const Text("Salvar"),
                      )
                    ],
                  )
                : TextButton(
                    onPressed: () =>
                        bloc.add(const EditDocumentSigningOptionsEvent(true)),
                    child: const Text("Editar"),
                  ),
      ],
    );
  }

  Widget _hiredButton(BuildContext context, EmployeeProfileState state) {
    return state.employee.canBeHired()
        ? Padding(
            padding: const EdgeInsets.all(16.0),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                FilledButton(
                  onPressed: () {
                    _confirmAction(context, state);
                  },
                  child: const Text("Contratar"),
                ),
                if (state.employee.status.id == 1) ...[
                  const SizedBox(width: 16),
                  TextButton(
                    onPressed: () {
                      _confirmMarkAsInactive(context, state);
                    },
                    child: const Text("Marcar como Inativo"),
                  ),
                ],
              ],
            ),
          )
        : const SizedBox();
  }

  void _confirmMarkAsInactive(
    BuildContext context,
    EmployeeProfileState state,
  ) {
    showDialog(
      context: context,
      builder: (_) {
        return AlertDialog(
          title: const Text("Confirmar Ação"),
          content: const Text(
              "Tem certeza que deseja marcar este funcionário como inativo?"),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text("Cancelar"),
            ),
            FilledButton(
              onPressed: () {
                Navigator.pop(context);
                bloc.add(MarkAsInactiveEvent());
              },
              child: const Text("Confirmar"),
            ),
          ],
        );
      },
    );
  }

  void _confirmAction(
    BuildContext context,
    EmployeeProfileState state,
  ) {
    WidgetsBinding.instance.addPostFrameCallback((_) {
      showDialog(
          barrierDismissible: false,
          context: context,
          builder: (_) {
            final dialogFormKey = GlobalKey<FormState>();
            String dateSeleted = "";
            String text = "";
            String typeSelected = "";
            return AlertDialog(
              title: Text("Contratar Funcionário"),
              content: SizedBox(
                  width: 400,
                  child: Form(
                    key: dialogFormKey,
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        _TextFrom(context, (value) => text = value),
                        SizedBox(
                          height: 16,
                        ),
                        _dataFrom(context, EmployeeContract.validate,
                            (value) => dateSeleted = value),
                        SizedBox(
                          height: 16,
                        ),
                        _dropDownForm(state.listContractTypes,
                            (value) => typeSelected = value),
                      ],
                    ),
                  )),
              actions: [
                TextButton(
                  onPressed: () => Navigator.pop(context),
                  child: const Text("Cancelar"),
                ),
                FilledButton(
                  onPressed: () {
                    if (dialogFormKey.currentState != null &&
                        dialogFormKey.currentState!.validate()) {
                      bloc.add(
                          NewContractEvent(dateSeleted, typeSelected, text));
                      Navigator.pop(context);
                    }
                  },
                  child: const Text("Confirmar"),
                ),
              ],
            );
          });
    });
  }

  Widget _dataFrom(BuildContext context, Function(String?) validation,
      Function(String value) onChange) {
    return TextFormField(
      inputFormatters: [
        MaskTextInputFormatter(
            mask: '##/##/####',
            filter: {"#": RegExp(r'[0-9]')},
            type: MaskAutoCompletionType.lazy)
      ],
      keyboardType: TextInputType.datetime,
      controller: TextEditingController(),
      enabled: true,
      decoration: InputDecoration(
        labelText: "Data de Inicio",
        border: const OutlineInputBorder(),
      ),
      style: TextStyle(color: Theme.of(context).colorScheme.onSurface),
      validator: (value) => validation(value),
      onChanged: onChange,
    );
  }

  Widget _TextFrom(BuildContext context, Function(String value) onChange) {
    return TextFormField(
      keyboardType: TextInputType.datetime,
      controller: TextEditingController(),
      enabled: true,
      decoration: InputDecoration(
        labelText: "Identificação de Registro",
        border: const OutlineInputBorder(),
      ),
      validator: Employee.validateRegistration,
      style: TextStyle(color: Theme.of(context).colorScheme.onSurface),
      onChanged: onChange,
    );
  }

  Widget _dropDownForm(List<EmployeeContractType> listContractTypeOptions,
      Function(String value) onChange) {
    return DropdownButtonFormField(
      items: listContractTypeOptions
          .map((e) => DropdownMenuItem(value: e, child: Text(e.name)))
          .toList(),
      onChanged: (EmployeeContractType? value) {
        if (value != null) {
          onChange(value.id);
        }
      },
      validator: (value) {
        if (value == null || value == EmployeeContractType.empty) {
          return 'Por favor, selecione um opção.';
        }
        return null;
      },
      decoration: InputDecoration(
        enabled: true,
        labelText: "Tipo de Contrato",
        border: const OutlineInputBorder(),
      ),
    );
  }
}
