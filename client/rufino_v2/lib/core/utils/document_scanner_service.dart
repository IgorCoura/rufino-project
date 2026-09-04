/// Builds the platform implementation of the product's scanner port.
///
/// The **contract** lives in `people_management` (`DocumentScannerService`);
/// only the choice of implementation lives here, because scanning needs the
/// camera, a runtime permission and an OCR engine — three plugins with native
/// code. This file is the single place that names them.
library;

import 'package:people_management/people_management.dart';

import 'document_scanner_service_stub.dart'
    if (dart.library.js_interop) 'document_scanner_service_web.dart'
    if (dart.library.io) 'document_scanner_service_mobile.dart' as platform;

/// Returns the [DocumentScannerService] for the platform this build targets.
///
/// Desktop gets the stub, which answers `isPlatformSupported == false` instead
/// of throwing — a product that cannot scan here still has to render.
DocumentScannerService createDocumentScannerService() =>
    platform.DocumentScannerServiceImpl();
