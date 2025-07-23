class VoteIdValidator {
  // ignore: constant_identifier_names
  static const List<String> BLACKLIST = [
    "000000000000",
    "111111111111",
    "222222222222",
    "333333333333",
    "444444444444",
    "555555555555",
    "666666666666",
    "777777777777",
    "888888888888",
    "999999999999"
  ];

  // ignore: constant_identifier_names
  static const STRIP_REGEX = r'[^\d]';

  static String format(String value) {
    RegExp regExp = RegExp(r'^(\d{4})(\d{4})(\d{4})$');

    return strip(value)
        .replaceAllMapped(regExp, (Match m) => "${m[1]}.${m[2]}.${m[3]}");
  }

  static String strip(String? value) {
    RegExp regExp = RegExp(STRIP_REGEX);
    value = value ?? "";

    return value.replaceAll(regExp, "");
  }

  static bool isValid(String? value, [stripBeforeValidation = true]) {
    if (stripBeforeValidation) {
      value = strip(value);
    }

    // must be defined
    if (value == null || value.isEmpty) {
      return false;
    }

    //must have 12 chars
    if (value.length != 12) {
      return false;
    }

    //can't be blacklisted
    if (BLACKLIST.contains(value)) {
      return false;
    }

    String uf = '${value[8]}${value[9]}';

    String aux = value.substring(0, 8);
    int sum = 0;

    var multiplierOne = [2, 3, 4, 5, 6, 7, 8, 9];
    var multiplierTwo = [7, 8, 9];

    for (int i = 0; i < 8; i++) {
      sum += int.parse(aux[i]) * multiplierOne[i];
    }

    int rest = sum % 11;

    if (rest > 9) {
      rest = 0;
    } else if (rest == 0) {
      if (uf == '01' || uf == '02') {
        rest = 1;
      }
    }

    String z1 = rest.toString();

    aux = '${value[8]}${value[9]}$z1';
    sum = 0;

    for (int i = 0; i < 3; i++) {
      sum += int.parse(aux[i]) * multiplierTwo[i];
    }

    rest = sum % 11;

    if (rest > 9) {
      rest = 0;
    } else if (rest == 0) {
      if (uf == '01' || uf == '02') {
        rest = 1;
      }
    }

    String z2 = rest.toString();

    String digit = '$z1$z2';

    return value.endsWith(digit);
  }
}
