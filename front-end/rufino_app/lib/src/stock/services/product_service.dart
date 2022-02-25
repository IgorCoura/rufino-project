import 'package:rufino_app/src/stock/db/dao/product_dao.dart';
import 'package:rufino_app/src/stock/db/stock_db.dart';
import 'package:rufino_app/src/stock/repository/product_repository.dart';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:rufino_app/src/stock/utils/stock_constants.dart';
import 'package:shared_preferences/shared_preferences.dart';

class ProductService {
  final ProductRepository _productRepository;
  final ProductDao _productDao;

  ProductService(this._productRepository, this._productDao);

  Stream<List<Product>> getAll(String searchString) async* {
    var sharedPreferences = await SharedPreferences.getInstance();
    var date = sharedPreferences.getString("productDate") ?? kDateDefault;
    syncProductWithServer(DateTime.parse(date));
    sharedPreferences.setString(
        "productDate", DateTime.now().toUtc().toString());
    yield* _productDao.getAll(searchString);
  }

  void syncProductWithServer(DateTime modificationDate) async {
    var connectivityResult = await (Connectivity().checkConnectivity());
    if (connectivityResult == ConnectivityResult.ethernet ||
        connectivityResult == ConnectivityResult.wifi) {
      var products = await _productRepository.getAllProducts(modificationDate);
      _productDao.updateOrAdd(products);
    }
  }
}
