/// Full-screen dialog for capturing document pages via the device camera.
///
/// Used on web where native document scanning is unavailable. Displays
/// a live camera preview and lets the user capture multiple pages, review
/// them in a thumbnail gallery, and confirm when all pages are captured.
/// On mobile platforms the native scanner is used instead of this dialog.
library;

import 'dart:typed_data';

import 'package:camera/camera.dart';
import 'package:flutter/material.dart';

import '../../../../core/theme/app_spacing.dart';

/// Dialog that provides a camera-based document capture experience.
///
/// Returns a `List<Uint8List>` of captured page images via [Navigator.pop]
/// when the user confirms, or `null` if cancelled.
class DocumentScanDialog extends StatefulWidget {
  /// Creates the document scan dialog.
  const DocumentScanDialog({super.key});

  @override
  State<DocumentScanDialog> createState() => _DocumentScanDialogState();
}

class _DocumentScanDialogState extends State<DocumentScanDialog> {
  CameraController? _controller;
  List<CameraDescription> _cameras = [];
  final List<Uint8List> _capturedPages = [];
  bool _isInitializing = true;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    _initCamera();
  }

  Future<void> _initCamera() async {
    try {
      _cameras = await availableCameras();
      if (_cameras.isEmpty) {
        setState(() {
          _isInitializing = false;
          _errorMessage = 'Nenhuma câmera disponível.';
        });
        return;
      }

      // Prefer back camera for document scanning.
      final camera = _cameras.firstWhere(
        (c) => c.lensDirection == CameraLensDirection.back,
        orElse: () => _cameras.first,
      );

      _controller = CameraController(
        camera,
        ResolutionPreset.high,
        enableAudio: false,
      );

      await _controller!.initialize();
      if (!mounted) return;
      setState(() => _isInitializing = false);
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _isInitializing = false;
        _errorMessage = 'Erro ao acessar a câmera: $e';
      });
    }
  }

  @override
  void dispose() {
    _controller?.dispose();
    super.dispose();
  }

  Future<void> _capturePhoto() async {
    if (_controller == null || !_controller!.value.isInitialized) return;
    if (_controller!.value.isTakingPicture) return;

    try {
      final xFile = await _controller!.takePicture();
      final bytes = await xFile.readAsBytes();
      setState(() => _capturedPages.add(bytes));
    } catch (e) {
      if (!mounted) return;
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          content: Text('Erro ao capturar foto: $e'),
          behavior: SnackBarBehavior.floating,
        ),
      );
    }
  }

  void _removePage(int index) {
    setState(() => _capturedPages.removeAt(index));
  }

  void _confirm() {
    Navigator.of(context).pop(_capturedPages);
  }

  void _cancel() {
    Navigator.of(context).pop(null);
  }

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Dialog.fullscreen(
      child: Scaffold(
        appBar: AppBar(
          leading: IconButton(
            icon: const Icon(Icons.close),
            onPressed: _cancel,
          ),
          title: const Text('Digitalizar Documento'),
          centerTitle: false,
          actions: [
            if (_capturedPages.isNotEmpty)
              FilledButton.icon(
                onPressed: _confirm,
                icon: const Icon(Icons.check, size: 18),
                label: Text('Confirmar (${_capturedPages.length})'),
              ),
            const SizedBox(width: AppSpacing.sm),
          ],
        ),
        body: SafeArea(
          child: Column(
            children: [
              // Camera preview area
              Expanded(child: _buildCameraArea(colorScheme, textTheme)),
              // Thumbnail gallery
              if (_capturedPages.isNotEmpty)
                _PageGallery(
                  pages: _capturedPages,
                  onRemove: _removePage,
                ),
            ],
          ),
        ),
        floatingActionButton: _errorMessage == null && !_isInitializing
            ? FloatingActionButton.large(
                onPressed: _capturePhoto,
                child: const Icon(Icons.camera_alt),
              )
            : null,
        floatingActionButtonLocation: FloatingActionButtonLocation.centerFloat,
      ),
    );
  }

  Widget _buildCameraArea(ColorScheme colorScheme, TextTheme textTheme) {
    if (_isInitializing) {
      return const Center(child: CircularProgressIndicator());
    }

    if (_errorMessage != null) {
      return Center(
        child: Padding(
          padding: const EdgeInsets.all(AppSpacing.lg),
          child: Column(
            mainAxisSize: MainAxisSize.min,
            children: [
              Icon(
                Icons.camera_alt_outlined,
                size: 48,
                color: colorScheme.error,
              ),
              const SizedBox(height: AppSpacing.md),
              Text(
                _errorMessage!,
                style: textTheme.bodyLarge?.copyWith(
                  color: colorScheme.error,
                ),
                textAlign: TextAlign.center,
              ),
            ],
          ),
        ),
      );
    }

    if (_controller == null || !_controller!.value.isInitialized) {
      return const Center(child: CircularProgressIndicator());
    }

    return CameraPreview(_controller!);
  }
}

/// Horizontal scrollable gallery of captured page thumbnails.
class _PageGallery extends StatelessWidget {
  const _PageGallery({required this.pages, required this.onRemove});

  final List<Uint8List> pages;
  final void Function(int index) onRemove;

  @override
  Widget build(BuildContext context) {
    final colorScheme = Theme.of(context).colorScheme;
    final textTheme = Theme.of(context).textTheme;

    return Container(
      height: 120,
      decoration: BoxDecoration(
        color: colorScheme.surfaceContainer,
        border: Border(
          top: BorderSide(color: colorScheme.outlineVariant),
        ),
      ),
      child: ListView.separated(
        scrollDirection: Axis.horizontal,
        padding: const EdgeInsets.symmetric(
          horizontal: AppSpacing.md,
          vertical: AppSpacing.sm,
        ),
        itemCount: pages.length,
        separatorBuilder: (_, __) => const SizedBox(width: AppSpacing.sm),
        itemBuilder: (context, index) {
          return Stack(
            children: [
              ClipRRect(
                borderRadius: BorderRadius.circular(8),
                child: Image.memory(
                  pages[index],
                  width: 80,
                  height: 100,
                  fit: BoxFit.cover,
                ),
              ),
              // Page number badge
              Positioned(
                left: 4,
                bottom: 4,
                child: Container(
                  padding: const EdgeInsets.symmetric(
                    horizontal: 6,
                    vertical: 2,
                  ),
                  decoration: BoxDecoration(
                    color: colorScheme.surface.withValues(alpha: 0.8),
                    borderRadius: BorderRadius.circular(4),
                  ),
                  child: Text(
                    '${index + 1}',
                    style: textTheme.labelSmall,
                  ),
                ),
              ),
              // Remove button
              Positioned(
                right: 0,
                top: 0,
                child: IconButton(
                  icon: Icon(
                    Icons.cancel,
                    size: 20,
                    color: colorScheme.error,
                  ),
                  padding: EdgeInsets.zero,
                  constraints: const BoxConstraints(
                    minWidth: 24,
                    minHeight: 24,
                  ),
                  onPressed: () => onRemove(index),
                ),
              ),
            ],
          );
        },
      ),
    );
  }
}
