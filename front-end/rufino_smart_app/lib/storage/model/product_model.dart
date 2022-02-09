import 'package:rufino_smart_app/utils/guid.dart';

class product {
  Guid _id;
  String _name;
  String _description;
  String _section;
  String _category;
  String _unity;
  int _quantity;

  product(
    this._id,
    this._name,
    this._description,
    this._section,
    this._category,
    this._unity,
    this._quantity,
  );

  get id => _id;
  get name => _name;
  get description => _description;
  get section => _section;
  get category => _category;
  get unity => _unity;
  get quantity => _quantity;
}
