import 'package:rufino/modules/employee/domain/model/contact/cellphone.dart';
import 'package:rufino/modules/employee/domain/model/contact/email.dart';
import 'package:rufino/modules/employee/domain/model/base/model_base.dart';
import 'package:rufino/modules/employee/domain/model/base/text_prop_base.dart';

class Contact extends ModelBase {
  final Cellphone cellphone;
  final Email email;

  const Contact(this.cellphone, this.email,
      {super.isLoading = false, super.isLazyLoading = false});

  const Contact.loading(
      {this.cellphone = const Cellphone.empty(),
      this.email = const Email.empty(),
      super.isLoading = true,
      super.isLazyLoading = false});

  Contact copyWith(
      {Cellphone? cellphone,
      Email? email,
      Object? generic,
      bool? isLoading,
      bool? isLazyLoading}) {
    if (generic != null) {
      switch (generic.runtimeType) {
        case const (Cellphone):
          cellphone = generic as Cellphone?;
        case const (Email):
          email = generic as Email?;
      }
    }
    return Contact(
      cellphone ?? this.cellphone,
      email ?? this.email,
      isLoading: isLoading ?? this.isLoading,
      isLazyLoading: isLazyLoading ?? this.isLazyLoading,
    );
  }

  factory Contact.fromJson(Map<String, dynamic> json) {
    return Contact(
        Cellphone.createFormatNumber(json["cellphone"]), Email(json["email"]),
        isLoading: false);
  }

  @override
  List<Object?> get props => [cellphone, email, isLoading, isLazyLoading];

  List<TextPropBase> get textProps => [cellphone, email];
}
