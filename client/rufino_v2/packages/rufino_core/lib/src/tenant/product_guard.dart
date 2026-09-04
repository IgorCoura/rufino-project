import 'package:flutter/widgets.dart';
import 'package:provider/provider.dart';

import 'tenant_context.dart';

/// Conditionally renders [child] only when the selected tenant has [product]
/// enabled.
///
/// This is the **first** of the two gates a feature has to pass. It answers
/// "is this product turned on for this customer?"; `PermissionGuard` answers
/// "is this person allowed to use it?". Both are required — a product enabled
/// for the tenant still shows nothing to someone without permission, and a
/// permission granted in Keycloak shows nothing for a tenant that never
/// bought the product.
///
/// Renders [SizedBox.shrink] when the product is off, or when no tenant is
/// selected.
class ProductGuard extends StatelessWidget {
  /// Creates a guard around [child] for [product].
  const ProductGuard({
    super.key,
    required this.product,
    required this.child,
  });

  /// The product code — use the constants in `TenantProducts`.
  final String product;

  /// The widget to render when the product is enabled.
  final Widget child;

  @override
  Widget build(BuildContext context) {
    final tenantContext = context.watch<TenantContextNotifier>();
    if (tenantContext.hasProduct(product)) return child;
    return const SizedBox.shrink();
  }
}
