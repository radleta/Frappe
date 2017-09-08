Frappe

# Rules of Sprites

  1. Each sub-folder including the root of App_Sprites will generate a unquie sprite based on the images within the folder.
  2. Each sprite generation settings can be optionally customized by creating a Settings.xml file in the directory. The defaults are used when none present.

# Razor View Example

@section Head {
    @Frappe.Mvc.Sprite.ImportStylesheet("~/App_Sprites/Example1WithoutSettings")
    @Frappe.Mvc.Sprite.ImportStylesheet("~/App_Sprites/Example2WithSettings")    
}
@section Scripts {
}
<h1>Frappe Mvc4 Examples</h1>

<h2>Sprite Examples</h2>

<h3>Example 1 Without Settings</h3>
@Frappe.Mvc.Sprite.Image("~/App_Sprites/Example1WithoutSettings/cat2.gif")
@Frappe.Mvc.Sprite.Image("~/App_Sprites/Example1WithoutSettings/cat2.png")

<h3>Example 2 With Settings</h3>
@Frappe.Mvc.Sprite.Image("~/App_Sprites/Example2WithSettings/cat1.gif")
@Frappe.Mvc.Sprite.Image("~/App_Sprites/Example2WithSettings/cat2.jpg")
@Frappe.Mvc.Sprite.Image("~/App_Sprites/Example2WithSettings/cat-ice-cream.png")

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