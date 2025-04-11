import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/document_template/number.dart';
import 'package:rufino/modules/employee/domain/model/document_template/type_signature.dart';

class PlaceSignature extends Equatable {
  final TypeSignature typeSignature;
  final Page page;
  final RelativePositionBotton relativePositionBotton;
  final RelativePositionLeft relativePositionLeft;
  final RelativeSizeX relativeSizeX;
  final RelativeSizeY relativeSizeY;

  const PlaceSignature(
      {required this.typeSignature,
      required this.page,
      required this.relativePositionBotton,
      required this.relativePositionLeft,
      required this.relativeSizeX,
      required this.relativeSizeY});

  const PlaceSignature.empty(
      {this.typeSignature = const TypeSignature.empty(),
      this.page = const Page.empty(),
      this.relativePositionBotton = const RelativePositionBotton.empty(),
      this.relativePositionLeft = const RelativePositionLeft.empty(),
      this.relativeSizeX = const RelativeSizeX.empty(),
      this.relativeSizeY = const RelativeSizeY.empty()});

  PlaceSignature copyWith({
    TypeSignature? typeSignature,
    Page? page,
    RelativePositionBotton? relativePositionBotton,
    RelativePositionLeft? relativePositionLeft,
    RelativeSizeX? relativeSizeX,
    RelativeSizeY? relativeSizeY,
  }) {
    return PlaceSignature(
      typeSignature: typeSignature ?? this.typeSignature,
      page: page ?? this.page,
      relativePositionBotton:
          relativePositionBotton ?? this.relativePositionBotton,
      relativePositionLeft: relativePositionLeft ?? this.relativePositionLeft,
      relativeSizeX: relativeSizeX ?? this.relativeSizeX,
      relativeSizeY: relativeSizeY ?? this.relativeSizeY,
    );
  }

  factory PlaceSignature.fromJson(Map<String, dynamic> json) {
    return PlaceSignature(
      typeSignature: TypeSignature.fromJson(json['typeSignature']),
      page: Page(json['page']),
      relativePositionBotton:
          RelativePositionBotton(json['relativePositionBotton']),
      relativePositionLeft: RelativePositionLeft(json['relativePositionLeft']),
      relativeSizeX: RelativeSizeX(json['relativeSizeX']),
      relativeSizeY: RelativeSizeY(json['relativeSizeY']),
    );
  }

  static List<PlaceSignature> fromListJson(List<dynamic> jsonList) {
    return jsonList
        .map((json) => PlaceSignature.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Map<String, dynamic> toJson() {
    return {
      'type': typeSignature.id,
      'page': page.toInt(),
      'relativePositionBotton': relativePositionBotton.toInt(),
      'relativePositionLeft': relativePositionLeft.toInt(),
      'relativeSizeX': relativeSizeX.toInt(),
      'relativeSizeY': relativeSizeY.toInt(),
    };
  }

  @override
  List<Object?> get props => [
        typeSignature,
        page,
        relativePositionBotton,
        relativePositionLeft,
        relativeSizeX,
        relativeSizeY
      ];

  List<Object?> get textProps => [
        page,
        relativePositionBotton,
        relativePositionLeft,
        relativeSizeX,
        relativeSizeY
      ];
}
