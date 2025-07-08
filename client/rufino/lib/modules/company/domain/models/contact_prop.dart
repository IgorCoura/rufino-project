import 'package:equatable/equatable.dart';

class ContactProp extends Equatable {
  final EmailProp email;
  final PhoneProp phone;

  const ContactProp(this.email, this.phone);

  const ContactProp.empty()
      : email = const EmailProp.empty(),
        phone = const PhoneProp.empty();

  ContactProp copyWith({
    EmailProp? email,
    PhoneProp? phone,
    Object? generic,
  }) {
    switch (generic.runtimeType) {
      case const (EmailProp):
        email = generic as EmailProp?;
        break;
      case const (PhoneProp):
        phone = generic as PhoneProp?;
        break;
    }
    return ContactProp(
      email ?? this.email,
      phone ?? this.phone,
    );
  }

  factory ContactProp.fromJson(Map<String, dynamic> json) {
    return ContactProp(
      EmailProp(json['email']),
      PhoneProp(json['phone']),
    );
  }

  Map<String, dynamic> toJson() {
    return {
      'email': email.value,
      'phone': phone.value,
    };
  }

  @override
  List<Object?> get props => [email, phone];
}

class EmailProp extends Equatable {
  final String value;

  const EmailProp(this.value);

  const EmailProp.empty() : value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }
    var regex = RegExp(r"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");
    if (!regex.hasMatch(value)) {
      return "Formato invalido.";
    }
    if (value.length > 100) {
      return "Não pode ser maior que 100 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}

class PhoneProp extends Equatable {
  final String value;

  const PhoneProp(this.value);

  const PhoneProp.empty() : value = "";

  static String? validate(String? value) {
    if (value == null || value.isEmpty) {
      return "Não pode ser vazio.";
    }
    value = value.replaceAll(RegExp(r'\D'), '');
    var regex = RegExp(r"^[0-9]{2}[0-9]{9}|[0-9]{8}");
    if (!regex.hasMatch(value)) {
      return "Formato invalido.";
    }
    if (value.length > 15) {
      return "Não pode ser maior que 15 caracteres.";
    }
    return null;
  }

  @override
  List<Object?> get props => [value];
}
