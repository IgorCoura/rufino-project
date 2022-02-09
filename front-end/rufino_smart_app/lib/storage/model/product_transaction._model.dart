import 'package:rufino_smart_app/utils/guid.dart';

class ProductTransaction {
  Guid _id;
  Guid _productId;
  int _quantityVariation;
  DateTime _date;
  Guid _responsibleId;
  Guid _takerId;
  bool _transactionServerId;

  ProductTransaction(
    this._id,
    this._productId,
    this._quantityVariation,
    this._date,
    this._responsibleId,
    this._takerId,
    this._transactionServerId,
  );

  get id => _id;
  get productId => _productId;
  get quantityVariation => _quantityVariation;
  get date => _date;
  get responsibleId => _responsibleId;
  get takerId => _takerId;
  get transactionServerId => _transactionServerId;
}
