import 'package:rufino_smart_app/storage/database/dao/product_dao.dart';
import 'package:rufino_smart_app/storage/database/stock_db.dart';

class ProductService {
  late final ProductDao _dao;

  ProductService(ProductDao dao) {
    _dao = dao;
  }

  Stream<List<Product>> getAll() {
    return _dao.listAll();
  }
}
