Frappe

# Rules of Bundling

  1. All bundles must be created within the App_Bundles folder or one of it's sub-folders.
  2. All bundles must have either '.js.bundle' or '.css.bundle' as their extension.
  3. All paths to files within the bundle must be relative file system paths.

# Known Issues

  1. Visual Studio does not execute AfterBuild when doing Build. This means changes 
     to bundles will not be reflected until Rebuild is run or source is changed.

# Bundle Example

<?xml version="1.0" encoding="utf-8" ?>
<Bundle>
  <!-- Use Bundle element when you want to include a bundle within another bundle. -->
  <Bundle File="themes\base\jquery.css.bundle" />
  <Include File="Widget.less" />
  <Include File="Site.css" />
</Bundle>