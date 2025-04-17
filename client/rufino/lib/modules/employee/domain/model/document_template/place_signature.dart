import 'package:equatable/equatable.dart';
import 'package:rufino/modules/employee/domain/model/document_template/number.dart';
import 'package:rufino/modules/employee/domain/model/document_template/page.dart';
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
    Object? generic,
  }) {
    if (generic != null) {
      switch (generic.runtimeType) {
        case const (TypeSignature):
          typeSignature = generic as TypeSignature;
          break;
        case const (Page):
          page = generic as Page;
          break;
        case const (RelativePositionBotton):
          relativePositionBotton = generic as RelativePositionBotton;
          break;
        case const (RelativePositionLeft):
          relativePositionLeft = generic as RelativePositionLeft;
          break;
        case const (RelativeSizeX):
          relativeSizeX = generic as RelativeSizeX;
          break;
        case const (RelativeSizeY):
          relativeSizeY = generic as RelativeSizeY;
          break;
        default:
          break;
      }
    }
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
      page: Page(json['page'].toString()),
      relativePositionBotton:
          RelativePositionBotton(json['relativePositionBotton'].toString()),
      relativePositionLeft:
          RelativePositionLeft(json['relativePositionLeft'].toString()),
      relativeSizeX: RelativeSizeX(json['relativeSizeX'].toString()),
      relativeSizeY: RelativeSizeY(json['relativeSizeY'].toString()),
    );
  }

  static List<PlaceSignature> fromListJson(List<dynamic> jsonList) {
    return jsonList
        .map((json) => PlaceSignature.fromJson(json as Map<String, dynamic>))
        .toList();
  }

  Map<String, dynamic> toJson() {
    return {
      'type': typeSignature.toInt(),
      'page': page.toInt(),
      'relativePositionBotton': relativePositionBotton.toDouble(),
      'relativePositionLeft': relativePositionLeft.toDouble(),
      'relativeSizeX': relativeSizeX.toDouble(),
      'relativeSizeY': relativeSizeY.toDouble(),
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
