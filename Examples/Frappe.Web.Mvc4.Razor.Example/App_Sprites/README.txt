Frappe

# Rules of Sprites

  1. Each sub-folder including the root of App_Sprites will generate a unquie sprite based on the images within the folder.
  2. Each sprite generation settings can be optionally customized by creating a Settings.xml file in the directory. The defaults are used when none present.

# Settings.xml Example

<?xml version="1.0" encoding="utf-8" ?>
<ImageOptimizationSettings>
  <!-- The output image file format -->
  <FileFormat>png</FileFormat>
  <!-- Controls whether base64 inlining should be used for high-compatibility browsers -->
  <Base64Encoding>true</Base64Encoding>
  <!-- The quality level of the format, if the format supports quality settings (such as jpg) -->
  <Quality>100</Quality>
  <!-- The maximum size of a sprite before it will be split into multiple images -->
  <MaxSize>450</MaxSize>
  <!-- The background color of the output sprite -->
  <BackgroundColor>00000000</BackgroundColor>
  <!-- Controls whether the application will tile images along the X or Y axis -->
  <TileInYAxis>false</TileInYAxis>
</ImageOptimizationSettings>