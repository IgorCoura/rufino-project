import 'package:flutter/material.dart';
import 'package:flutter_bloc/flutter_bloc.dart';
import 'package:flutter_modular/flutter_modular.dart';
import 'package:rufino/modules/company/company_selection/bloc/company_selection_bloc.dart';
import 'package:rufino/shared/components/error_components.dart';

class CompanySelectionPage extends StatelessWidget {
  final bloc = Modular.get<CompanySelectionBloc>();
  CompanySelectionPage({super.key}) {
    bloc.add(InitialCompanyEvent());
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: Center(
          child: Padding(
        padding: const EdgeInsets.all(16.0),
        child: BlocBuilder<CompanySelectionBloc, CompanySelectionState>(
          bloc: bloc,
          builder: (context, state) {
            if (state.hasSelectedCompany) {
              Modular.to.navigate("/home/");
            }

            if (state.exception != null) {
              ErrorComponent.showException(
                  context, state.exception!, () => Modular.to.navigate("/"));
            }
            if (state.isLoading) {
              return const CircularProgressIndicator();
            }
            return Column(
              mainAxisAlignment: MainAxisAlignment.center,
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                state.companies.isNotEmpty
                    ? Column(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          const Text(
                            "Selecione a Empresa.",
                            style: TextStyle(
                              fontSize: 24,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          const SizedBox(
                            height: 16,
                          ),
                          DropdownMenu(
                              width: 600,
                              textStyle: const TextStyle(fontSize: 18),
                              initialSelection: state.selectedCompany,
                              onSelected: (selected) => bloc
                                  .add(ChangeSelectionOptionEvent(selected)),
                              dropdownMenuEntries: state.companies
                                  .map((comp) => DropdownMenuEntry(
                                      value: comp.id, label: comp.fantasyName))
                                  .toList()),
                          const SizedBox(
                            height: 16,
                          ),
                          FilledButton(
                            onPressed: () => bloc.add(SelectCompanyEvent()),
                            child: const Padding(
                              padding: EdgeInsets.all(8.0),
                              child: Text(
                                'Selecionar',
                                style: TextStyle(
                                  fontSize: 14,
                                ),
                              ),
                            ),
                          ),
                        ],
                      )
                    : SizedBox(),
                const SizedBox(
                  height: 16,
                ),
              ],
            );
          },
        ),
      )),
      floatingActionButton: FloatingActionButton.extended(
        onPressed: () {
          Modular.to.navigate('/company/create');
        },
        icon: const Icon(Icons.add),
        label: const Text('Criar Empresa'),
      ),
    );
  }
}
