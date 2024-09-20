import 'package:flutter/material.dart';
import 'package:flutter_modular/flutter_modular.dart';

class EmployeesListPage extends StatefulWidget {
  @override
  _EmployeesListPageState createState() => _EmployeesListPageState();
}

class _EmployeesListPageState extends State<EmployeesListPage> {
  // Lista de funcionários
  final List<Employee> employees = List.generate(
    50,
    (index) => Employee(
      numeroRegistro: index + 1,
      nome: 'Funcionário ${index + 1}',
      cargo: index % 2 == 0 ? 'Gerente' : 'Desenvolvedor',
      status: index % 2 == 0 ? 'Ativo' : 'Inativo',
    ),
  );

  // Paginação
  int _currentPage = 0;
  final int _itemsPerPage = 15;

  List<Employee> get _currentPageEmployees {
    final start = _currentPage * _itemsPerPage;
    final end = start + _itemsPerPage;
    return employees.sublist(
        start, end > employees.length ? employees.length : end);
  }

  void _nextPage() {
    if ((_currentPage + 1) * _itemsPerPage < employees.length) {
      setState(() {
        _currentPage++;
      });
    }
  }

  void _previousPage() {
    if (_currentPage > 0) {
      setState(() {
        _currentPage--;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        leading: IconButton(
          icon: const Icon(Icons.arrow_back),
          onPressed: () {
            Modular.to.navigate("/home");
          },
        ),
        title: const Row(
          children: [
            CircleAvatar(
              backgroundImage: AssetImage("assets/img/company_default.jpg"),
            ),
            SizedBox(width: 10),
            Text(
              'Empresa XYZ',
              overflow: TextOverflow.ellipsis,
            ),
          ],
        ),
        actions: [
          Padding(
            padding: const EdgeInsets.all(16.0),
            child: Icon(Icons.filter_list_outlined),
          ),
          Padding(
            padding: const EdgeInsets.all(16.0),
            child: Icon(Icons.search),
          )
        ],
      ),
      body: Column(
        children: [
          Expanded(
            child: ListView.builder(
              itemCount: _currentPageEmployees.length,
              itemBuilder: (context, index) {
                final employee = _currentPageEmployees[index];
                return ListTile(
                  leading: const CircleAvatar(
                    backgroundImage:
                        AssetImage("assets/img/avatar_default.png"),
                  ),
                  title: Text(
                    employee.nome,
                  ),
                  subtitle: Text('Função: ${employee.cargo}'),
                  trailing: Text(
                    employee.status,
                    style: const TextStyle(fontSize: 14),
                  ),
                  onTap: () {},
                );
              },
            ),
          ),
        ],
      ),
      floatingActionButton: FloatingActionButton(
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(30), // Arredondando os cantos
        ),
        onPressed: () {
          // Ação do segundo botão
        }, // Ícone centralizado
        heroTag: "btn2",
        child: const Icon(Icons.add), // HeroTag único
      ),
    );
  }
}

class Employee {
  final int numeroRegistro;
  final String nome;
  final String cargo;
  final String status;

  Employee({
    required this.numeroRegistro,
    required this.nome,
    required this.cargo,
    required this.status,
  });
}
