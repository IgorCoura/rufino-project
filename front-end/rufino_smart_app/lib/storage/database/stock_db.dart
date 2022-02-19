import 'package:drift/drift.dart';
import 'package:drift/native.dart';
import 'package:path_provider/path_provider.dart';
import 'package:path/path.dart' as p;
import 'dart:io';

import 'package:rufino_smart_app/storage/database/dao/product_dao.dart';
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
  static StockDb instance = StockDb._internal();

  ProductDao productDao = ProductDao(instance);

  StockDb._internal() : super(_openConnection());

  @override
  int get schemaVersion => 1;
}

LazyDatabase _openConnection() {
  return LazyDatabase(() async {
    final dbFolder = await getApplicationDocumentsDirectory();
    final file = File(p.join(p.join(dbFolder.path, 'db.sqlite')));
    return NativeDatabase(file);
  });
}
