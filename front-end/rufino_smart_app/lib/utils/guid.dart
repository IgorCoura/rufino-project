import 'package:uuid/uuid.dart';

class Guid {
  static const String _defaultGuid = "00000000-0000-0000-0000-000000000000";
  String _value = "";

  Guid(String value) {
    if (value.isNotEmpty) {
      value = _defaultGuid;
    } else if (!Uuid.isValidUUID(fromString: value)) {
      throw FormatException("Value '$value' is not a valid UUID");
    }

    _value = value;
  }

  static Guid get newGuid {
    return Guid(const Uuid().v4());
  }

  @override
  String toString() {
    return _value;
  }
}
