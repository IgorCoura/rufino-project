// GENERATED CODE - DO NOT MODIFY BY HAND

part of 'stock_db.dart';

// **************************************************************************
// MoorGenerator
// **************************************************************************

// ignore_for_file: unnecessary_brace_in_string_interps, unnecessary_this
class Product extends DataClass implements Insertable<Product> {
  final String id;
  final String name;
  final String description;
  final String section;
  final String category;
  final String unity;
  final int quantity;
  Product(
      {required this.id,
      required this.name,
      required this.description,
      required this.section,
      required this.category,
      required this.unity,
      required this.quantity});
  factory Product.fromData(Map<String, dynamic> data, {String? prefix}) {
    final effectivePrefix = prefix ?? '';
    return Product(
      id: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}id'])!,
      name: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}name'])!,
      description: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}description'])!,
      section: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}section'])!,
      category: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}category'])!,
      unity: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}unity'])!,
      quantity: const IntType()
          .mapFromDatabaseResponse(data['${effectivePrefix}quantity'])!,
    );
  }
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['id'] = Variable<String>(id);
    map['name'] = Variable<String>(name);
    map['description'] = Variable<String>(description);
    map['section'] = Variable<String>(section);
    map['category'] = Variable<String>(category);
    map['unity'] = Variable<String>(unity);
    map['quantity'] = Variable<int>(quantity);
    return map;
  }

  ProductsCompanion toCompanion(bool nullToAbsent) {
    return ProductsCompanion(
      id: Value(id),
      name: Value(name),
      description: Value(description),
      section: Value(section),
      category: Value(category),
      unity: Value(unity),
      quantity: Value(quantity),
    );
  }

  factory Product.fromJson(Map<String, dynamic> json,
      {ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return Product(
      id: serializer.fromJson<String>(json['id']),
      name: serializer.fromJson<String>(json['name']),
      description: serializer.fromJson<String>(json['description']),
      section: serializer.fromJson<String>(json['section']),
      category: serializer.fromJson<String>(json['category']),
      unity: serializer.fromJson<String>(json['unity']),
      quantity: serializer.fromJson<int>(json['quantity']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'id': serializer.toJson<String>(id),
      'name': serializer.toJson<String>(name),
      'description': serializer.toJson<String>(description),
      'section': serializer.toJson<String>(section),
      'category': serializer.toJson<String>(category),
      'unity': serializer.toJson<String>(unity),
      'quantity': serializer.toJson<int>(quantity),
    };
  }

  Product copyWith(
          {String? id,
          String? name,
          String? description,
          String? section,
          String? category,
          String? unity,
          int? quantity}) =>
      Product(
        id: id ?? this.id,
        name: name ?? this.name,
        description: description ?? this.description,
        section: section ?? this.section,
        category: category ?? this.category,
        unity: unity ?? this.unity,
        quantity: quantity ?? this.quantity,
      );
  @override
  String toString() {
    return (StringBuffer('Product(')
          ..write('id: $id, ')
          ..write('name: $name, ')
          ..write('description: $description, ')
          ..write('section: $section, ')
          ..write('category: $category, ')
          ..write('unity: $unity, ')
          ..write('quantity: $quantity')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode =>
      Object.hash(id, name, description, section, category, unity, quantity);
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is Product &&
          other.id == this.id &&
          other.name == this.name &&
          other.description == this.description &&
          other.section == this.section &&
          other.category == this.category &&
          other.unity == this.unity &&
          other.quantity == this.quantity);
}

class ProductsCompanion extends UpdateCompanion<Product> {
  final Value<String> id;
  final Value<String> name;
  final Value<String> description;
  final Value<String> section;
  final Value<String> category;
  final Value<String> unity;
  final Value<int> quantity;
  const ProductsCompanion({
    this.id = const Value.absent(),
    this.name = const Value.absent(),
    this.description = const Value.absent(),
    this.section = const Value.absent(),
    this.category = const Value.absent(),
    this.unity = const Value.absent(),
    this.quantity = const Value.absent(),
  });
  ProductsCompanion.insert({
    required String id,
    required String name,
    required String description,
    required String section,
    required String category,
    required String unity,
    required int quantity,
  })  : id = Value(id),
        name = Value(name),
        description = Value(description),
        section = Value(section),
        category = Value(category),
        unity = Value(unity),
        quantity = Value(quantity);
  static Insertable<Product> custom({
    Expression<String>? id,
    Expression<String>? name,
    Expression<String>? description,
    Expression<String>? section,
    Expression<String>? category,
    Expression<String>? unity,
    Expression<int>? quantity,
  }) {
    return RawValuesInsertable({
      if (id != null) 'id': id,
      if (name != null) 'name': name,
      if (description != null) 'description': description,
      if (section != null) 'section': section,
      if (category != null) 'category': category,
      if (unity != null) 'unity': unity,
      if (quantity != null) 'quantity': quantity,
    });
  }

  ProductsCompanion copyWith(
      {Value<String>? id,
      Value<String>? name,
      Value<String>? description,
      Value<String>? section,
      Value<String>? category,
      Value<String>? unity,
      Value<int>? quantity}) {
    return ProductsCompanion(
      id: id ?? this.id,
      name: name ?? this.name,
      description: description ?? this.description,
      section: section ?? this.section,
      category: category ?? this.category,
      unity: unity ?? this.unity,
      quantity: quantity ?? this.quantity,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (id.present) {
      map['id'] = Variable<String>(id.value);
    }
    if (name.present) {
      map['name'] = Variable<String>(name.value);
    }
    if (description.present) {
      map['description'] = Variable<String>(description.value);
    }
    if (section.present) {
      map['section'] = Variable<String>(section.value);
    }
    if (category.present) {
      map['category'] = Variable<String>(category.value);
    }
    if (unity.present) {
      map['unity'] = Variable<String>(unity.value);
    }
    if (quantity.present) {
      map['quantity'] = Variable<int>(quantity.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('ProductsCompanion(')
          ..write('id: $id, ')
          ..write('name: $name, ')
          ..write('description: $description, ')
          ..write('section: $section, ')
          ..write('category: $category, ')
          ..write('unity: $unity, ')
          ..write('quantity: $quantity')
          ..write(')'))
        .toString();
  }
}

class $ProductsTable extends Products with TableInfo<$ProductsTable, Product> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $ProductsTable(this.attachedDatabase, [this._alias]);
  final VerificationMeta _idMeta = const VerificationMeta('id');
  @override
  late final GeneratedColumn<String?> id = GeneratedColumn<String?>(
      'id', aliasedName, false,
      type: const StringType(), requiredDuringInsert: true);
  final VerificationMeta _nameMeta = const VerificationMeta('name');
  @override
  late final GeneratedColumn<String?> name = GeneratedColumn<String?>(
      'name', aliasedName, false,
      type: const StringType(), requiredDuringInsert: true);
  final VerificationMeta _descriptionMeta =
      const VerificationMeta('description');
  @override
  late final GeneratedColumn<String?> description = GeneratedColumn<String?>(
      'description', aliasedName, false,
      type: const StringType(), requiredDuringInsert: true);
  final VerificationMeta _sectionMeta = const VerificationMeta('section');
  @override
  late final GeneratedColumn<String?> section = GeneratedColumn<String?>(
      'section', aliasedName, false,
      type: const StringType(), requiredDuringInsert: true);
  final VerificationMeta _categoryMeta = const VerificationMeta('category');
  @override
  late final GeneratedColumn<String?> category = GeneratedColumn<String?>(
      'category', aliasedName, false,
      type: const StringType(), requiredDuringInsert: true);
  final VerificationMeta _unityMeta = const VerificationMeta('unity');
  @override
  late final GeneratedColumn<String?> unity = GeneratedColumn<String?>(
      'unity', aliasedName, false,
      type: const StringType(), requiredDuringInsert: true);
  final VerificationMeta _quantityMeta = const VerificationMeta('quantity');
  @override
  late final GeneratedColumn<int?> quantity = GeneratedColumn<int?>(
      'quantity', aliasedName, false,
      type: const IntType(), requiredDuringInsert: true);
  @override
  List<GeneratedColumn> get $columns =>
      [id, name, description, section, category, unity, quantity];
  @override
  String get aliasedName => _alias ?? 'products';
  @override
  String get actualTableName => 'products';
  @override
  VerificationContext validateIntegrity(Insertable<Product> instance,
      {bool isInserting = false}) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('id')) {
      context.handle(_idMeta, id.isAcceptableOrUnknown(data['id']!, _idMeta));
    } else if (isInserting) {
      context.missing(_idMeta);
    }
    if (data.containsKey('name')) {
      context.handle(
          _nameMeta, name.isAcceptableOrUnknown(data['name']!, _nameMeta));
    } else if (isInserting) {
      context.missing(_nameMeta);
    }
    if (data.containsKey('description')) {
      context.handle(
          _descriptionMeta,
          description.isAcceptableOrUnknown(
              data['description']!, _descriptionMeta));
    } else if (isInserting) {
      context.missing(_descriptionMeta);
    }
    if (data.containsKey('section')) {
      context.handle(_sectionMeta,
          section.isAcceptableOrUnknown(data['section']!, _sectionMeta));
    } else if (isInserting) {
      context.missing(_sectionMeta);
    }
    if (data.containsKey('category')) {
      context.handle(_categoryMeta,
          category.isAcceptableOrUnknown(data['category']!, _categoryMeta));
    } else if (isInserting) {
      context.missing(_categoryMeta);
    }
    if (data.containsKey('unity')) {
      context.handle(
          _unityMeta, unity.isAcceptableOrUnknown(data['unity']!, _unityMeta));
    } else if (isInserting) {
      context.missing(_unityMeta);
    }
    if (data.containsKey('quantity')) {
      context.handle(_quantityMeta,
          quantity.isAcceptableOrUnknown(data['quantity']!, _quantityMeta));
    } else if (isInserting) {
      context.missing(_quantityMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {id};
  @override
  Product map(Map<String, dynamic> data, {String? tablePrefix}) {
    return Product.fromData(data,
        prefix: tablePrefix != null ? '$tablePrefix.' : null);
  }

  @override
  $ProductsTable createAlias(String alias) {
    return $ProductsTable(attachedDatabase, alias);
  }
}

class ProductTransaction extends DataClass
    implements Insertable<ProductTransaction> {
  final String id;
  final String? idTransactionServer;
  final String idProduct;
  final int quantityVariation;
  final DateTime date;
  final String idResponsible;
  final String idTaker;
  ProductTransaction(
      {required this.id,
      this.idTransactionServer,
      required this.idProduct,
      required this.quantityVariation,
      required this.date,
      required this.idResponsible,
      required this.idTaker});
  factory ProductTransaction.fromData(Map<String, dynamic> data,
      {String? prefix}) {
    final effectivePrefix = prefix ?? '';
    return ProductTransaction(
      id: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}id'])!,
      idTransactionServer: const StringType().mapFromDatabaseResponse(
          data['${effectivePrefix}id_transaction_server']),
      idProduct: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}id_product'])!,
      quantityVariation: const IntType().mapFromDatabaseResponse(
          data['${effectivePrefix}quantity_variation'])!,
      date: const DateTimeType()
          .mapFromDatabaseResponse(data['${effectivePrefix}date'])!,
      idResponsible: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}id_responsible'])!,
      idTaker: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}id_taker'])!,
    );
  }
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['id'] = Variable<String>(id);
    if (!nullToAbsent || idTransactionServer != null) {
      map['id_transaction_server'] = Variable<String?>(idTransactionServer);
    }
    map['id_product'] = Variable<String>(idProduct);
    map['quantity_variation'] = Variable<int>(quantityVariation);
    map['date'] = Variable<DateTime>(date);
    map['id_responsible'] = Variable<String>(idResponsible);
    map['id_taker'] = Variable<String>(idTaker);
    return map;
  }

  ProductsTransactionsCompanion toCompanion(bool nullToAbsent) {
    return ProductsTransactionsCompanion(
      id: Value(id),
      idTransactionServer: idTransactionServer == null && nullToAbsent
          ? const Value.absent()
          : Value(idTransactionServer),
      idProduct: Value(idProduct),
      quantityVariation: Value(quantityVariation),
      date: Value(date),
      idResponsible: Value(idResponsible),
      idTaker: Value(idTaker),
    );
  }

  factory ProductTransaction.fromJson(Map<String, dynamic> json,
      {ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return ProductTransaction(
      id: serializer.fromJson<String>(json['id']),
      idTransactionServer:
          serializer.fromJson<String?>(json['idTransactionServer']),
      idProduct: serializer.fromJson<String>(json['idProduct']),
      quantityVariation: serializer.fromJson<int>(json['quantityVariation']),
      date: serializer.fromJson<DateTime>(json['date']),
      idResponsible: serializer.fromJson<String>(json['idResponsible']),
      idTaker: serializer.fromJson<String>(json['idTaker']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'id': serializer.toJson<String>(id),
      'idTransactionServer': serializer.toJson<String?>(idTransactionServer),
      'idProduct': serializer.toJson<String>(idProduct),
      'quantityVariation': serializer.toJson<int>(quantityVariation),
      'date': serializer.toJson<DateTime>(date),
      'idResponsible': serializer.toJson<String>(idResponsible),
      'idTaker': serializer.toJson<String>(idTaker),
    };
  }

  ProductTransaction copyWith(
          {String? id,
          String? idTransactionServer,
          String? idProduct,
          int? quantityVariation,
          DateTime? date,
          String? idResponsible,
          String? idTaker}) =>
      ProductTransaction(
        id: id ?? this.id,
        idTransactionServer: idTransactionServer ?? this.idTransactionServer,
        idProduct: idProduct ?? this.idProduct,
        quantityVariation: quantityVariation ?? this.quantityVariation,
        date: date ?? this.date,
        idResponsible: idResponsible ?? this.idResponsible,
        idTaker: idTaker ?? this.idTaker,
      );
  @override
  String toString() {
    return (StringBuffer('ProductTransaction(')
          ..write('id: $id, ')
          ..write('idTransactionServer: $idTransactionServer, ')
          ..write('idProduct: $idProduct, ')
          ..write('quantityVariation: $quantityVariation, ')
          ..write('date: $date, ')
          ..write('idResponsible: $idResponsible, ')
          ..write('idTaker: $idTaker')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(id, idTransactionServer, idProduct,
      quantityVariation, date, idResponsible, idTaker);
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is ProductTransaction &&
          other.id == this.id &&
          other.idTransactionServer == this.idTransactionServer &&
          other.idProduct == this.idProduct &&
          other.quantityVariation == this.quantityVariation &&
          other.date == this.date &&
          other.idResponsible == this.idResponsible &&
          other.idTaker == this.idTaker);
}

class ProductsTransactionsCompanion
    extends UpdateCompanion<ProductTransaction> {
  final Value<String> id;
  final Value<String?> idTransactionServer;
  final Value<String> idProduct;
  final Value<int> quantityVariation;
  final Value<DateTime> date;
  final Value<String> idResponsible;
  final Value<String> idTaker;
  const ProductsTransactionsCompanion({
    this.id = const Value.absent(),
    this.idTransactionServer = const Value.absent(),
    this.idProduct = const Value.absent(),
    this.quantityVariation = const Value.absent(),
    this.date = const Value.absent(),
    this.idResponsible = const Value.absent(),
    this.idTaker = const Value.absent(),
  });
  ProductsTransactionsCompanion.insert({
    required String id,
    this.idTransactionServer = const Value.absent(),
    required String idProduct,
    required int quantityVariation,
    required DateTime date,
    required String idResponsible,
    required String idTaker,
  })  : id = Value(id),
        idProduct = Value(idProduct),
        quantityVariation = Value(quantityVariation),
        date = Value(date),
        idResponsible = Value(idResponsible),
        idTaker = Value(idTaker);
  static Insertable<ProductTransaction> custom({
    Expression<String>? id,
    Expression<String?>? idTransactionServer,
    Expression<String>? idProduct,
    Expression<int>? quantityVariation,
    Expression<DateTime>? date,
    Expression<String>? idResponsible,
    Expression<String>? idTaker,
  }) {
    return RawValuesInsertable({
      if (id != null) 'id': id,
      if (idTransactionServer != null)
        'id_transaction_server': idTransactionServer,
      if (idProduct != null) 'id_product': idProduct,
      if (quantityVariation != null) 'quantity_variation': quantityVariation,
      if (date != null) 'date': date,
      if (idResponsible != null) 'id_responsible': idResponsible,
      if (idTaker != null) 'id_taker': idTaker,
    });
  }

  ProductsTransactionsCompanion copyWith(
      {Value<String>? id,
      Value<String?>? idTransactionServer,
      Value<String>? idProduct,
      Value<int>? quantityVariation,
      Value<DateTime>? date,
      Value<String>? idResponsible,
      Value<String>? idTaker}) {
    return ProductsTransactionsCompanion(
      id: id ?? this.id,
      idTransactionServer: idTransactionServer ?? this.idTransactionServer,
      idProduct: idProduct ?? this.idProduct,
      quantityVariation: quantityVariation ?? this.quantityVariation,
      date: date ?? this.date,
      idResponsible: idResponsible ?? this.idResponsible,
      idTaker: idTaker ?? this.idTaker,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (id.present) {
      map['id'] = Variable<String>(id.value);
    }
    if (idTransactionServer.present) {
      map['id_transaction_server'] =
          Variable<String?>(idTransactionServer.value);
    }
    if (idProduct.present) {
      map['id_product'] = Variable<String>(idProduct.value);
    }
    if (quantityVariation.present) {
      map['quantity_variation'] = Variable<int>(quantityVariation.value);
    }
    if (date.present) {
      map['date'] = Variable<DateTime>(date.value);
    }
    if (idResponsible.present) {
      map['id_responsible'] = Variable<String>(idResponsible.value);
    }
    if (idTaker.present) {
      map['id_taker'] = Variable<String>(idTaker.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('ProductsTransactionsCompanion(')
          ..write('id: $id, ')
          ..write('idTransactionServer: $idTransactionServer, ')
          ..write('idProduct: $idProduct, ')
          ..write('quantityVariation: $quantityVariation, ')
          ..write('date: $date, ')
          ..write('idResponsible: $idResponsible, ')
          ..write('idTaker: $idTaker')
          ..write(')'))
        .toString();
  }
}

class $ProductsTransactionsTable extends ProductsTransactions
    with TableInfo<$ProductsTransactionsTable, ProductTransaction> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $ProductsTransactionsTable(this.attachedDatabase, [this._alias]);
  final VerificationMeta _idMeta = const VerificationMeta('id');
  @override
  late final GeneratedColumn<String?> id = GeneratedColumn<String?>(
      'id', aliasedName, false,
      type: const StringType(), requiredDuringInsert: true);
  final VerificationMeta _idTransactionServerMeta =
      const VerificationMeta('idTransactionServer');
  @override
  late final GeneratedColumn<String?> idTransactionServer =
      GeneratedColumn<String?>('id_transaction_server', aliasedName, true,
          type: const StringType(), requiredDuringInsert: false);
  final VerificationMeta _idProductMeta = const VerificationMeta('idProduct');
  @override
  late final GeneratedColumn<String?> idProduct = GeneratedColumn<String?>(
      'id_product', aliasedName, false,
      type: const StringType(), requiredDuringInsert: true);
  final VerificationMeta _quantityVariationMeta =
      const VerificationMeta('quantityVariation');
  @override
  late final GeneratedColumn<int?> quantityVariation = GeneratedColumn<int?>(
      'quantity_variation', aliasedName, false,
      type: const IntType(), requiredDuringInsert: true);
  final VerificationMeta _dateMeta = const VerificationMeta('date');
  @override
  late final GeneratedColumn<DateTime?> date = GeneratedColumn<DateTime?>(
      'date', aliasedName, false,
      type: const IntType(), requiredDuringInsert: true);
  final VerificationMeta _idResponsibleMeta =
      const VerificationMeta('idResponsible');
  @override
  late final GeneratedColumn<String?> idResponsible = GeneratedColumn<String?>(
      'id_responsible', aliasedName, false,
      type: const StringType(), requiredDuringInsert: true);
  final VerificationMeta _idTakerMeta = const VerificationMeta('idTaker');
  @override
  late final GeneratedColumn<String?> idTaker = GeneratedColumn<String?>(
      'id_taker', aliasedName, false,
      type: const StringType(), requiredDuringInsert: true);
  @override
  List<GeneratedColumn> get $columns => [
        id,
        idTransactionServer,
        idProduct,
        quantityVariation,
        date,
        idResponsible,
        idTaker
      ];
  @override
  String get aliasedName => _alias ?? 'products_transactions';
  @override
  String get actualTableName => 'products_transactions';
  @override
  VerificationContext validateIntegrity(Insertable<ProductTransaction> instance,
      {bool isInserting = false}) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('id')) {
      context.handle(_idMeta, id.isAcceptableOrUnknown(data['id']!, _idMeta));
    } else if (isInserting) {
      context.missing(_idMeta);
    }
    if (data.containsKey('id_transaction_server')) {
      context.handle(
          _idTransactionServerMeta,
          idTransactionServer.isAcceptableOrUnknown(
              data['id_transaction_server']!, _idTransactionServerMeta));
    }
    if (data.containsKey('id_product')) {
      context.handle(_idProductMeta,
          idProduct.isAcceptableOrUnknown(data['id_product']!, _idProductMeta));
    } else if (isInserting) {
      context.missing(_idProductMeta);
    }
    if (data.containsKey('quantity_variation')) {
      context.handle(
          _quantityVariationMeta,
          quantityVariation.isAcceptableOrUnknown(
              data['quantity_variation']!, _quantityVariationMeta));
    } else if (isInserting) {
      context.missing(_quantityVariationMeta);
    }
    if (data.containsKey('date')) {
      context.handle(
          _dateMeta, date.isAcceptableOrUnknown(data['date']!, _dateMeta));
    } else if (isInserting) {
      context.missing(_dateMeta);
    }
    if (data.containsKey('id_responsible')) {
      context.handle(
          _idResponsibleMeta,
          idResponsible.isAcceptableOrUnknown(
              data['id_responsible']!, _idResponsibleMeta));
    } else if (isInserting) {
      context.missing(_idResponsibleMeta);
    }
    if (data.containsKey('id_taker')) {
      context.handle(_idTakerMeta,
          idTaker.isAcceptableOrUnknown(data['id_taker']!, _idTakerMeta));
    } else if (isInserting) {
      context.missing(_idTakerMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {id};
  @override
  ProductTransaction map(Map<String, dynamic> data, {String? tablePrefix}) {
    return ProductTransaction.fromData(data,
        prefix: tablePrefix != null ? '$tablePrefix.' : null);
  }

  @override
  $ProductsTransactionsTable createAlias(String alias) {
    return $ProductsTransactionsTable(attachedDatabase, alias);
  }
}

class Worker extends DataClass implements Insertable<Worker> {
  final String id;
  final String name;
  Worker({required this.id, required this.name});
  factory Worker.fromData(Map<String, dynamic> data, {String? prefix}) {
    final effectivePrefix = prefix ?? '';
    return Worker(
      id: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}id'])!,
      name: const StringType()
          .mapFromDatabaseResponse(data['${effectivePrefix}name'])!,
    );
  }
  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    map['id'] = Variable<String>(id);
    map['name'] = Variable<String>(name);
    return map;
  }

  WorkersCompanion toCompanion(bool nullToAbsent) {
    return WorkersCompanion(
      id: Value(id),
      name: Value(name),
    );
  }

  factory Worker.fromJson(Map<String, dynamic> json,
      {ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return Worker(
      id: serializer.fromJson<String>(json['id']),
      name: serializer.fromJson<String>(json['name']),
    );
  }
  @override
  Map<String, dynamic> toJson({ValueSerializer? serializer}) {
    serializer ??= driftRuntimeOptions.defaultSerializer;
    return <String, dynamic>{
      'id': serializer.toJson<String>(id),
      'name': serializer.toJson<String>(name),
    };
  }

  Worker copyWith({String? id, String? name}) => Worker(
        id: id ?? this.id,
        name: name ?? this.name,
      );
  @override
  String toString() {
    return (StringBuffer('Worker(')
          ..write('id: $id, ')
          ..write('name: $name')
          ..write(')'))
        .toString();
  }

  @override
  int get hashCode => Object.hash(id, name);
  @override
  bool operator ==(Object other) =>
      identical(this, other) ||
      (other is Worker && other.id == this.id && other.name == this.name);
}

class WorkersCompanion extends UpdateCompanion<Worker> {
  final Value<String> id;
  final Value<String> name;
  const WorkersCompanion({
    this.id = const Value.absent(),
    this.name = const Value.absent(),
  });
  WorkersCompanion.insert({
    required String id,
    required String name,
  })  : id = Value(id),
        name = Value(name);
  static Insertable<Worker> custom({
    Expression<String>? id,
    Expression<String>? name,
  }) {
    return RawValuesInsertable({
      if (id != null) 'id': id,
      if (name != null) 'name': name,
    });
  }

  WorkersCompanion copyWith({Value<String>? id, Value<String>? name}) {
    return WorkersCompanion(
      id: id ?? this.id,
      name: name ?? this.name,
    );
  }

  @override
  Map<String, Expression> toColumns(bool nullToAbsent) {
    final map = <String, Expression>{};
    if (id.present) {
      map['id'] = Variable<String>(id.value);
    }
    if (name.present) {
      map['name'] = Variable<String>(name.value);
    }
    return map;
  }

  @override
  String toString() {
    return (StringBuffer('WorkersCompanion(')
          ..write('id: $id, ')
          ..write('name: $name')
          ..write(')'))
        .toString();
  }
}

class $WorkersTable extends Workers with TableInfo<$WorkersTable, Worker> {
  @override
  final GeneratedDatabase attachedDatabase;
  final String? _alias;
  $WorkersTable(this.attachedDatabase, [this._alias]);
  final VerificationMeta _idMeta = const VerificationMeta('id');
  @override
  late final GeneratedColumn<String?> id = GeneratedColumn<String?>(
      'id', aliasedName, false,
      type: const StringType(), requiredDuringInsert: true);
  final VerificationMeta _nameMeta = const VerificationMeta('name');
  @override
  late final GeneratedColumn<String?> name = GeneratedColumn<String?>(
      'name', aliasedName, false,
      type: const StringType(), requiredDuringInsert: true);
  @override
  List<GeneratedColumn> get $columns => [id, name];
  @override
  String get aliasedName => _alias ?? 'workers';
  @override
  String get actualTableName => 'workers';
  @override
  VerificationContext validateIntegrity(Insertable<Worker> instance,
      {bool isInserting = false}) {
    final context = VerificationContext();
    final data = instance.toColumns(true);
    if (data.containsKey('id')) {
      context.handle(_idMeta, id.isAcceptableOrUnknown(data['id']!, _idMeta));
    } else if (isInserting) {
      context.missing(_idMeta);
    }
    if (data.containsKey('name')) {
      context.handle(
          _nameMeta, name.isAcceptableOrUnknown(data['name']!, _nameMeta));
    } else if (isInserting) {
      context.missing(_nameMeta);
    }
    return context;
  }

  @override
  Set<GeneratedColumn> get $primaryKey => {id};
  @override
  Worker map(Map<String, dynamic> data, {String? tablePrefix}) {
    return Worker.fromData(data,
        prefix: tablePrefix != null ? '$tablePrefix.' : null);
  }

  @override
  $WorkersTable createAlias(String alias) {
    return $WorkersTable(attachedDatabase, alias);
  }
}

abstract class _$StockDb extends GeneratedDatabase {
  _$StockDb(QueryExecutor e) : super(SqlTypeSystem.defaultInstance, e);
  late final $ProductsTable products = $ProductsTable(this);
  late final $ProductsTransactionsTable productsTransactions =
      $ProductsTransactionsTable(this);
  late final $WorkersTable workers = $WorkersTable(this);
  @override
  Iterable<TableInfo> get allTables => allSchemaEntities.whereType<TableInfo>();
  @override
  List<DatabaseSchemaEntity> get allSchemaEntities =>
      [products, productsTransactions, workers];
}
