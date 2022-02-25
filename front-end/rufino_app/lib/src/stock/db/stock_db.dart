import 'package:drift/drift.dart';
import 'package:drift/native.dart';
import 'package:path_provider/path_provider.dart';
import 'package:path/path.dart' as p;
import 'dart:io';

import 'package:uuid/uuid.dart';

part 'stock_db.g.dart';

@DataClassName("Product")
class Products extends Table {
  TextColumn get id => text()();
  TextColumn get name => text()();
  TextColumn get description => text()();
  TextColumn get section => text()();
  TextColumn get category => text()();
  TextColumn get unity => text()();
  IntColumn get quantity => integer()();

  @override
  Set<Column>? get primaryKey => {id};
}

@DataClassName("ProductTransaction")
class ProductsTransactions extends Table {
  TextColumn get id => text()();
  TextColumn get idTransactionServer => text().nullable()();
  TextColumn get idProduct => text()();
  IntColumn get quantityVariation => integer()();
  DateTimeColumn get date => dateTime()();
  TextColumn get idResponsible => text()();
  TextColumn get idTaker => text()();

  @override
  Set<Column>? get primaryKey => {id};
}

@DataClassName("Worker")
class Workers extends Table {
  TextColumn get id => text()();
  TextColumn get name => text()();

  @override
  Set<Column>? get primaryKey => {id};
}

@DriftDatabase(tables: [Products, ProductsTransactions, Workers])
class StockDb extends _$StockDb {
  final ConnectOptions options;
  StockDb(this.options) : super(_openConnect(options));

  @override
  MigrationStrategy get migration =>
      MigrationStrategy(onCreate: (Migrator m) async {
        var idGeneration = Uuid().v4();
        await m.createAll();
        if (options == ConnectOptions.dev) {
          //generateDataOnDb();
        }
      });

  // Future<void> generateDataOnDb() async {
  //   var listProducts = List.generate(
  //     10000,
  //     (i) => const Uuid().v4(),
  //   );
  //   var listWorkers = List.generate(
  //     100,
  //     (i) => const Uuid().v4(),
  //   );
  //   batch((batch) => {
  //         batch.insertAll(products, listProducts.map((e) {
  //           return Product(
  //               id: e,
  //               name: "Tubo de PVC 100 mm para esgoto $e",
  //               description:
  //                   "Se o conteúdo for muito grande para caber na tela verticalmente, a caixa de diálogo exibirá o título e as ações e deixará o conteúdo transbordar, o que raramente é desejado. Considere usar um widget de rolagem para conteúdo , como SingleChildScrollView , para evitar estouro. (No entanto, esteja ciente de que, como AlertDialog tenta se dimensionar usando as dimensões intrínsecas de seus filhos, widgets como ListView , GridView e CustomScrollView , que usam viewports preguiçosas, não funcionarão. Se isso for um problema, considere usar o Dialog diretamente. )",
  //               section: "section",
  //               category: "category",
  //               unity: "unity",
  //               quantity: 10000);
  //         }))
  //       });
  //   batch((batch) => {
  //         batch.insertAll(workers, listWorkers.map((e) {
  //           return Worker(
  //             id: e,
  //             name: "Nome: $e",
  //           );
  //         }))
  //       });
  // }

  @override
  int get schemaVersion => 1;
}

enum ConnectOptions {
  dev,
  mobile,
}

QueryExecutor _openConnect(ConnectOptions options) {
  switch (options) {
    case ConnectOptions.mobile:
      return _connectionMobile();
    default:
      return NativeDatabase.memory();
  }
}

LazyDatabase _connectionMobile() {
  return LazyDatabase(() async {
    final dbFolder = await getApplicationDocumentsDirectory();
    final file = File(p.join(p.join(dbFolder.path, 'Stockdb.sqlite')));
    return NativeDatabase(file);
  });
}
